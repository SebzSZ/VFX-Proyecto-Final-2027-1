using UnityEngine;

namespace FinalVFX.Player
{
    public class PlayerCameraController : MonoBehaviour
    {
        [SerializeField] private Transform _player;

        [Header("Position")]
        [SerializeField, Min(0f)] private float _targetHeight = 1.5f;

        [Header("Look")]
        [SerializeField, Min(0f)] private float _mouseSensitivity = 0.08f;
        [SerializeField, Min(0f)] private float _stickSensitivity = 150f;
        [SerializeField, Range(-89f, 89f)] private float _minimumPitch = -35f;
        [SerializeField, Range(-89f, 89f)] private float _maximumPitch = 65f;
        [SerializeField] private float _initialPitch = 10f;
        [SerializeField] private bool _invertVertical;

        private float _yaw;
        private float _pitch;

        public Transform Target => _player;
        public Vector3 PlanarForward => Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;

        private void Awake()
        {
            ResetOrientation();
            FollowTarget();
        }

        public void TickLook(Vector2 look, bool fromMouse, float deltaTime)
        {
            float sensitivity = fromMouse ? _mouseSensitivity : _stickSensitivity * deltaTime;
            look *= sensitivity;

            _yaw = Mathf.Repeat(_yaw + look.x, 360f);

            float verticalSign = _invertVertical ? 1f : -1f;
            _pitch = Mathf.Clamp(_pitch + look.y * verticalSign, _minimumPitch, _maximumPitch);

            ApplyRotation();
        }

        public void FollowTarget()
        {
            transform.position = _player.position + Vector3.up * _targetHeight;
        }

        public void ResetOrientation()
        {
            _yaw = _player.eulerAngles.y;
            _pitch = Mathf.Clamp(_initialPitch, _minimumPitch, _maximumPitch);
            ApplyRotation();
        }

        private void ApplyRotation()
        {
            transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }

        private void OnValidate()
        {
            _targetHeight = Mathf.Max(0f, _targetHeight);
            _mouseSensitivity = Mathf.Max(0f, _mouseSensitivity);
            _stickSensitivity = Mathf.Max(0f, _stickSensitivity);
            _minimumPitch = Mathf.Clamp(_minimumPitch, -89f, 88.9f);
            _maximumPitch = Mathf.Clamp(_maximumPitch, _minimumPitch + 0.1f, 89f);
            _initialPitch = Mathf.Clamp(_initialPitch, _minimumPitch, _maximumPitch);
        }
    }
}
