using UnityEngine;

namespace FinalVFX.Player
{
    public class PlayerInputManager : MonoBehaviour
    {
        private PlayerControls _controls;

        public Vector2 Movement => _controls.Player.Movement.ReadValue<Vector2>();
        public bool SprintHeld => _controls.Player.Sprint.IsPressed();
        public bool JumpPressed => _controls.Player.Jump.WasPressedThisFrame();
        public Vector2 Look => _controls.Player.Look.ReadValue<Vector2>();
        public bool IsLookingFromMouse => _controls.Player.Look.activeControl?.device is UnityEngine.InputSystem.Mouse;
        public bool ConsumablePressed => _controls.Player.Consume.WasPressedThisFrame();
        public bool IsDodgePressed => _controls.Player.Dodge.WasPressedThisFrame();

        private void Awake()
        {
            _controls = new PlayerControls();
        }

        private void OnEnable()
        {
            _controls.Player.Enable();
        }

        private void OnDisable()
        {
            _controls.Player.Disable();
        }

        private void OnDestroy()
        {
            _controls.Dispose();
        }
    }
}
