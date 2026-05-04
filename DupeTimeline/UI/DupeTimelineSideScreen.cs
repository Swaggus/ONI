using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DupeTimeline.UI {
    // Per-dupe activity panel. Appears in the right-hand details strip when
    // a duplicant is selected. v1 shows the most recent N segments as text
    // rows; the Gantt visualization layers in later by replacing the row
    // factory.
    //
    // Lifecycle (DetailsScreen drives this — never invoke manually):
    //   OnPrefabInit  -> build UI once, allocate row pool
    //   IsValidForTarget(go) -> gate visibility (only for MinionIdentity)
    //   SetTarget(go) -> bind to the selected dupe, refresh rows
    //   ClearTarget() -> unsubscribe handlers, hide rows
    //
    // Real mods use SetTarget every selection — keep it cheap. UI is built
    // once in OnPrefabInit; SetTarget only mutates text on existing rows.
    public sealed class DupeTimelineSideScreen : SideScreenContent {
        private const int MaxRows = 20;

        private MinionIdentity target;
        private GameObject rowContainer;
        private readonly List<LocText> rows = new List<LocText>();

        public override string GetTitle() {
            return "Recent Activity";
        }

        public override bool IsValidForTarget(GameObject t) {
            return t != null && t.GetComponent<MinionIdentity>() != null;
        }

        protected override void OnPrefabInit() {
            base.OnPrefabInit();

            rowContainer = new GameObject("DupeTimelineRows");
            rowContainer.transform.SetParent(ContentContainer.transform, false);

            var vlg = rowContainer.AddComponent<VerticalLayoutGroup>();
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 2f;
            vlg.padding = new RectOffset(4, 4, 4, 4);

            var fitter = rowContainer.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            for (int i = 0; i < MaxRows; i++) {
                var rowGo = new GameObject("Row" + i);
                rowGo.transform.SetParent(rowContainer.transform, false);
                var lt = rowGo.AddComponent<LocText>();
                lt.fontSize = 12f;
                lt.alignment = TMPro.TextAlignmentOptions.Left;
                lt.text = string.Empty;
                rows.Add(lt);
                rowGo.SetActive(false);
            }
        }

        public override void SetTarget(GameObject t) {
            if (t == null) return;
            target = t.GetComponent<MinionIdentity>();
            Refresh();
        }

        public override void ClearTarget() {
            target = null;
            for (int i = 0; i < rows.Count; i++) {
                if (rows[i] != null) rows[i].gameObject.SetActive(false);
            }
        }

        private void Refresh() {
            if (target == null) return;
            var pid = target.GetComponent<KPrefabID>();
            if (pid == null) return;

            var segments = TimelineStore.GetRecent(pid.InstanceID, MaxRows);
            // Render newest at the top.
            segments.Reverse();

            for (int i = 0; i < rows.Count; i++) {
                if (i < segments.Count) {
                    rows[i].text = Format(segments[i]);
                    rows[i].gameObject.SetActive(true);
                } else {
                    rows[i].gameObject.SetActive(false);
                }
            }
        }

        private static string Format(TimelineSegment seg) {
            string kind = seg.Kind.ToString().PadRight(6);
            string chore = string.IsNullOrEmpty(seg.ChoreTypeId) ? "?" : seg.ChoreTypeId;
            if (chore.Length > 22) chore = chore.Substring(0, 22);
            return string.Format("{0} {1,5:F0}s  {2}",
                kind, seg.Duration, chore);
        }
    }
}
