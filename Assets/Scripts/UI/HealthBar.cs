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
            if (_valueLabel == null) _valueLabel = ResourceBarValue.Create(transform);
        }

        private void Start()
        {
            Refresh(_health.Current, _health.Maximum);
        }

        private void Refresh(float current, float maximum)
        {
            _slider.SetValueWithoutNotify(maximum > 0f ? current / maximum : 0f);
            ResourceBarValue.Refresh(_valueLabel, current, maximum);
        }

        private void OnEnable()
        {
            _health.Changed += Refresh;
            Refresh(_health.Current, _health.Maximum);
        }

        private void OnDisable()
        {
            _health.Changed -= Refresh;
        }
    }
}
