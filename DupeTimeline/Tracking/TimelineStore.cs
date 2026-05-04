using System;
using System.Collections.Generic;

namespace DupeTimeline {
    // Singleton normalizer. Receives raw hook events (chore start/end,
    // workable start/stop, navigator stop), folds them into TimelineSegments,
    // and pushes finished segments onto a per-dupe ring buffer.
    //
    // Per-dupe state machine (one OpenChore per dupe at a time):
    //
    //   (idle)
    //     |
    //     +-- OnChoreStart --> open Travel segment
    //                            |
    //                            +-- OnWorkableEvent(WorkStarted) -->
    //                            |     close Travel, open Work
    //                            |       |
    //                            |       +-- WorkStopped/Completed -->
    //                            |             close Work, open Travel
    //                            |             (loop -> may see another
    //                            |              WorkStarted in the same chore;
    //                            |              this is the build-chore case
    //                            |              where the dupe ferries materials
    //                            |              between fetches and placements)
    //                            |
    //                            +-- OnChoreEnd --> close current segment,
    //                                              dupe is idle
    //
    // A chore with no Workable target (mingle, idle move) stays in its
    // initial Travel segment for the entire chore.
    //
    // Audited edge cases:
    //   - Multi-bout build chores (work, fetch, work): each WorkStarted opens
    //     a new Work segment; intervening Travels are recorded.
    //   - Cancelled chore mid-work (no WorkStopped event): OnChoreEnd flushes
    //     whatever segment kind is open and removes the workerByWorkable
    //     entry defensively.
    //   - Two dupes on the same workable (rare): workerByWorkable is 1:1, the
    //     second WorkStarted overwrites; the first dupe's Work segment closes
    //     at OnChoreEnd instead of WorkStopped. Slight time inflation; not a
    //     correctness issue for v1.
    //   - Travel segment after WorkStopped retains the chore's primary
    //     workable id (e.g. build site) even though the dupe might be
    //     pathing to a storage bin — we report the chore's target, not
    //     the navigator's current destination.
    public static class TimelineStore {
        // ~3 cycles for a busy dupe.
        private const int Capacity = 210;

        private static readonly Dictionary<int, TimelineRingBuffer> store
            = new Dictionary<int, TimelineRingBuffer>();
        private static readonly Dictionary<int, OpenChore> inflight
            = new Dictionary<int, OpenChore>();
        // Reverse lookup: workable instance id -> dupe instance id currently
        // working it. Set on WorkStarted, cleared on WorkStopped/Completed.
        private static readonly Dictionary<int, int> workerByWorkable
            = new Dictionary<int, int>();

        private struct OpenChore {
            public string Guid;
            public string ChoreTypeId;
            public string ChoreTypeName;
            public int WorkableInstanceId;   // -1 if none
            public string WorkableName;
            public int TargetCell;            // -1 if unknown
            public float SegmentStartTime;
            public TimelineSegmentKind SegmentKind;
        }

        public static void Reset() {
            store.Clear();
            inflight.Clear();
            workerByWorkable.Clear();
        }

        public static IEnumerable<TimelineSegment> GetTimeline(int dupeInstanceId) {
            return store.TryGetValue(dupeInstanceId, out var buf)
                ? buf.InOrder()
                : Array.Empty<TimelineSegment>();
        }

        public static IEnumerable<int> KnownDupeIds() {
            return store.Keys;
        }

        public static List<TimelineSegment> GetRecent(int dupeInstanceId, int max) {
            var result = new List<TimelineSegment>();
            if (!store.TryGetValue(dupeInstanceId, out var buf)) return result;
            foreach (var seg in buf.InOrder()) result.Add(seg);
            int drop = result.Count - max;
            if (drop > 0) result.RemoveRange(0, drop);
            return result;
        }

        // ---------- Save/load ----------

        public static void Restore(int dupeInstanceId, List<SerializedSegment> segments) {
            if (segments == null) return;
            var buf = GetOrCreate(dupeInstanceId);
            buf.Clear();
            foreach (var s in segments) {
                buf.Add(new TimelineSegment {
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    Kind = (TimelineSegmentKind)s.Kind,
                    ChoreGuid = s.ChoreGuid,
                    ChoreTypeId = s.ChoreTypeId,
                    ChoreTypeName = s.ChoreTypeName,
                    WorkableInstanceId = s.WorkableInstanceId,
                    WorkableName = s.WorkableName,
                    TargetCell = s.TargetCell,
                });
            }
        }

        public static List<SerializedSegment> Snapshot(int dupeInstanceId) {
            var list = new List<SerializedSegment>();
            if (!store.TryGetValue(dupeInstanceId, out var buf)) return list;
            foreach (var seg in buf.InOrder()) {
                list.Add(new SerializedSegment {
                    StartTime = seg.StartTime,
                    EndTime = seg.EndTime,
                    Kind = (int)seg.Kind,
                    ChoreGuid = seg.ChoreGuid,
                    ChoreTypeId = seg.ChoreTypeId,
                    ChoreTypeName = seg.ChoreTypeName,
                    WorkableInstanceId = seg.WorkableInstanceId,
                    WorkableName = seg.WorkableName,
                    TargetCell = seg.TargetCell,
                });
            }
            return list;
        }

        // ---------- Hook entry points ----------

        public static void OnChoreStart(int dupeInstanceId, Chore chore) {
            if (chore == null) return;
            var now = Now();
            var workable = chore.target as Workable;

            var open = new OpenChore {
                Guid = Guid.NewGuid().ToString("N"),
                ChoreTypeId = chore.choreType?.Id,
                ChoreTypeName = chore.choreType?.Name ?? chore.choreType?.Id,
                WorkableInstanceId = workable != null ? InstanceIdOf(workable.gameObject) : -1,
                WorkableName = ResolveName(workable),
                TargetCell = -1,
                SegmentStartTime = now,
                SegmentKind = TimelineSegmentKind.Travel,
            };
            if (inflight.TryGetValue(dupeInstanceId, out var prev)) {
                FlushOpenSegment(dupeInstanceId, prev, now);
            }
            inflight[dupeInstanceId] = open;
        }

        public static void OnChoreEnd(int dupeInstanceId, Chore chore) {
            _ = chore;
            if (!inflight.TryGetValue(dupeInstanceId, out var open)) return;
            FlushOpenSegment(dupeInstanceId, open, Now());
            inflight.Remove(dupeInstanceId);
            if (open.WorkableInstanceId >= 0) {
                workerByWorkable.Remove(open.WorkableInstanceId);
            }
        }

        public static void OnWorkableEvent(Workable w, Workable.WorkableEvent evt) {
            if (w == null) return;

            if (evt == Workable.WorkableEvent.WorkStarted) {
                var worker = w.GetWorker();
                if (worker == null) return;
                var dupeId = InstanceIdOf(worker.gameObject);
                var workableId = InstanceIdOf(w.gameObject);
                workerByWorkable[workableId] = dupeId;
                TransitionTo(dupeId, TimelineSegmentKind.Work, workableId, ResolveName(w));
                return;
            }

            // WorkStopped / WorkCompleted: look up the dupe via reverse map.
            var workableInstanceId = InstanceIdOf(w.gameObject);
            if (!workerByWorkable.TryGetValue(workableInstanceId, out var stoppedDupe)) {
                return;
            }
            workerByWorkable.Remove(workableInstanceId);
            TransitionTo(stoppedDupe, TimelineSegmentKind.Travel, -1, null);
        }

        public static void OnNavigatorStop(int dupeInstanceId, bool arrived) {
            // No-op for v1. Reserved for non-Workable chores (mingle, idle
            // moves, schedule transitions) where Hook B doesn't fire.
            _ = dupeInstanceId;
            _ = arrived;
        }

        // ---------- Internals ----------

        private static void TransitionTo(int dupeInstanceId,
                TimelineSegmentKind newKind, int newWorkableId, string newWorkableName) {
            if (!inflight.TryGetValue(dupeInstanceId, out var open)) return;
            var now = Now();
            FlushOpenSegment(dupeInstanceId, open, now);
            open.SegmentStartTime = now;
            open.SegmentKind = newKind;
            if (newWorkableId >= 0) {
                open.WorkableInstanceId = newWorkableId;
                open.WorkableName = newWorkableName;
            }
            inflight[dupeInstanceId] = open;
        }

        private static void FlushOpenSegment(int dupeInstanceId,
                OpenChore open, float endTime) {
            if (endTime <= open.SegmentStartTime) return;
            GetOrCreate(dupeInstanceId).Add(new TimelineSegment {
                StartTime = open.SegmentStartTime,
                EndTime = endTime,
                Kind = open.SegmentKind,
                ChoreGuid = open.Guid,
                ChoreTypeId = open.ChoreTypeId,
                ChoreTypeName = open.ChoreTypeName,
                WorkableInstanceId = open.WorkableInstanceId,
                WorkableName = open.WorkableName,
                TargetCell = open.TargetCell,
            });
        }

        private static TimelineRingBuffer GetOrCreate(int dupeInstanceId) {
            if (!store.TryGetValue(dupeInstanceId, out var buf)) {
                buf = new TimelineRingBuffer(Capacity);
                store[dupeInstanceId] = buf;
            }
            return buf;
        }

        private static string ResolveName(Workable w) {
            if (w == null) return null;
            var sel = w.GetComponent<KSelectable>();
            if (sel != null) {
                var name = sel.GetName();
                if (!string.IsNullOrEmpty(name)) return name;
            }
            return w.name;
        }

        private static int InstanceIdOf(UnityEngine.GameObject go) {
            if (go == null) return -1;
            var id = go.GetComponent<KPrefabID>();
            return id != null ? id.InstanceID : go.GetInstanceID();
        }

        private static float Now() {
            return GameClock.Instance != null
                ? GameClock.Instance.GetTime()
                : UnityEngine.Time.time;
        }
    }
}
