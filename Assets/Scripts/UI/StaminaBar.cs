using FinalVFX.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace FinalVFX
{
    [RequireComponent(typeof(Slider))]
    public class StaminaBar : MonoBehaviour
    {
        [SerializeField] private StaminaComponent _stamina;
        [SerializeField] private Text _valueLabel;

        private Slider _slider;

        private void Awake()
        {
            _slider = GetComponent<Slider>();
            _slider.minValue = 0f;
            _slider.maxValue = 1f;
            _slider.wholeNumbers = false;
            _slider.interactable = false;
            if (_valueLabel == null) _valueLabel = UI.ResourceBarValue.Create(transform);
        }

        private void OnEnable()
        {
            _stamina.Changed += Refresh;
            Refresh(_stamina.Current, _stamina.Maximum);
        }

        private void Start()
        {
            // En OnEnable la stamina puede no haber pasado por su Awake todavía, así que se refresca de nuevo
            Refresh(_stamina.Current, _stamina.Maximum);
        }

        private void OnDisable()
        {
            _stamina.Changed -= Refresh;
        }

        private void Refresh(float current, float maximum)
        {
            _slider.SetValueWithoutNotify(maximum > 0f ? current / maximum : 0f);
            UI.ResourceBarValue.Refresh(_valueLabel, current, maximum);
        }
    }
}
