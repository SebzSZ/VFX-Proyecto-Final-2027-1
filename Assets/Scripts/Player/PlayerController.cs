using FinalVFX.Combat;
using UnityEngine;

namespace FinalVFX.Player
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInputManager))]
    [RequireComponent(typeof(PlayerLocomotion))]
    [RequireComponent(typeof(PlayerMotor))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private PlayerCameraController _cameraController;
        [SerializeField] private bool _gameplayEnabled = true;

        private PlayerInputManager _input;
        private PlayerLocomotion _locomotion;
        private PlayerMotor _motor;
        private StaminaComponent _stamina;

        private bool _hasFocus;

        public bool IsSprinting => _locomotion.IsSprinting;

        private void Awake()
        {
            _input = GetComponent<PlayerInputManager>();
            _motor = GetComponent<PlayerMotor>();
            _locomotion = GetComponent<PlayerLocomotion>();
            _stamina = GetComponent<StaminaComponent>();
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0f)
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

            _locomotion.Tick(direction, sprintHeld, jumpPressed, acceptInput, deltaTime);
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

            _cameraController.ResetOrientation();
            _cameraController.FollowTarget();
        }

        private void OnEnable()
        {
            _hasFocus = Application.isFocused;
            ApplyCursorState();
        }

        private void OnDisable()
        {
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
