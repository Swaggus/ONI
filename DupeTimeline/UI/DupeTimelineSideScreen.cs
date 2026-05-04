using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DupeTimeline.UI {
    // Per-dupe activity panel in the right-hand details strip. The strip is
    // narrow (~200px) — way too cramped for a real Gantt, so this just shows
    // a one-line summary plus an "Open Timeline View" button. The button
    // pops a fullscreen modal (DupeTimelineModal) where the actual Gantt
    // gets enough horizontal space to be readable.
    public sealed class DupeTimelineSideScreen : SideScreenContent {
        private const float SecondsPerCycle = 600f;
        private const int CyclesShown = 3;
        private const float RangeSeconds = SecondsPerCycle * CyclesShown;
        private const float RefreshInterval = 1f;

        private MinionIdentity target;
        private TextMeshProUGUI cycleLabel;
        private TextMeshProUGUI summaryLabel;
        private float lastRefresh;

        public override string GetTitle() {
            return "Recent Activity";
        }

        public override bool IsValidForTarget(GameObject t) {
            return t != null && t.GetComponent<MinionIdentity>() != null;
        }

        protected override void OnPrefabInit() {
            base.OnPrefabInit();
            BuildUI();
        }

        public override void SetTarget(GameObject t) {
            if (t == null) return;
            target = t.GetComponent<MinionIdentity>();
            Refresh();
        }

        public override void ClearTarget() {
            target = null;
        }

        private void Update() {
            if (target == null) return;
            if (Time.unscaledTime - lastRefresh < RefreshInterval) return;
            Refresh();
        }

        private void BuildUI() {
            var content = ContentContainer != null
                ? ContentContainer.transform
                : transform;

            var root = UICommon.NewChild(content, "Root");
            var vlg = root.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 6f;
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.UpperLeft;

            cycleLabel = UICommon.MakeText(root.transform, "Cycle ?", 12f);
            var cle = cycleLabel.gameObject.AddComponent<LayoutElement>();
            cle.preferredHeight = 18f;

            summaryLabel = UICommon.MakeText(root.transform, "—", 12f);
            var sle = summaryLabel.gameObject.AddComponent<LayoutElement>();
            sle.preferredHeight = 18f;

            UICommon.MakeButton(root.transform, "Open Timeline View",
                OpenModal, height: 30f);
        }

        private void OpenModal() {
            if (target == null) return;
            DupeTimelineModal.Show(target.gameObject);
        }

        private void Refresh() {
            lastRefresh = Time.unscaledTime;
            if (target == null) return;
            var pid = target.GetComponent<KPrefabID>();
            if (pid == null) return;

            var segs = TimelineStore.GetRecent(pid.InstanceID, 1000);
            float now = GameClock.Instance != null
                ? GameClock.Instance.GetTime()
                : Time.time;
            float rangeEnd = now;
            float rangeStart = rangeEnd - RangeSeconds;

            int endCycle = Mathf.FloorToInt(rangeEnd / SecondsPerCycle) + 1;
            int startCycle = endCycle - CyclesShown + 1;

            float travel = 0f, work = 0f, totalInRange = 0f;
            foreach (var seg in segs) {
                if (seg.EndTime <= rangeStart) continue;
                if (seg.StartTime >= rangeEnd) continue;
                float clampedStart = Mathf.Max(seg.StartTime, rangeStart);
                float clampedEnd = Mathf.Min(seg.EndTime, rangeEnd);
                float dur = clampedEnd - clampedStart;
                if (dur <= 0) continue;
                if (seg.Kind == TimelineSegmentKind.Travel) travel += dur;
                else if (seg.Kind == TimelineSegmentKind.Work) work += dur;
                totalInRange += dur;
            }

            if (cycleLabel != null) {
                cycleLabel.text = startCycle == endCycle
                    ? $"Cycle {endCycle}"
                    : $"Cycle {startCycle} - {endCycle}";
            }

            if (summaryLabel != null) {
                if (totalInRange <= 0f) {
                    summaryLabel.text = "No data yet";
                } else {
                    float idleSec = Mathf.Max(0f, RangeSeconds - totalInRange);
                    float denom = totalInRange + idleSec;
                    int pTravel = Mathf.RoundToInt(travel / denom * 100f);
                    int pWork = Mathf.RoundToInt(work / denom * 100f);
                    int pIdle = Mathf.RoundToInt(idleSec / denom * 100f);
                    summaryLabel.text = $"T {pTravel}%  W {pWork}%  I {pIdle}%";
                }
            }
        }
    }
}
