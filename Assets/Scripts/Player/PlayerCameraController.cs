using Unity.Cinemachine;
using UnityEngine;

using FinalVFX.Boss;
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

        [Header("Boss Lock")]
        [SerializeField] private CinemachineCamera _bossCamera;
        [SerializeField] private Transform _combatPivot;
        [SerializeField] private Transform _bossAimPoint;
        [SerializeField] private GuardianHealth _bossHealth;

        private bool _viewLocked;
        private float _combatYaw;
        private bool _hasCombatYaw;

        private bool CanLockView =>
            _bossCamera != null &&
            _combatPivot != null &&
            _bossAimPoint != null &&
            _bossAimPoint.gameObject.activeInHierarchy &&
            _bossHealth != null &&
            !_bossHealth.IsDead;

        private float _yaw;
        private float _pitch;

        public Transform Target => _player;
        public Vector3 PlanarForward => Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        public Transform LockedTarget => _viewLocked && CanLockView ? _bossAimPoint : null;

        private void Awake()
        {
            ResetOrientation();
            FollowTarget();
        }

        public void TickLook(Vector2 look, bool fromMouse, float deltaTime)
        {
            if (_viewLocked)
            {
                if (CanLockView)
                    return;

                SetBossView(false);
            }

            float sensitivity = fromMouse ? _mouseSensitivity : _stickSensitivity * deltaTime;
            look *= sensitivity;

            _yaw = Mathf.Repeat(_yaw + look.x, 360f);

            float verticalSign = _invertVertical ? 1f : -1f;
            _pitch = Mathf.Clamp(_pitch + look.y * verticalSign, _minimumPitch, _maximumPitch);

            ApplyRotation();
        }

        public void FollowTarget()
        {
            Vector3 pivotPosition = _player.position + Vector3.up * _targetHeight;
            transform.position = pivotPosition;

            if (_combatPivot == null || _bossAimPoint == null)
                return;

            Vector3 toBoss = _bossAimPoint.position - pivotPosition;
            toBoss.y = 0f;

            if (!_hasCombatYaw)
            {
                _combatYaw = transform.eulerAngles.y;
                _hasCombatYaw = true;
            }

            if (toBoss.sqrMagnitude > 0.25f)
            {
                float targetYaw = Mathf.Atan2(toBoss.x, toBoss.z) * Mathf.Rad2Deg;

                targetYaw += 18f;

                _combatYaw = _viewLocked
                    ? Mathf.MoveTowardsAngle(_combatYaw, targetYaw, 360f * Time.deltaTime)
                    : targetYaw;
            }

            _combatPivot.SetPositionAndRotation(
                pivotPosition,
                Quaternion.Euler(12f, _combatYaw, 0f));
        }

        public void ResetOrientation()
        {
            SetBossView(false);

            _yaw = _player.eulerAngles.y;
            _pitch = Mathf.Clamp(_initialPitch, _minimumPitch, _maximumPitch);
            ApplyRotation();
        }

        private void ApplyRotation()
        {
            transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }

        public void ToggleBossView()
        {
            SetBossView(!_viewLocked && CanLockView);
        }

        private void SetBossView(bool locked)
        {
            _viewLocked = locked;

            if (_bossCamera != null)
                _bossCamera.Priority = locked ? 210 : 0;
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
