using UnityEngine;
using TMPro;

namespace FinalVFX.UI
{
    // Updates scene-authored labels without changing their presentation.
    internal static class ResourceBarValue
    {
        public static void Refresh(TMP_Text label, float current, float maximum)
        {
            int max = Mathf.CeilToInt(Mathf.Max(0f, maximum));
            int value = Mathf.Clamp(Mathf.CeilToInt(current), 0, max);
            string content = $"{value}/{max}";
            if (label.text != content) label.text = content;
        }
    }
}
