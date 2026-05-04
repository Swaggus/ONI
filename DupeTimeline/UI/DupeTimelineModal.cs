using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DupeTimeline.UI {
    // Fullscreen modal showing the per-dupe Gantt at proper size. The
    // side-screen container is way too narrow for a useful timeline; this
    // opens a centered ~900x420 panel over a dimmer.
    //
    // Hosted on its own Canvas with sortingOrder=9000 so it draws above
    // ONI's UI. Esc, the X button, or a click on the dimmer all dismiss.
    // Refreshes every 0.5s while open so bars/percentages tick live.
    public sealed class DupeTimelineModal : MonoBehaviour {
        private const float SecondsPerCycle = 600f;
        private const int CyclesShown = 3;
        private const float RangeSeconds = SecondsPerCycle * CyclesShown;
        private const float RefreshInterval = 0.5f;

        private static readonly Color StripBg = new Color(0.10f, 0.10f, 0.13f, 1f);
        private static readonly Color CycleDivider = new Color(0.35f, 0.35f, 0.40f, 1f);
        private static readonly Color PanelBg = new Color(0.13f, 0.14f, 0.18f, 1f);
        private static readonly Color HeaderBg = new Color(0.18f, 0.20f, 0.26f, 1f);
        private static readonly Color DimmerColor = new Color(0f, 0f, 0f, 0.6f);

        private static DupeTimelineModal instance;

        private MinionIdentity target;
        private GameObject ganttStrip;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI cycleText;
        private TextMeshProUGUI summaryText;
        private readonly List<RectTransform> barPool = new List<RectTransform>();
        private float lastRefresh;

        public static void Show(GameObject dupeGo) {
            Hide();
            if (dupeGo == null) return;
            var ident = dupeGo.GetComponent<MinionIdentity>();
            if (ident == null) return;

            var canvas = FindMainCanvas();
            if (canvas == null) {
                Log.Warn("could not find a Canvas to host the timeline modal");
                return;
            }

            var rootGo = UICommon.NewChild(canvas.transform, "DupeTimelineModalRoot");
            var modalCanvas = rootGo.AddComponent<Canvas>();
            modalCanvas.overrideSorting = true;
            modalCanvas.sortingOrder = 9000;
            rootGo.AddComponent<GraphicRaycaster>();
            var rrt = rootGo.GetComponent<RectTransform>();
            rrt.anchorMin = Vector2.zero;
            rrt.anchorMax = Vector2.one;
            rrt.offsetMin = Vector2.zero;
            rrt.offsetMax = Vector2.zero;

            instance = rootGo.AddComponent<DupeTimelineModal>();
            instance.target = ident;
            instance.BuildUI(rootGo.transform);
            instance.Refresh();
        }

        public static void Hide() {
            if (instance != null && instance.gameObject != null) {
                Destroy(instance.gameObject);
            }
            instance = null;
        }

        public static bool IsOpen => instance != null;

        private void Update() {
            if (Input.GetKeyDown(KeyCode.Escape)) {
                Hide();
                return;
            }
            if (target == null) return;
            if (Time.unscaledTime - lastRefresh < RefreshInterval) return;
            Refresh();
        }

        private void BuildUI(Transform root) {
            // Dimmer fills the screen and dismisses on click.
            var dimmer = UICommon.NewChild(root, "Dimmer");
            var drt = dimmer.GetComponent<RectTransform>();
            drt.anchorMin = Vector2.zero;
            drt.anchorMax = Vector2.one;
            drt.offsetMin = Vector2.zero;
            drt.offsetMax = Vector2.zero;
            var dimImg = dimmer.AddComponent<Image>();
            dimImg.sprite = Sprites.White();
            dimImg.color = DimmerColor;
            var dimBtn = dimmer.AddComponent<Button>();
            dimBtn.targetGraphic = dimImg;
            dimBtn.transition = Selectable.Transition.None;
            dimBtn.onClick.AddListener(Hide);

            // Centered panel. Has its own Image which sinks raycasts so
            // panel-area clicks don't reach the dimmer.
            var panel = UICommon.NewChild(root, "Panel");
            var prt = panel.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.5f, 0.5f);
            prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(900f, 420f);
            prt.anchoredPosition = Vector2.zero;
            var panelImg = panel.AddComponent<Image>();
            panelImg.sprite = Sprites.White();
            panelImg.color = PanelBg;
            panelImg.raycastTarget = true;

            var vlg = panel.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(0, 0, 0, 16);
            vlg.spacing = 8f;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.UpperCenter;

            BuildHeader(panel.transform);
            BuildBody(panel.transform);
        }

        private void BuildHeader(Transform parent) {
            var header = UICommon.NewChild(parent, "Header");
            var hi = header.AddComponent<Image>();
            hi.sprite = Sprites.White();
            hi.color = HeaderBg;
            var hle = header.AddComponent<LayoutElement>();
            hle.preferredHeight = 36f;
            hle.flexibleWidth = 1f;

            var titleGo = UICommon.MakeLabel(header.transform,
                "Recent Activity", TextAnchor.MiddleLeft);
            titleText = UICommon.TextOf(titleGo);
            var trt = titleGo.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0, 0);
            trt.anchorMax = new Vector2(1, 1);
            trt.offsetMin = new Vector2(16, 0);
            trt.offsetMax = new Vector2(-44, 0);

            // Close X.
            var close = UICommon.NewChild(header.transform, "Close");
            var crt = close.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(1, 0.5f);
            crt.anchorMax = new Vector2(1, 0.5f);
            crt.pivot = new Vector2(1, 0.5f);
            crt.sizeDelta = new Vector2(28, 28);
            crt.anchoredPosition = new Vector2(-6, 0);
            var ci = close.AddComponent<Image>();
            ci.sprite = Sprites.White();
            ci.color = new Color(0.5f, 0.2f, 0.2f, 1f);
            var cb = close.AddComponent<Button>();
            cb.targetGraphic = ci;
            cb.onClick.AddListener(Hide);
            var cTxtGo = UICommon.MakeLabel(close.transform, "X",
                TextAnchor.MiddleCenter);
            var cTrt = cTxtGo.GetComponent<RectTransform>();
            cTrt.anchorMin = Vector2.zero;
            cTrt.anchorMax = Vector2.one;
            cTrt.offsetMin = Vector2.zero;
            cTrt.offsetMax = Vector2.zero;
        }

        private void BuildBody(Transform parent) {
            var body = UICommon.NewChild(parent, "Body");
            var bvlg = body.AddComponent<VerticalLayoutGroup>();
            bvlg.padding = new RectOffset(20, 20, 4, 0);
            bvlg.spacing = 10f;
            bvlg.childForceExpandWidth = true;
            bvlg.childForceExpandHeight = false;
            var ble = body.AddComponent<LayoutElement>();
            ble.flexibleWidth = 1f;

            var cycleGo = UICommon.MakeLabel(body.transform, "Cycle ?");
            cycleText = UICommon.TextOf(cycleGo);
            var cle = cycleGo.AddComponent<LayoutElement>();
            cle.preferredHeight = 18f;

            // Gantt strip
            ganttStrip = UICommon.NewChild(body.transform, "GanttStrip");
            var stripLE = ganttStrip.AddComponent<LayoutElement>();
            stripLE.preferredHeight = 60f;
            stripLE.flexibleWidth = 1f;
            var stripImg = ganttStrip.AddComponent<Image>();
            stripImg.sprite = Sprites.White();
            stripImg.color = StripBg;

            for (int i = 1; i < CyclesShown; i++) {
                var div = UICommon.NewChild(ganttStrip.transform, "Div" + i);
                var rt = div.GetComponent<RectTransform>();
                float x = (float)i / CyclesShown;
                rt.anchorMin = new Vector2(x, 0);
                rt.anchorMax = new Vector2(x, 1);
                rt.offsetMin = new Vector2(-1f, 0);
                rt.offsetMax = new Vector2(1f, 0);
                var img = div.AddComponent<Image>();
                img.sprite = Sprites.White();
                img.color = CycleDivider;
            }

            // Legend row
            var legend = UICommon.NewChild(body.transform, "Legend");
            var lhlg = legend.AddComponent<HorizontalLayoutGroup>();
            lhlg.spacing = 18f;
            lhlg.childForceExpandWidth = false;
            lhlg.childForceExpandHeight = false;
            lhlg.childAlignment = TextAnchor.MiddleLeft;
            var lle = legend.AddComponent<LayoutElement>();
            lle.preferredHeight = 22f;
            AddLegendItem(legend.transform, TimelineSegmentKind.Travel, "Travel");
            AddLegendItem(legend.transform, TimelineSegmentKind.Work, "Work");
            AddLegendItem(legend.transform, TimelineSegmentKind.Idle, "Idle");

            var summaryGo = UICommon.MakeLabel(body.transform, "—");
            summaryText = UICommon.TextOf(summaryGo);
            var sle = summaryGo.AddComponent<LayoutElement>();
            sle.preferredHeight = 20f;
        }

        private void AddLegendItem(Transform parent, TimelineSegmentKind kind, string label) {
            var item = UICommon.NewChild(parent, "Legend_" + label);
            var hlg = item.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6f;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.childAlignment = TextAnchor.MiddleLeft;

            var swatch = UICommon.NewChild(item.transform, "Swatch");
            var sle = swatch.AddComponent<LayoutElement>();
            sle.preferredWidth = 14;
            sle.preferredHeight = 14;
            var simg = swatch.AddComponent<Image>();
            simg.sprite = Sprites.White();
            simg.color = UICommon.ColorOf(kind);

            var lbl = UICommon.MakeLabel(item.transform, label);
            var lble = lbl.AddComponent<LayoutElement>();
            lble.preferredWidth = 60f;
            lble.preferredHeight = 16f;
        }

        private void Refresh() {
            lastRefresh = Time.unscaledTime;
            if (target == null || ganttStrip == null) return;
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

            if (titleText != null) {
                titleText.text = target.GetProperName() + " — Recent Activity";
            }
            if (cycleText != null) {
                cycleText.text = startCycle == endCycle
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
                float dur = clampedEnd - clampedStart;
                if (dur <= 0) continue;

                float xMin = (clampedStart - rangeStart) / RangeSeconds;
                float xMax = (clampedEnd - rangeStart) / RangeSeconds;

                var rt = GetOrAllocateBar(barIdx);
                rt.anchorMin = new Vector2(xMin, 0);
                rt.anchorMax = new Vector2(xMax, 1);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                rt.GetComponent<Image>().color = UICommon.ColorOf(seg.Kind);
                rt.gameObject.SetActive(true);
                barIdx++;

                if (seg.Kind == TimelineSegmentKind.Travel) travel += dur;
                else if (seg.Kind == TimelineSegmentKind.Work) work += dur;
                totalInRange += dur;
            }

            if (summaryText != null) {
                if (totalInRange <= 0f) {
                    summaryText.text = "No data yet — give the dupe a few minutes to chore.";
                } else {
                    float idleSec = Mathf.Max(0f, RangeSeconds - totalInRange);
                    float denom = totalInRange + idleSec;
                    int pTravel = Mathf.RoundToInt(travel / denom * 100f);
                    int pWork = Mathf.RoundToInt(work / denom * 100f);
                    int pIdle = Mathf.RoundToInt(idleSec / denom * 100f);
                    summaryText.text =
                        $"Travel {pTravel}%     Work {pWork}%     Idle {pIdle}%";
                }
            }
        }

        private RectTransform GetOrAllocateBar(int idx) {
            while (barPool.Count <= idx) {
                var go = UICommon.NewChild(ganttStrip.transform, "Bar" + barPool.Count);
                var img = go.AddComponent<Image>();
                img.sprite = Sprites.White();
                barPool.Add(go.GetComponent<RectTransform>());
            }
            return barPool[idx];
        }

        private static Canvas FindMainCanvas() {
            if (DetailsScreen.Instance != null) {
                Canvas best = null;
                var t = DetailsScreen.Instance.transform;
                while (t != null) {
                    var c = t.GetComponent<Canvas>();
                    if (c != null) best = c;
                    t = t.parent;
                }
                if (best != null) return best;
            }
            return Object.FindObjectOfType<Canvas>();
        }
    }
}
