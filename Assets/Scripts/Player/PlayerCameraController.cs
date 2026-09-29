using UnityEngine;
using UnityEngine.InputSystem;

namespace FinalVFX.Player
{
    [DefaultExecutionOrder(-100)]
    public class PlayerCameraController : MonoBehaviour
    {
        [SerializeField] private Transform _player;
        [SerializeField] private PlayerInputManager _inputManager;

        [Header("Position")]
        [SerializeField] private float _targetHeight = 1.5f;

        [Header("Look")]
        [SerializeField, Min(0f)]
        private float _mouseSensitivity = 0.08f;

        [SerializeField, Min(0f)]
        private float _stickSensitivity = 150f;

        [SerializeField] private float _minimumPitch = -35f;
        [SerializeField] private float _maximumPitch = 65f;
        [SerializeField] private float _initialPitch = 10f;
        [SerializeField] private bool _invertVertical;

        private float _yaw;
        private float _pitch;

        private void Awake()
        {
            if (_player == null || _inputManager == null)
            {
                Debug.LogError(
                    "Assign Player and Input Reader.", this);

                enabled = false;
                return;
            }

            _yaw = _player.eulerAngles.y;
            _pitch = Mathf.Clamp(
                _initialPitch, _minimumPitch, _maximumPitch);

            ApplyRotation();
            FollowPlayer();
        }

        private void OnEnable()
        {
            SetCursorLocked(true);
        }

        private void OnDisable()
        {
            SetCursorLocked(false);
        }

        private void Update()
        {
            // Controles provisionales para trabajar en el editor.
            if (Keyboard.current != null &&
                Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                SetCursorLocked(false);
                return;
            }

            if (Cursor.lockState != CursorLockMode.Locked)
            {
                if (Mouse.current != null &&
                    Mouse.current.leftButton.wasPressedThisFrame)
                {
                    SetCursorLocked(true);
                }

                return;
            }

            if (Time.timeScale == 0f)
            {
                return;
            }

            Vector2 look = _inputManager.Look;

            if (_inputManager.IsLookingFromMouse)
            {
                look *= _mouseSensitivity;
            }
            else
            {
                look *= _stickSensitivity * Time.deltaTime;
            }

            _yaw = Mathf.Repeat(_yaw + look.x, 360f);

            float verticalSign = _invertVertical ? 1f : -1f;

            _pitch = Mathf.Clamp(
                _pitch + look.y * verticalSign,
                _minimumPitch,
                _maximumPitch);

            ApplyRotation();
        }

        private void LateUpdate()
        {
            FollowPlayer();
        }

        private void ApplyRotation()
        {
            transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }

        private void FollowPlayer()
        {
            transform.position =
                _player.position + Vector3.up * _targetHeight;
        }

        private void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked
                ? CursorLockMode.Locked
                : CursorLockMode.None;

            Cursor.visible = !locked;
        }
    }
}