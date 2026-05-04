using System;
using System.Collections.Generic;
using PeterHan.PLib.Core;

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
    //                            |              WorkStarted in the same chore)
    //                            |
    //                            +-- OnChoreEnd --> close current segment,
    //                                              dupe is idle
    //
    // A chore with no Workable target (mingle, idle move) stays in its
    // initial Travel segment for the entire chore.
    public static class TimelineStore {
        private const int DefaultCapacity = 200;
        private const int FallbackCapacity = 200;

        private static readonly Dictionary<int, TimelineRingBuffer> store
            = new Dictionary<int, TimelineRingBuffer>();
        private static readonly Dictionary<int, OpenChore> inflight
            = new Dictionary<int, OpenChore>();
        // Reverse lookup: workable instance id -> dupe instance id currently
        // working it. Set on WorkStarted, cleared on WorkStopped/Completed.
        private static readonly Dictionary<int, int> workerByWorkable
            = new Dictionary<int, int>();

        private static int capacity = DefaultCapacity;

        private struct OpenChore {
            public string Guid;
            public string ChoreTypeId;
            public int WorkableInstanceId;   // -1 if none
            public int TargetCell;            // -1 if unknown
            public float SegmentStartTime;
            public TimelineSegmentKind SegmentKind;
        }

        public static void Configure(DupeTimelineOptions options) {
            // 200 segments is roughly 3 cycles for a busy dupe; tune via options.
            capacity = options != null && options.CyclesRetained > 0
                ? options.CyclesRetained * 70
                : FallbackCapacity;
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
                    WorkableInstanceId = s.WorkableInstanceId,
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
                    WorkableInstanceId = seg.WorkableInstanceId,
                    TargetCell = seg.TargetCell,
                });
            }
            return list;
        }

        // ---------- Hook entry points ----------

        public static void OnChoreStart(int dupeInstanceId, Chore chore) {
            if (chore == null) return;
            var now = Now();
            var open = new OpenChore {
                Guid = Guid.NewGuid().ToString("N"),
                ChoreTypeId = chore.choreType?.Id,
                WorkableInstanceId = WorkableInstanceIdFor(chore),
                TargetCell = TargetCellFor(chore),
                SegmentStartTime = now,
                SegmentKind = TimelineSegmentKind.Travel,
            };
            // If a previous chore wasn't closed cleanly (interrupt / save-load
            // race), flush whatever is open before we overwrite.
            if (inflight.TryGetValue(dupeInstanceId, out var prev)) {
                FlushOpenSegment(dupeInstanceId, prev, now);
            }
            inflight[dupeInstanceId] = open;
        }

        public static void OnChoreEnd(int dupeInstanceId, Chore chore) {
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
                TransitionTo(dupeId, TimelineSegmentKind.Work, workableId);
                return;
            }

            // WorkStopped / WorkCompleted: look up the dupe via reverse map.
            var workableInstanceId = InstanceIdOf(w.gameObject);
            if (!workerByWorkable.TryGetValue(workableInstanceId, out var stoppedDupe)) {
                return;
            }
            workerByWorkable.Remove(workableInstanceId);
            TransitionTo(stoppedDupe, TimelineSegmentKind.Travel, -1);
        }

        public static void OnNavigatorStop(int dupeInstanceId, bool arrived) {
            // Optional: for non-Workable chores this is the only "arrived"
            // signal we get. For workable chores it fires before WorkStarted
            // so we don't need to act on it. Leaving as a no-op for the
            // skeleton; revisit when we add idle/mingle support.
            _ = dupeInstanceId;
            _ = arrived;
        }

        // ---------- Internals ----------

        private static void TransitionTo(int dupeInstanceId,
                TimelineSegmentKind newKind, int newWorkableId) {
            if (!inflight.TryGetValue(dupeInstanceId, out var open)) return;
            var now = Now();
            FlushOpenSegment(dupeInstanceId, open, now);
            open.SegmentStartTime = now;
            open.SegmentKind = newKind;
            if (newWorkableId >= 0) open.WorkableInstanceId = newWorkableId;
            inflight[dupeInstanceId] = open;
        }

        private static void FlushOpenSegment(int dupeInstanceId,
                OpenChore open, float endTime) {
            if (endTime <= open.SegmentStartTime) return;
            var seg = new TimelineSegment {
                StartTime = open.SegmentStartTime,
                EndTime = endTime,
                Kind = open.SegmentKind,
                ChoreGuid = open.Guid,
                ChoreTypeId = open.ChoreTypeId,
                WorkableInstanceId = open.WorkableInstanceId,
                TargetCell = open.TargetCell,
            };
            GetOrCreate(dupeInstanceId).Add(seg);
        }

        private static TimelineRingBuffer GetOrCreate(int dupeInstanceId) {
            if (!store.TryGetValue(dupeInstanceId, out var buf)) {
                buf = new TimelineRingBuffer(capacity);
                store[dupeInstanceId] = buf;
            }
            return buf;
        }

        private static int WorkableInstanceIdFor(Chore chore) {
            // chore.target is an IStateMachineTarget; many chores point at a
            // Workable directly. Read via its GameObject when available.
            try {
                var w = chore.target as Workable;
                if (w != null) return InstanceIdOf(w.gameObject);
            } catch (Exception e) {
                PUtil.LogExcWarn(e);
            }
            return -1;
        }

        private static int TargetCellFor(Chore chore) {
            // Chore.destination exists on FetchChore and a few other subclasses.
            // For the skeleton, return -1 — we'll resolve target cells later
            // from chore-subclass-specific fields.
            _ = chore;
            return -1;
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
