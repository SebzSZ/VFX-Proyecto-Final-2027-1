using UnityEngine;
using UnityEngine.UI;

namespace FinalVFX.UI
{
    // Shared presentation for health and stamina; values still come from their events.
    internal static class ResourceBarValue
    {
        public static Text Create(Transform parent)
        {
            var go = new GameObject("ValueLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 18;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(1f, -1f);
            return text;
        }

        public static void Refresh(Text label, float current, float maximum)
        {
            int max = Mathf.CeilToInt(Mathf.Max(0f, maximum));
            int value = Mathf.Clamp(Mathf.CeilToInt(current), 0, max);
            string content = $"{value}/{max}";
            if (label.text != content) label.text = content;
        }
    }
}
