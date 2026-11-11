using FinalVFX.Combat;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FinalVFX.UI
{
    [RequireComponent(typeof(Slider))]
    public class HealthBar : MonoBehaviour
    {
        [SerializeField] private HealthComponent _health;
        [SerializeField] private TMP_Text _valueLabel;
        private Slider _slider;

        private void Awake()
        {
            _slider = GetComponent<Slider>();
            _slider.minValue = 0f;
            _slider.maxValue = 1f;
            _slider.wholeNumbers = false;
            _slider.interactable = false;
            if (_valueLabel == null)
                _valueLabel = ResourceBarValue.Create(transform);
        }

        private void Start() => Refresh();

        private void Refresh()
        {
            float maximum = _health.Maximum;
            _slider.SetValueWithoutNotify(maximum > 0f ? _health.Current / maximum : 0f);

            int total = Mathf.CeilToInt(Mathf.Max(0f, _health.Current + _health.Shield));
            int max = Mathf.CeilToInt(Mathf.Max(0f, maximum));
            string content = $"{total}/{max}";
            if (_valueLabel.text != content)
                _valueLabel.text = content;
        }

        private void HandleResourceChanged(float value, float maximum) => Refresh();

        private void OnEnable()
        {
            _health.Changed += HandleResourceChanged;
            _health.ShieldChanged += HandleResourceChanged;
            Refresh();
        }

        private void OnDisable()
        {
            _health.Changed -= HandleResourceChanged;
            _health.ShieldChanged -= HandleResourceChanged;
        }
    }
}