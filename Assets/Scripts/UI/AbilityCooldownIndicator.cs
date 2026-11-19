using FinalVFX.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FinalVFX.UI
{
    [DisallowMultipleComponent]
    public sealed class AbilityCooldownIndicator : MonoBehaviour
    {
        [SerializeField] private PlayerCombat _combat;
        [SerializeField] private PlayerCombat.Ability _ability;
        [SerializeField] private Image _cover;
        [SerializeField] private TMP_Text _timeLabel;
        [Tooltip("Al estar lista, muestra la duración configurada. Desactivado muestra 0.")]
        [SerializeField] private bool _showDurationWhenReady = true;

        private PlayerEquipment _equipment;

        private void Awake()
        {
            if (!_combat || !_cover || !_cover.sprite || !_timeLabel)
            {
                enabled = false;
                return;
            }

            _equipment = _combat.GetComponent<PlayerEquipment>();
            _cover.type = Image.Type.Filled;
            _cover.fillMethod = Image.FillMethod.Radial360;
            _cover.fillOrigin = (int)Image.Origin360.Top;
            _cover.fillClockwise = true;
            _cover.raycastTarget = false;
            _timeLabel.raycastTarget = false;
        }

        private void OnEnable()
        {
            _timeLabel.enabled = true;
            Refresh();
        }

        private void LateUpdate() => Refresh();

        private void Refresh()
        {
            _combat.GetCooldown(_ability, out float remaining, out float remainingFraction);
            _cover.fillAmount = 1f - Mathf.Clamp01(remainingFraction);

            float seconds = remaining;
            if (remaining <= 0f && _showDurationWhenReady && _equipment && _equipment.EquippedWeapon)
            {
                var weapon = _equipment.EquippedWeapon;
                seconds = _ability == PlayerCombat.Ability.Ultimate
                    ? weapon.UltimateCooldown
                    : weapon.SecondaryCooldown;
                
                _timeLabel.fontSize = 75;
                _timeLabel.SetText("Ready");

                return;
            }

            _timeLabel.fontSize = 125;
            if (seconds > 0f && seconds < 1f)
                _timeLabel.SetText("{0:1}", Mathf.Ceil(seconds * 10f) / 10f);
            else
                _timeLabel.SetText("{0}", Mathf.Ceil(seconds));
        }
    }
}