using System;
using FinalVFX.Combat;
using UnityEngine;

namespace FinalVFX.Player
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInputManager))]
    [RequireComponent(typeof(PlayerLocomotion))]
    [RequireComponent(typeof(PlayerMotor))]
    [RequireComponent(typeof(PlayerDodge))]
    [RequireComponent(typeof(HealingConsumable))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private PlayerCameraController _cameraController;
        [SerializeField] private bool _gameplayEnabled = true;

        private PlayerInputManager _input;
        private PlayerLocomotion _locomotion;
        private PlayerMotor _motor;
        private StaminaComponent _stamina;
        private HealthComponent _health;
        private HealingConsumable _healing;
        private PlayerDodge _dodge;
        private bool _hasFocus;
        private bool _sequenceLocked;

        public event Action Died;

        public bool IsSprinting => _locomotion.IsSprinting;
        public bool IsDodging => _dodge.IsDodging;
        public bool IsSequenceLocked => _sequenceLocked;
        public bool CanAct => isActiveAndEnabled && !_sequenceLocked && _gameplayEnabled && _hasFocus && !_health.IsDead && !_dodge.IsDodging && Time.timeScale > 0f;

        private void Awake()
        {
            _input = GetComponent<PlayerInputManager>();
            _motor = GetComponent<PlayerMotor>();
            _locomotion = GetComponent<PlayerLocomotion>();
            _stamina = GetComponent<StaminaComponent>();
            _health = GetComponent<HealthComponent>();
            _healing = GetComponent<HealingConsumable>();
            _dodge = GetComponent<PlayerDodge>();
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0f || _health.IsDead)
                return;

            if (_sequenceLocked)
            {
                _locomotion.Tick(
                    Vector3.zero,
                    false,
                    false,
                    false,
                    deltaTime);

                return;
            }

            bool acceptInput = _gameplayEnabled && _hasFocus;
            Vector2 movement = acceptInput ? Vector2.ClampMagnitude(_input.Movement, 1f) : Vector2.zero;
            Vector2 look = acceptInput ? _input.Look : Vector2.zero;
            bool sprintHeld = _input.SprintHeld;
            bool jumpPressed = acceptInput && _input.JumpPressed;

            _cameraController.TickLook(look, _input.IsLookingFromMouse, deltaTime);
            Vector3 forward = _cameraController.PlanarForward;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 direction = right * movement.x + forward * movement.y;

            // Una petición de esquive tiene prioridad sobre curación y salto.
            bool dodgeRequested = acceptInput && _input.IsDodgePressed;
            if (dodgeRequested)
                _dodge.TryStart(direction);

            if (_dodge.IsDodging)
            {
                _locomotion.Suspend(sprintHeld);
                _dodge.Tick(deltaTime);
            }
            else
            {
                bool healed = !dodgeRequested && acceptInput && _motor.IsGrounded && _input.ConsumablePressed && _healing.TryUse();
                if (healed)
                    _stamina.NotifyActionActive();

                _locomotion.Tick(direction, sprintHeld, jumpPressed && !dodgeRequested && !healed, acceptInput, deltaTime);
            }

            _cameraController.FollowTarget();
        }

        public void SetGameplayEnabled(bool enabled)
        {
            _gameplayEnabled = enabled;
            if (!enabled)
                _dodge.Cancel();

            ApplyCursorState();
        }

        // Restablece al jugador; no reinicia enemigos, hazards ni el encuentro.
        public void ResetForRespawn()
        {
            _dodge.Cancel();
            _motor.ResetMotion();
            _locomotion.ResetState();
            _stamina.RestoreFull();
            _health.RestoreFull();
            _healing.RestoreFull();
            _cameraController.ResetOrientation();
            _cameraController.FollowTarget();
            SetGameplayEnabled(true);
        }

        public void SetSequenceLocked(bool locked)
        {
            _sequenceLocked = locked;

            if (locked)
            {
                _dodge.Cancel();
                _motor.ResetMotion();
                _locomotion.ResetState();
            }

            ApplyCursorState();
        }

        private void HandleDeath()
        {
            SetGameplayEnabled(false);
            _motor.ResetMotion();
            _locomotion.ResetState();
            Died?.Invoke();
        }

        private void OnEnable()
        {
            _health.Died += HandleDeath;
            _hasFocus = Application.isFocused;
            ApplyCursorState();
        }

        private void OnDisable()
        {
            _health.Died -= HandleDeath;
            _dodge.Cancel();
            ApplyCursorState();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            _hasFocus = hasFocus;
            if (!hasFocus)
                _dodge.Cancel();

            ApplyCursorState();
        }

        private void ApplyCursorState()
        {
            bool locked = isActiveAndEnabled && (_gameplayEnabled || _sequenceLocked) && _hasFocus;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}

