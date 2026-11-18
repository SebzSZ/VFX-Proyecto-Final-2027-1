using UnityEngine;

namespace FinalVFX.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMotor : MonoBehaviour
    {
        private const float GroundedVerticalSpeed = -2f;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float _rotationSpeed = 720f;
        [SerializeField] private float _gravity = -20f;

        [Header("Air")]
        [SerializeField, Min(0f)] private float _airSteeringSpeed = 20f;

        private Vector3 _horizontalVelocity;
        private float _verticalSpeed;

        private CharacterController _characterController;

        public bool IsGrounded => _characterController.isGrounded && _verticalSpeed <= 0f;
        public Vector3 Velocity => _horizontalVelocity + Vector3.up * _verticalSpeed;


        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
        }

        public void SetGroundMovement(Vector3 direction, float speed, float deltaTime)
        {
            _horizontalVelocity = direction * speed;

            if (direction.sqrMagnitude <= 0.0001f)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, targetRotation, _rotationSpeed * deltaTime);
        }

        public void Jump(float height)
        {
            _verticalSpeed = Mathf.Sqrt(-2f * _gravity * height);
        }

        public void SteerInAir(Vector3 direction, float deltaTime)
        {
            if (direction.sqrMagnitude <= 0.0001f || _horizontalVelocity.sqrMagnitude <= 0.0001f)
                return;

            Vector3 targetVelocity = direction.normalized * _horizontalVelocity.magnitude;
            _horizontalVelocity = Vector3.RotateTowards(_horizontalVelocity, targetVelocity, _airSteeringSpeed * Mathf.Deg2Rad * deltaTime, 0f);
        }

        public void Simulate(float deltaTime)
        {
            if (IsGrounded)
                _verticalSpeed = GroundedVerticalSpeed;

            float previousVerticalSpeed = _verticalSpeed;
            _verticalSpeed += _gravity * deltaTime;

            Vector3 velocity = _horizontalVelocity;
            velocity.y = (previousVerticalSpeed + _verticalSpeed) * 0.5f;

            Vector3 displacement = FinalVFX.Boss.BossFogBarrier.Constrain(
                _characterController, velocity * deltaTime);
            CollisionFlags collisions = _characterController.Move(displacement);

            if ((collisions & CollisionFlags.Above) != 0 && _verticalSpeed > 0f)
                _verticalSpeed = 0f;

            Vector3 actualVelocity = _characterController.velocity;
            _horizontalVelocity = new Vector3(actualVelocity.x, 0f, actualVelocity.z);
        }

        // Un solo Move por frame. Conserva la velocidad vertical y la orientación.
        public void SimulatePush(Vector3 displacement, float deltaTime)
        {
            _horizontalVelocity = displacement / deltaTime;
            Simulate(deltaTime);
            _horizontalVelocity = Vector3.zero;
        }

        public Vector3 FaceTarget(Transform target)
        {
            if (target == null)
                return transform.forward;

            Vector3 forward = target.position - transform.position;
            forward.y = 0f;

            if (forward.sqrMagnitude < 0.0001f)
                return transform.forward;

            forward.Normalize();
            transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
            return forward;
        }

        public void ResetMotion()
        {
            _horizontalVelocity = Vector3.zero;
            _verticalSpeed = 0f;
        }

        private void OnValidate()
        {
            _rotationSpeed = Mathf.Max(0f, _rotationSpeed);
            _airSteeringSpeed = Mathf.Max(0f, _airSteeringSpeed);
            _gravity = Mathf.Min(-0.01f, _gravity);
        }
    }
}

