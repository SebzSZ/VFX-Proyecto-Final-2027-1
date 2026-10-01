using FinalVFX.Combat;
using UnityEngine;

namespace FinalVFX.Player
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInputManager))]
    [RequireComponent(typeof(PlayerLocomotion))]
    [RequireComponent(typeof(PlayerMotor))]
    [RequireComponent(typeof(HealthComponent))]
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

        private bool _hasFocus;

        public bool IsSprinting => _locomotion.IsSprinting;
        public bool CanAct => isActiveAndEnabled && _gameplayEnabled && _hasFocus && !_health.IsDead && Time.timeScale > 0f;

        private void Awake()
        {
            _input = GetComponent<PlayerInputManager>();
            _motor = GetComponent<PlayerMotor>();
            _locomotion = GetComponent<PlayerLocomotion>();
            _stamina = GetComponent<StaminaComponent>();
            _health = GetComponent<HealthComponent>();
            _healing = GetComponent<HealingConsumable>();
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0f || _health.IsDead)
                return;

            bool acceptInput = _gameplayEnabled && _hasFocus;

            Vector2 movement = acceptInput ? Vector2.ClampMagnitude(_input.Movement, 1f) : Vector2.zero;
            Vector2 look = acceptInput ? _input.Look : Vector2.zero;

            bool sprintHeld = _input.SprintHeld;
            bool jumpPressed = acceptInput && _input.JumpPressed;
            bool fromMouse = _input.IsLookingFromMouse;

            _cameraController.TickLook(look, fromMouse, deltaTime);

            Vector3 forward = _cameraController.PlanarForward;
            Vector3 right = Vector3.Cross(Vector3.up, forward);

            Vector3 direction = right * movement.x + forward * movement.y;

            bool healed = acceptInput && _motor.IsGrounded && _input.ConsumablePressed && _healing.TryUse();
            if (healed)
                _stamina.NotifyActionActive();

            _locomotion.Tick(direction, sprintHeld, jumpPressed && !healed, acceptInput, deltaTime);
            _cameraController.FollowTarget();
        }

        public void SetGameplayEnabled(bool enabled)
        {
            _gameplayEnabled = enabled;
            ApplyCursorState();
        }

        public void ResetForRespawn()
        {
            _motor.ResetMotion();
            _locomotion.ResetState();
            _stamina.RestoreFull();
            _health.RestoreFull();
            _healing.RestoreFull();

            _cameraController.ResetOrientation();
            _cameraController.FollowTarget();

            SetGameplayEnabled(true);
        }

        private void HandleDeath()
        {
            SetGameplayEnabled(false);
            _motor.ResetMotion();
            _locomotion.ResetState();
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
            ApplyCursorState();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            _hasFocus = hasFocus;
            ApplyCursorState();
        }

        private void ApplyCursorState()
        {
            bool locked = isActiveAndEnabled && _gameplayEnabled && _hasFocus;

            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
