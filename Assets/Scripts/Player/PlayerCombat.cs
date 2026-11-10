using FinalVFX.Combat;
using UnityEngine;

namespace FinalVFX.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerEquipment), typeof(PlayerAnimationController))]
    [RequireComponent(typeof(PlayerHitVolumes))]
    public sealed class PlayerCombat : MonoBehaviour
    {
        public enum ActionState { Ready, Basic, Secondary, Ultimate }
        public ActionState State { get; private set; }
        public bool Busy => State != ActionState.Ready;
        public bool AllowsDodge => !Busy || State == ActionState.Basic;

        private PlayerEquipment _equipment;
        private PlayerAnimationController _presentation;
        private PlayerHitVolumes _volumes;
        private float _previousSample;
        private int _step;
        private bool _queued;
        private float _startedAt;
        private float _duration;
        private float _basicAvailableAt;

        private void Awake()
        {
            _volumes = GetComponent<PlayerHitVolumes>();
            _equipment = GetComponent<PlayerEquipment>();
            _presentation = GetComponent<PlayerAnimationController>();
        }

        private void OnEnable() => _equipment.WeaponChanged += OnWeaponChanged;

        private void OnDisable()
        {
            _equipment.WeaponChanged -= OnWeaponChanged;
            ResetCombat();
        }

        private void OnWeaponChanged(WeaponDefinition weapon) => ResetCombat();

        public void ResetCombat()
        {
            EndAction();
            _basicAvailableAt = 0f;
        }

        public void InterruptBasic()
        {
            if (State == ActionState.Basic)
                EndAction();
        }

        private void EndAction()
        {
            if (Busy)
                _presentation.StopCombatClip();

            State = ActionState.Ready;
            _queued = false;
            _step = 0;
        }

        // Se llama antes de decidir qué entrada gana este frame.
        public void Advance(float now)
        {
            if (!Busy)
                return;

            WeaponDefinition weapon = _equipment.EquippedWeapon;
            SampleImpact(weapon, now);
            if (State == ActionState.Basic && _queued)
            {
                var current = weapon.Step(_step);
                float link = Mathf.Clamp(current.link, WindowEnd(current), 1f);
                if (now - _startedAt >= link * _duration)
                {
                    StartStep(weapon, _step + 1, now);
                    return;
                }
            }

            if (now < _startedAt + _duration)
                return;

            if (State == ActionState.Basic)
                _basicAvailableAt = _startedAt + _duration + weapon.BasicRestartDelay;

            EndAction();
        }

        public void ReadPresses(PlayerInputManager input, float now)
        {
            if (!isActiveAndEnabled)
                return;

            WeaponDefinition weapon = _equipment.EquippedWeapon;
            if (!weapon)
                return;

            if (State == ActionState.Basic)
            {
                if (input.BasicPressed && !_queued && _step + 1 < weapon.StepCount)
                {
                    var current = weapon.Step(_step);
                    float progress = (now - _startedAt) / _duration;
                    if (progress >= current.open && progress <= WindowEnd(current))
                    {
                        _queued = true;
                        Advance(now);
                    }
                }
                return;
            }

            if (Busy)
                return;

            if (input.UltimatePressed)
                StartSingle(weapon, weapon.Ultimate, ActionState.Ultimate, now);
            else if (input.SecondaryPressed)
                StartSingle(weapon, weapon.Secondary, ActionState.Secondary, now);
            else if (input.BasicPressed && now >= _basicAvailableAt && weapon.StepCount > 0)
                StartStep(weapon, 0, now);
        }

        private void SampleImpact(WeaponDefinition weapon, float now)
        {
            if (State == ActionState.Ultimate || !weapon)
                return;

            bool basic = State == ActionState.Basic;
            var hit = basic ? weapon.Step(_step).impact : weapon.SecondaryImpact;
            float elapsed = now - _startedAt;
            if (hit != null && hit.damage > 0f)
            {
                float start = hit.from * _duration;
                float end = Mathf.Max(hit.from, hit.until) * _duration;
                float sampleEnd = elapsed;
                if (basic && _queued)
                {
                    var step = weapon.Step(_step);
                    float link = Mathf.Clamp(step.link, WindowEnd(step), 1f);
                    sampleEnd = Mathf.Min(sampleEnd, link * _duration);
                }

                if (sampleEnd >= start && (sampleEnd <= end || _previousSample < start))
                    _volumes.Strike(basic, weapon.PoseType, hit.damage);
            }
            _previousSample = elapsed;
        }

        private void StartImpact(WeaponDefinition weapon, float now)
        {
            _previousSample = 0f;
            _volumes.BeginStrike();
            SampleImpact(weapon, now);
        }

        private static float WindowEnd(WeaponDefinition.BasicStep step) =>
            Mathf.Clamp(step.close, step.open, 1f);

        private void StartStep(WeaponDefinition weapon, int index, float now)
        {
            var step = weapon.Step(index);
            State = ActionState.Basic;
            _step = index;
            _startedAt = now;
            _duration = step.Duration;
            _queued = false;

            StartImpact(weapon, now);
            if (!_presentation.PlayCombatClip(step.clip))
                Debug.Log($"{weapon.DisplayName} · básico {index + 1}/{weapon.StepCount}", this);
        }

        private void StartSingle(WeaponDefinition weapon, WeaponDefinition.SingleAction action,
            ActionState state, float now)
        {
            State = state;
            _startedAt = now;
            _duration = action.lockSeconds;
            if (state == ActionState.Secondary)
                StartImpact(weapon, now);
            if (!_presentation.PlayCombatClip(action.clip))
            {
                string label = state == ActionState.Ultimate ? "ultimate" : "ataque secundario";
                Debug.Log($"{weapon.DisplayName} · {label}", this);
            }
        }
    }
}
