using PeterHan.PLib.UI;
using TMPro;
using UnityEngine;

namespace DupeTimeline.UI {
    // Shared UI helpers. After ILRepack folds PLib into our DLL, we use
    // PLib's PLabel/PButton/PUITuning so text picks up Klei's curated
    // fonts and colors instead of trying to derive them from raw
    // TextMeshProUGUI components (which look unstyled and wrap badly).
    //
    // PLabel/PButton return GameObjects with a child LocText/Button already
    // wired up. To mutate the text on refresh, find the TMP_Text via
    // TextOf(go).
    internal static class UICommon {
        public static GameObject NewChild(Transform parent, string name) {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        public static GameObject MakeLabel(Transform parent, string text,
                TextAnchor align = TextAnchor.MiddleLeft) {
            var label = new PLabel("Label") {
                Text = text,
                TextStyle = PUITuning.Fonts.UILightStyle,
                TextAlignment = align,
            };
            var go = label.Build();
            go.transform.SetParent(parent, false);
            return go;
        }

        public static GameObject MakeButton(Transform parent, string label,
                System.Action onClick) {
            var btn = new PButton("Button") {
                Text = label,
                OnClick = _ => {
                    try { onClick?.Invoke(); }
                    catch (System.Exception e) { Log.Exc(e); }
                },
            }.SetKleiBlueStyle();
            var go = btn.Build();
            go.transform.SetParent(parent, false);
            return go;
        }

        public static TextMeshProUGUI TextOf(GameObject labelGo) {
            return labelGo != null
                ? labelGo.GetComponentInChildren<TextMeshProUGUI>()
                : null;
        }

        public static Color ColorOf(TimelineSegmentKind kind) {
            switch (kind) {
                case TimelineSegmentKind.Travel: return new Color(0.96f, 0.65f, 0.18f, 1f);
                case TimelineSegmentKind.Work:   return new Color(0.32f, 0.78f, 0.32f, 1f);
                default:                          return new Color(0.45f, 0.45f, 0.45f, 1f);
            }
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
