using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DupeTimeline.UI {
    // Shared UI helpers reused between the side-screen panel and the modal.
    // - Fonts.Default: borrows a TMP_FontAsset from anything Klei has loaded
    //   so our text actually renders. Without this, fresh TextMeshProUGUI
    //   components have no font and draw nothing.
    // - Sprites.White: a 1x1 white sprite shared across all colored rects.
    // - MakeText: TextMeshProUGUI with NoWrap and a sane default font.
    // - MakeButton: clickable Image + TextMeshProUGUI label.
    // - NewChild: bare GameObject + RectTransform under a parent.
    internal static class UICommon {
        public static GameObject NewChild(Transform parent, string name) {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        public static TextMeshProUGUI MakeText(Transform parent, string content,
                float size, TextAlignmentOptions align = TextAlignmentOptions.Left) {
            var go = NewChild(parent, "Text");
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = content;
            tmp.fontSize = size;
            tmp.color = Color.white;
            tmp.alignment = align;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            var font = Fonts.Default();
            if (font != null) tmp.font = font;
            return tmp;
        }

        public static Button MakeButton(Transform parent, string label,
                System.Action onClick, float height = 28f) {
            var go = NewChild(parent, "Button");
            var img = go.AddComponent<Image>();
            img.sprite = Sprites.White();
            img.color = new Color(0.27f, 0.32f, 0.42f, 1f);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.normalColor = new Color(0.27f, 0.32f, 0.42f, 1f);
            colors.highlightedColor = new Color(0.36f, 0.42f, 0.55f, 1f);
            colors.pressedColor = new Color(0.20f, 0.24f, 0.32f, 1f);
            colors.selectedColor = colors.highlightedColor;
            btn.colors = colors;
            if (onClick != null) btn.onClick.AddListener(() => onClick());

            var labelTmp = MakeText(go.transform, label, 12f, TextAlignmentOptions.Center);
            var lrt = labelTmp.rectTransform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(8, 0);
            lrt.offsetMax = new Vector2(-8, 0);

            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.flexibleWidth = 1f;
            return btn;
        }

        public static Color ColorOf(TimelineSegmentKind kind) {
            switch (kind) {
                case TimelineSegmentKind.Travel: return new Color(0.96f, 0.65f, 0.18f, 1f);
                case TimelineSegmentKind.Work:   return new Color(0.32f, 0.78f, 0.32f, 1f);
                default:                          return new Color(0.45f, 0.45f, 0.45f, 1f);
            }
        }
    }

    internal static class Fonts {
        private static TMP_FontAsset cached;

        public static TMP_FontAsset Default() {
            if (cached != null) return cached;
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
