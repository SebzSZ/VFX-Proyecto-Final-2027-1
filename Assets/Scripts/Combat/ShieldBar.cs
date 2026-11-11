using FinalVFX.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace FinalVFX.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public sealed class ShieldBar : MonoBehaviour
    {
        [SerializeField] private HealthComponent _health;
        private Image _fill;

        private void Awake()
        {
            _fill = GetComponent<Image>();
            if (!_health)
            {
                Debug.LogError("Asigna la salud del jugador a Shield Bar.", this);
                enabled = false;
                return;
            }
            _fill.type = Image.Type.Filled;
            _fill.fillMethod = Image.FillMethod.Horizontal;
            _fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            _fill.raycastTarget = false;
        }

        private void OnEnable()
        {
            _health.ShieldChanged += Refresh;
            Refresh(_health.Shield, _health.Maximum);
        }

        private void Start() => Refresh(_health.Shield, _health.Maximum);

        private void OnDisable()
        {
            if (_health)
                _health.ShieldChanged -= Refresh;
        }

        private void Refresh(float shield, float maximum)
        {
            _fill.fillAmount = maximum > 0f ? Mathf.Clamp01(shield / maximum) : 0f;
            _fill.enabled = shield > 0f;
        }
    }
}
