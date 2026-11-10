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
    [RequireComponent(typeof(PlayerCombat))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private PlayerCameraController _cameraController;
        [SerializeField] private bool _gameplayEnabled = true;

        [Header("Reacción a Direct Damage")]
        [SerializeField, Min(0f)] private float _pushDistance = 0.6f;
        [SerializeField, Min(0.01f)] private float _pushSeconds = 0.12f;
        private Vector3 _pushVelocity;
        private float _pushRemaining;

        private PlayerInputManager _input;
        private PlayerLocomotion _locomotion;
        private PlayerMotor _motor;
        private StaminaComponent _stamina;
        private HealthComponent _health;
        private HealingConsumable _healing;
        private PlayerDodge _dodge;
        private PlayerCombat _combat;
        private bool _hasFocus;
        private bool _sequenceLocked;

        public event Action Died;

        public bool IsSprinting => _locomotion.IsSprinting;
        public bool IsDodging => _dodge.IsDodging;
        public bool IsSequenceLocked => _sequenceLocked;
        public bool CanAct => isActiveAndEnabled && !_sequenceLocked && _gameplayEnabled && _hasFocus && !_health.IsDead && !_dodge.IsDodging && !_combat.Busy && _pushRemaining <= 0f && Time.timeScale > 0f;

        private void Awake()
        {
            _input = GetComponent<PlayerInputManager>();
            _motor = GetComponent<PlayerMotor>();
            _locomotion = GetComponent<PlayerLocomotion>();
            _stamina = GetComponent<StaminaComponent>();
            _health = GetComponent<HealthComponent>();
            _healing = GetComponent<HealingConsumable>();
            _dodge = GetComponent<PlayerDodge>();
            _combat = GetComponent<PlayerCombat>();
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

            if (acceptInput && _input.LockViewPressed)
                _cameraController.ToggleBossView();

            _cameraController.TickLook(look, _input.IsLookingFromMouse, deltaTime);
            Vector3 forward = _cameraController.PlanarForward;
            Transform lockedTarget = _cameraController.LockedTarget;

            if (lockedTarget != null)
            {
                Vector3 toBoss = lockedTarget.position - transform.position;
                toBoss.y = 0f;

                if (toBoss.sqrMagnitude > 0.0001f)
                    forward = toBoss.normalized;
            }

            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 direction = right * movement.x + forward * movement.y;

            float now = Time.time;
            if (_pushRemaining > 0f)
            {
                _combat.Advance(now);
                _locomotion.Suspend(sprintHeld);
                float slice = Mathf.Min(deltaTime, _pushRemaining);
                _motor.SimulatePush(_pushVelocity * slice, deltaTime);
                _pushRemaining -= slice;
                _cameraController.FollowTarget();
                return;
            }

            if (_combat.SingleHasFinished(now))
                _combat.Advance(now);

            bool dodgeRequested = acceptInput && _input.IsDodgePressed && _combat.AllowsDodge;
            if (dodgeRequested && _dodge.TryStart(direction))
                _combat.InterruptBasic();

            if (_dodge.IsDodging)
            {
                _locomotion.Suspend(sprintHeld);
                _dodge.Tick(deltaTime);
            }
            else
            {
                bool canAttack = acceptInput && _motor.IsGrounded;
                bool abilityStarted = canAttack && _combat.TryAbility(_input, now);
                if (!abilityStarted)
                {
                    _combat.Advance(now);
                    if (canAttack)
                        _combat.ReadBasicPress(_input, now);
                }

                bool canMove = acceptInput && !_combat.Busy;
                bool healed = canMove && !dodgeRequested && _motor.IsGrounded
                    && _input.ConsumablePressed && _healing.TryUse();
                if (healed)
                    _stamina.NotifyActionActive();

                _locomotion.Tick(direction, sprintHeld, jumpPressed && !dodgeRequested && !healed,
                    canMove, deltaTime);
            }

            _cameraController.FollowTarget();
        }

        public void ReactToDirectDamage(Vector3 origin)
        {
            if (!isActiveAndEnabled || _health.IsDead || _sequenceLocked || !_gameplayEnabled)
                return;

            Vector3 away = Vector3.ProjectOnPlane(transform.position - origin, Vector3.up);
            if (away.sqrMagnitude < 0.0001f)
                away = -transform.forward;

            _dodge.Cancel();
            _combat.InterruptByDamage();
            _locomotion.Suspend(_input.SprintHeld);
            _pushRemaining = Mathf.Max(0.01f, _pushSeconds);
            _pushVelocity = away.normalized * (_pushDistance / _pushRemaining);
        }

        public void SetGameplayEnabled(bool enabled)
        {
            _gameplayEnabled = enabled;
            if (!enabled)
            {
                _dodge.Cancel();
                _combat.ResetCombat();
                _pushRemaining = 0f;
            }

            ApplyCursorState();
        }

        // Restablece al jugador; no reinicia enemigos, hazards ni el encuentro.
        public void ResetForRespawn()
        {
            _dodge.Cancel();
            _combat.ResetEncounter();
            _pushRemaining = 0f;
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
                _combat.ResetCombat();
                _pushRemaining = 0f;
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
            _combat.ResetCombat();
            _pushRemaining = 0f;
            ApplyCursorState();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            _hasFocus = hasFocus;
            if (!hasFocus)
            {
                _dodge.Cancel();
                _combat.ResetCombat();
                _pushRemaining = 0f;
            }

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
