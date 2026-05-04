using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DupeTimeline.UI {
    // Per-dupe activity Gantt panel. Appears in the right-hand details strip
    // when a duplicant is selected. Renders the most recent N cycles of
    // segments as proportional colored rectangles on a time axis:
    //
    //   ┌─ Cycle 14 - 16 ──────────────────────────────────┐
    //   │ ████░░██████░░░██████░░░░░░░██████████░░░██░░██░ │  Gantt strip
    //   │ ▮ Travel 24%   ▮ Work 71%   ▮ Idle 5%            │  legend + summary
    //   └──────────────────────────────────────────────────┘
    //
    // Colors are flat (Travel = amber, Work = green, Idle = gray). Bars are
    // pooled across refreshes so we don't allocate UI per selection.
    public sealed class DupeTimelineSideScreen : SideScreenContent {
        private const float SecondsPerCycle = 600f;
        private const int CyclesShown = 3;
        private const float RangeSeconds = SecondsPerCycle * CyclesShown;
        private const float StripHeight = 32f;
        private const float RefreshInterval = 0.5f;

        private static readonly Color TravelColor = new Color(0.96f, 0.65f, 0.18f, 1f);
        private static readonly Color WorkColor = new Color(0.32f, 0.78f, 0.32f, 1f);
        private static readonly Color IdleColor = new Color(0.45f, 0.45f, 0.45f, 1f);
        private static readonly Color StripBg = new Color(0.12f, 0.12f, 0.12f, 1f);
        private static readonly Color CycleDivider = new Color(0.35f, 0.35f, 0.35f, 1f);

        private MinionIdentity target;
        private GameObject ganttStrip;
        private TextMeshProUGUI titleLabel;
        private TextMeshProUGUI summaryLabel;
        private readonly List<RectTransform> barPool = new List<RectTransform>();
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
            foreach (var bar in barPool) {
                if (bar != null) bar.gameObject.SetActive(false);
            }
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

            var root = NewChild(content, "Root");
            var rootVlg = root.AddComponent<VerticalLayoutGroup>();
            rootVlg.spacing = 4f;
            rootVlg.padding = new RectOffset(8, 8, 8, 8);
            rootVlg.childForceExpandWidth = true;
            rootVlg.childForceExpandHeight = false;

            titleLabel = MakeText(root.transform, "Recent Activity", 12);

            ganttStrip = NewChild(root.transform, "GanttStrip");
            var stripLE = ganttStrip.AddComponent<LayoutElement>();
            stripLE.preferredHeight = StripHeight;
            stripLE.flexibleWidth = 1f;
            var stripImg = ganttStrip.AddComponent<Image>();
            stripImg.sprite = Sprites.White();
            stripImg.color = StripBg;

            // Cycle dividers — vertical lines at each cycle boundary.
            for (int i = 1; i < CyclesShown; i++) {
                var div = NewChild(ganttStrip.transform, "Div" + i);
                var rt = div.GetComponent<RectTransform>();
                float x = (float)i / CyclesShown;
                rt.anchorMin = new Vector2(x, 0);
                rt.anchorMax = new Vector2(x, 1);
                rt.offsetMin = new Vector2(-0.5f, 0);
                rt.offsetMax = new Vector2(0.5f, 0);
                var img = div.AddComponent<Image>();
                img.sprite = Sprites.White();
                img.color = CycleDivider;
            }

            summaryLabel = MakeText(root.transform, "—", 11);

            // Legend.
            var legend = NewChild(root.transform, "Legend");
            var legHlg = legend.AddComponent<HorizontalLayoutGroup>();
            legHlg.spacing = 10f;
            legHlg.childForceExpandWidth = false;
            legHlg.childForceExpandHeight = false;
            AddLegendItem(legend.transform, TravelColor, "Travel");
            AddLegendItem(legend.transform, WorkColor, "Work");
            AddLegendItem(legend.transform, IdleColor, "Idle");
        }

        private void Refresh() {
            lastRefresh = Time.unscaledTime;
            if (target == null || ganttStrip == null) return;

            var pid = target.GetComponent<KPrefabID>();
            if (pid == null) return;
            var segs = TimelineStore.GetRecent(pid.InstanceID, 500);

            float now = GameClock.Instance != null
                ? GameClock.Instance.GetTime()
                : Time.time;
            float rangeEnd = now;
            float rangeStart = rangeEnd - RangeSeconds;

            // Cycle numbers: ONI's first cycle is Cycle 1 (time 0..600).
            int endCycle = Mathf.FloorToInt(rangeEnd / SecondsPerCycle) + 1;
            int startCycle = endCycle - CyclesShown + 1;
            if (titleLabel != null) {
                titleLabel.text = startCycle == endCycle
                    ? $"Cycle {endCycle}"
                    : $"Cycle {startCycle} - {endCycle}";
            }

            foreach (var bar in barPool) bar.gameObject.SetActive(false);

            float travel = 0f, work = 0f, totalInRange = 0f;
            int barIdx = 0;
            foreach (var seg in segs) {
                if (seg.EndTime <= rangeStart) continue;
                if (seg.StartTime >= rangeEnd) continue;

                float clampedStart = Mathf.Max(seg.StartTime, rangeStart);
                float clampedEnd = Mathf.Min(seg.EndTime, rangeEnd);
                float clampedDur = clampedEnd - clampedStart;
                if (clampedDur <= 0) continue;

                float xMin = (clampedStart - rangeStart) / RangeSeconds;
                float xMax = (clampedEnd - rangeStart) / RangeSeconds;

                var rt = GetOrAllocateBar(barIdx);
                rt.anchorMin = new Vector2(xMin, 0);
                rt.anchorMax = new Vector2(xMax, 1);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                rt.GetComponent<Image>().color = ColorOf(seg.Kind);
                rt.gameObject.SetActive(true);
                barIdx++;

                if (seg.Kind == TimelineSegmentKind.Travel) travel += clampedDur;
                else if (seg.Kind == TimelineSegmentKind.Work) work += clampedDur;
                totalInRange += clampedDur;
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
                    summaryLabel.text = $"Travel {pTravel}%   Work {pWork}%   Idle {pIdle}%";
                }
            }
        }

        private RectTransform GetOrAllocateBar(int idx) {
            while (barPool.Count <= idx) {
                var go = NewChild(ganttStrip.transform, "Bar" + barPool.Count);
                var img = go.AddComponent<Image>();
                img.sprite = Sprites.White();
                barPool.Add(go.GetComponent<RectTransform>());
            }
            return barPool[idx];
        }

        private static Color ColorOf(TimelineSegmentKind k) {
            switch (k) {
                case TimelineSegmentKind.Travel: return TravelColor;
                case TimelineSegmentKind.Work: return WorkColor;
                default: return IdleColor;
            }
        }

        private void AddLegendItem(Transform parent, Color color, string label) {
            var item = NewChild(parent, "Legend_" + label);
            var hlg = item.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 4f;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            var swatch = NewChild(item.transform, "Swatch");
            var sle = swatch.AddComponent<LayoutElement>();
            sle.preferredWidth = 12; sle.preferredHeight = 12;
            var simg = swatch.AddComponent<Image>();
            simg.sprite = Sprites.White();
            simg.color = color;

            MakeText(item.transform, label, 10);
        }

        private static GameObject NewChild(Transform parent, string name) {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static TextMeshProUGUI MakeText(Transform parent, string content, float size) {
            var go = NewChild(parent, "Text");
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = content;
            tmp.fontSize = size;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Left;
            var font = Fonts.Default();
            if (font != null) tmp.font = font;
            return tmp;
        }
    }

    internal static class Fonts {
        private static TMP_FontAsset cached;

        public static TMP_FontAsset Default() {
            if (cached != null) return cached;
            // FindObjectsOfTypeAll includes inactive objects and loaded assets.
            // ONI bundles several fonts; pick whichever loads first.
            cached = Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault();
            if (cached == null) {
                Log.Warn("could not locate TMP_FontAsset; text labels will not render");
            }
            return cached;
        }
    }

    internal static class Sprites {
        private static Sprite white;

        public static Sprite White() {
            if (white != null) return white;
            var tex = Texture2D.whiteTexture;
            white = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f));
            return white;
        }
    }
}
