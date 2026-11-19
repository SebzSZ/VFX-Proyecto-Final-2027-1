using TMPro;
using UnityEngine;

namespace FinalVFX.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TextMeshProUGUI), typeof(CanvasGroup))]
    public sealed class LoadingIndicator : MonoBehaviour
    {
        [SerializeField, Min(0.05f)] private float _dotSeconds = 0.35f;
        private TMP_Text _label;
        private CanvasGroup _group;
        private float _elapsed;
        private bool _visible;

        private void Awake()
        {
            Initialize();
            SetVisible(false);
        }

        private void Initialize()
        {
            if (!_label) _label = GetComponent<TextMeshProUGUI>();
            if (!_group) _group = GetComponent<CanvasGroup>();
            _label.text = "Cargando...";
            _label.raycastTarget = false;
            _group.interactable = false;
            _group.blocksRaycasts = false;
        }

        public void SetVisible(bool visible)
        {
            Initialize();
            _visible = visible;
            _elapsed = 0;
            // Keep the full layout; only reveal the dots, so the word does not shift.
            _label.maxVisibleCharacters = 9;
            _group.alpha = visible ? 1 : 0;
        }

        private void Update()
        {
            if (!_visible) return;
            _elapsed += Time.unscaledDeltaTime;
            int dots = 1 + Mathf.FloorToInt(_elapsed / Mathf.Max(0.05f, _dotSeconds)) % 3;
            _label.maxVisibleCharacters = 8 + dots;
        }

        private void OnDisable()
        {
            _visible = false;
            if (_group) _group.alpha = 0;
        }
    }
}
