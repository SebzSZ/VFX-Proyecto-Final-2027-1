using FinalVFX.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace FinalVFX
{
    [RequireComponent(typeof(Slider))]
    public class StaminaBar : MonoBehaviour
    {
        [SerializeField] private StaminaComponent _stamina;
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
            if (_stamina != null)
            {
                Refresh(_stamina.Current, _stamina.Maximum);
            }
        }

        private void Refresh(float current, float maximum)
        {
            _slider.SetValueWithoutNotify(maximum > 0f ? current / maximum : 0f);
        }

        private void OnEnable()
        {
            if (_stamina == null) return;

            _stamina.Changed += Refresh;
            Refresh(_stamina.Current, _stamina.Maximum);
        }

        private void OnDisable()
        {
            if (_stamina != null)
            {
                _stamina.Changed -= Refresh;
            }
        }
    }
}
