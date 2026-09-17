using UnityEngine;

namespace FinalVFX.Player
{
    public class PlayerInputManager : MonoBehaviour
    {
        private PlayerControls _controls;

        public Vector2 Movement => isActiveAndEnabled && _controls != null ? _controls.Player.Movement.ReadValue<Vector2>() : Vector2.zero;
        public bool SprintHeld => isActiveAndEnabled && _controls != null && _controls.Player.Sprint.IsPressed();
        public bool JumpPressed => isActiveAndEnabled && _controls != null && _controls.Player.Jump.WasPressedThisFrame();

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
            _controls?.Dispose();
        }
    }
}
