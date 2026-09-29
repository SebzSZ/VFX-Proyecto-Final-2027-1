using FinalVFX.Combat;
using UnityEngine;

namespace FinalVFX.Player
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInputManager))]
    [RequireComponent(typeof(StaminaComponent))]
    public class PlayerMotor : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField, Min(0f)] private float _moveSpeed;
        [SerializeField, Min(0f)] private float _rotationSpeed;
        [SerializeField] private float _gravity;
        [SerializeField] private Transform _movementReference;

        [Header("Sprint")]
        [SerializeField, Min(0f)] private float _sprintSpeed;
        [SerializeField, Min(0.01f)] private float _sprintStaminaPerSecond;
        [SerializeField, Min(0.01f)] private float _mininumSprintStamina;

        [Header("Jump")]
        [SerializeField, Min(0.01f)] private float _jumpHeight;
        [SerializeField, Min(0f)] private float _jumpStaminaCost;
        [SerializeField, Min(0f)] private float _airSteeringSpeed;


        private float _verticalSpeed;
        private bool _mustReleaseSprint;
        private Vector3 _horizontalVelocity;

        private CharacterController _characterController;
        private PlayerInputManager _inputManager;
        private StaminaComponent _stamina;

        public bool IsSprinting { get; private set; }


        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _inputManager = GetComponent<PlayerInputManager>();
            _stamina = GetComponent<StaminaComponent>();
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;

            if (deltaTime <= 0f) return;

            Vector2 input = Vector2.ClampMagnitude(_inputManager.Movement, 1f);

            Vector3 forward = _movementReference != null ? Vector3.ProjectOnPlane(_movementReference.forward, Vector3.up).normalized : Vector3.forward;

            Vector3 right = Vector3.Cross(Vector3.up, forward);

            Vector3 direction = right * input.x + forward * input.y;

            bool hasMovement = direction.sqrMagnitude > 0.0001f;

            if (!hasMovement)
                direction = Vector3.zero;
            
            if (!_inputManager.SprintHeld)
                _mustReleaseSprint = false;

            bool grounded = _characterController.isGrounded && _verticalSpeed <= 0f;
            bool jumpedThisFrame = false;

            if (grounded)
            {
                _verticalSpeed = -2f;

                if (_inputManager.JumpPressed && _stamina.TrySpend(_jumpStaminaCost))
                {
                    _verticalSpeed = Mathf.Sqrt(-2f * _gravity * _jumpHeight);
                    jumpedThisFrame = true;
                    IsSprinting = false;

                    if (_stamina.Current <= 0f)
                    {
                        _mustReleaseSprint = true;
                    }
                }
                else
                {
                    float speed = ResolveSpeed(hasMovement, deltaTime);
                    _horizontalVelocity = direction * speed;

                    if (hasMovement)
                    {
                        RotateTowardsMovement(direction, deltaTime);
                    }
                }
            }
            else
            {
                IsSprinting = false;
                ApplyAirSteering(direction, deltaTime);
            }

            if (!grounded || jumpedThisFrame)
            {
                _stamina.NotifyActionActive();
            }

            _verticalSpeed += _gravity * deltaTime;

            Vector3 velocity = _horizontalVelocity;
            velocity.y = _verticalSpeed;

            CollisionFlags collisions = _characterController.Move(velocity * deltaTime);

            if ((collisions & CollisionFlags.Above) != 0 && _verticalSpeed > 0f)
            {
                _verticalSpeed = 0f;
            }

            if ((collisions & CollisionFlags.Below) != 0 && _verticalSpeed < 0f)
            {
                _verticalSpeed = -2f;
            }

            Vector3 actualVelocity = _characterController.velocity;
            _horizontalVelocity = new Vector3(actualVelocity.x, 0f, actualVelocity.z);
        }

        private float ResolveSpeed(bool hasMovement, float deltaTime)
        {
            bool sprintHeld = _inputManager.SprintHeld;

            if (!sprintHeld) _mustReleaseSprint = false;

            bool wantsSprint = sprintHeld && hasMovement && _characterController.isGrounded && !_mustReleaseSprint;
            if (!wantsSprint)
            {
                IsSprinting = false;
                return _moveSpeed;
            }

            float requested = _sprintStaminaPerSecond * deltaTime;
            float consumed = _stamina.ConsumeUpTo(requested);

            IsSprinting = consumed > 0f;

            float sprintFraction = consumed / requested;
            float speed = Mathf.Lerp(_moveSpeed, _sprintSpeed, sprintFraction);

            if (_stamina.Current <= 0f)
            {
                IsSprinting = false;
                _mustReleaseSprint = true;
            }

            return speed;
        }

        private void RotateTowardsMovement(Vector3 direction, float deltaTime)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _rotationSpeed * deltaTime);
        }

        private void ApplyAirSteering(Vector3 direction, float deltaTime)
        {
            if (direction.sqrMagnitude <= 0.0001f || _horizontalVelocity.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Vector3 targetVelocity = direction.normalized * _horizontalVelocity.magnitude;
            _horizontalVelocity = Vector3.RotateTowards(_horizontalVelocity, targetVelocity, _airSteeringSpeed * Mathf.Deg2Rad * deltaTime, 0f);
        }

        private void OnDisable()
        {
            IsSprinting = false;
            _mustReleaseSprint = false;
            _horizontalVelocity = Vector3.zero;
        }
    }
}
