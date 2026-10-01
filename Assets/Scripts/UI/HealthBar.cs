using FinalVFX.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace FinalVFX.UI
{
    [RequireComponent(typeof(Slider))]
    public class HealthBar : MonoBehaviour
    {
        [SerializeField] private HealthComponent _health;
        private Slider _slider;

        private void Awake()
        {
            _slider = GetComponent<Slider>();
            _slider.minValue = 0f;
            _slider.maxValue = 1f;
            _slider.wholeNumbers = false;
            _slider.interactable = false;
        }

        private void Start()
        {
            Refresh(_health.Current, _health.Maximum);
        }

        private void Refresh(float current, float maximum)
        {
            _slider.SetValueWithoutNotify(current / maximum);
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
