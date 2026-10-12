using System;
using FinalVFX.Combat;
using UnityEngine;

namespace FinalVFX.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerMotor))]
    [RequireComponent(typeof(StaminaComponent))]
    [RequireComponent(typeof(HealthComponent))]
    public class PlayerDodge : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _staminaCost = 20f;
        [SerializeField, Min(0.01f)] private float _duration = 0.5f;
        [SerializeField, Min(0f)] private float _distance = 3.5f;
        [SerializeField, Min(0f)] private float _invulnerabilityStart = 0.05f;
        [SerializeField, Min(0f)] private float _invulnerabilityDuration = 0.25f;

        private PlayerMotor _motor;
        private StaminaComponent _stamina;
        private HealthComponent _health;
        private InvulnerabilityComponent _invulnerability;
        private Vector3 _direction;
        private float _endsAt;
        private float _lastMovementAt;

        public bool IsDodging { get; private set; }
        public float Duration => _duration;

        public event Action Started;
        public event Action Ended;

        private void Awake()
        {
            _motor = GetComponent<PlayerMotor>();
            _stamina = GetComponent<StaminaComponent>();
            _health = GetComponent<HealthComponent>();
            _invulnerability = GetComponent<InvulnerabilityComponent>();
        }

        public bool TryStart(Vector3 direction)
        {
            if (!isActiveAndEnabled || IsDodging || _health.IsDead || !_motor.IsGrounded)
                return false;

            if (!_stamina.TrySpend(_staminaCost))
                return false;

            _direction = Vector3.ProjectOnPlane(direction, Vector3.up);
            _direction = _direction.sqrMagnitude > 0.0001f ? _direction.normalized : transform.forward;
            transform.rotation = Quaternion.LookRotation(_direction, Vector3.up);

            IsDodging = true;
            _lastMovementAt = Time.time;
            _endsAt = Time.time + _duration;
            float start = Time.time + _invulnerabilityStart;
            _invulnerability.SetWindow(this, start, start + _invulnerabilityDuration);
            Started?.Invoke();
            return true;
        }

        // Llamar una sola vez por frame, en lugar del Tick de locomoción.
        public void Tick(float deltaTime)
        {
            _stamina.NotifyActionActive();

            // Integrar solo el tiempo transcurrido: distancia estable con FPS variables.
            float movementUntil = Mathf.Min(Time.time, _endsAt);
            float movementTime = Mathf.Max(0f, movementUntil - _lastMovementAt);
            _lastMovementAt = movementUntil;
            float speed = _distance / _duration * (movementTime / deltaTime);
            _motor.SetGroundMovement(_direction, speed, deltaTime);
            _motor.Simulate(deltaTime);

            if (Time.time >= _endsAt)
                Cancel();
        }

        public void Cancel()
        {
            _invulnerability.RemoveWindow(this);
            if (!IsDodging)
                return;

            IsDodging = false;
            _motor.SetGroundMovement(Vector3.zero, 0f, 0f);
            Ended?.Invoke();
        }

        private void OnDisable()
        {
            Cancel();
        }

        private void OnValidate()
        {
            _invulnerabilityStart = Mathf.Clamp(_invulnerabilityStart, 0f, _duration);
            _invulnerabilityDuration = Mathf.Clamp(_invulnerabilityDuration, 0f, _duration - _invulnerabilityStart);
        }
    }
}
