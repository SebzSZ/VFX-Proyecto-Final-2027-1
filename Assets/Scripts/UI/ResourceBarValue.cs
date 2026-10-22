using UnityEngine;
using TMPro;

namespace FinalVFX.UI
{
    // Shared presentation for health and stamina; values still come from their events.
    internal static class ResourceBarValue
    {
        public static TMP_Text Create(Transform parent)
        {
            var go = new GameObject("ValueLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var text = go.GetComponent<TextMeshProUGUI>();
            text.font = Resources.Load<TMP_FontAsset>("Fonts/LegacyRuntime SDF");
            text.fontSize = 18;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;
            text.enableAutoSizing = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.fontFeatures.Clear();
            text.outlineColor = new Color(0f, 0f, 0f, 0.9f);
            // Match the former one-pixel outline using the SDF atlas scale.
            text.outlineWidth = text.font.faceInfo.pointSize /
                (text.fontSize * (text.font.atlasPadding + 1f));
            text.extraPadding = true;
            return text;
        }

        public static void Refresh(TMP_Text label, float current, float maximum)
        {
            int max = Mathf.CeilToInt(Mathf.Max(0f, maximum));
            int value = Mathf.Clamp(Mathf.CeilToInt(current), 0, max);
            string content = $"{value}/{max}";
            if (label.text != content) label.text = content;
        }
    }
}
