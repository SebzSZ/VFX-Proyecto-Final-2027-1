using FinalVFX.Combat;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FinalVFX
{
    [RequireComponent(typeof(Slider))]
    public class StaminaBar : MonoBehaviour
    {
        [SerializeField] private StaminaComponent _stamina;
        [SerializeField] private TMP_Text _valueLabel;

        private Slider _slider;

        private void Awake()
        {
            _slider = GetComponent<Slider>();
            if (_stamina == null || _valueLabel == null)
            {
                Debug.LogError($"{name}: assign Stamina and Value Label in the Inspector.", this);
                enabled = false;
            }
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
            if (_stamina == null) return;
            _stamina.Changed -= Refresh;
        }

        private void Refresh(float current, float maximum)
        {
            _slider.SetValueWithoutNotify(maximum > 0f ? current / maximum : 0f);
            UI.ResourceBarValue.Refresh(_valueLabel, current, maximum);
        }
    }
}
