using FinalVFX.Combat;
using UnityEngine;

namespace FinalVFX.Player
{
    [RequireComponent(typeof(PlayerMotor))]
    [RequireComponent(typeof(StaminaComponent))]
    public class PlayerLocomotion : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField, Min(0f)] private float _moveSpeed = 5f;

        [Header("Sprint")]
        [SerializeField, Min(0f)] private float _sprintSpeed = 8f;
        [SerializeField, Min(0.01f)] private float _sprintStaminaPerSecond = 15f;
        private bool _mustReleaseSprint;

        [Header("Jump")]
        [SerializeField, Min(0.01f)] private float _jumpHeight = 0.6f;
        [SerializeField, Min(0f)] private float _jumpStaminaCost = 20f;

        private PlayerMotor _motor;
        private StaminaComponent _stamina;

        public bool IsSprinting { get; private set; }

        private void Awake()
        {
            _motor = GetComponent<PlayerMotor>();
            _stamina = GetComponent<StaminaComponent>();

            if (_jumpStaminaCost > _stamina.Maximum)
                Debug.LogError("[PlayerLocomotion] Jump cost exceeds maximum stamina, the player will never be able to jump.", this);
        }

        public void Tick(Vector3 direction, bool sprintHeld, bool jumpPressed, bool acceptInput, float deltaTime)
        {
            // Si se agota la stamina hay que soltar el sprint antes de poder volver a correr
            if (!sprintHeld)
                _mustReleaseSprint = false;

            if (sprintHeld && _stamina.Current <= 0f)
                _mustReleaseSprint = true;

            if (!acceptInput)
            {
                direction = Vector3.zero;
                jumpPressed = false;
            }

            bool hasMovement = direction.sqrMagnitude > 0.0001f;
            bool wasSprinting = IsSprinting && sprintHeld && !_mustReleaseSprint && acceptInput;

            IsSprinting = false;

            bool grounded = _motor.IsGrounded;
            bool jumpedThisFrame = false;

            if (grounded)
            {
                if (jumpPressed && _stamina.TrySpend(_jumpStaminaCost))
                {
                    float launchSpeed = wasSprinting ? _sprintSpeed : _moveSpeed;

                    _motor.SetGroundMovement(direction, launchSpeed, deltaTime);
                    _motor.Jump(_jumpHeight);
                    jumpedThisFrame = true;

                    if (sprintHeld && _stamina.Current <= 0f)
                        _mustReleaseSprint = true;
                }
                else
                {
                    float speed = ResolveGroundSpeed(hasMovement, acceptInput && sprintHeld, deltaTime);
                    _motor.SetGroundMovement(direction, speed, deltaTime);
                }
            }
            else
            {
                _motor.SteerInAir(direction, deltaTime);
            }

            if (!grounded || jumpedThisFrame)
                _stamina.NotifyActionActive();

            _motor.Simulate(deltaTime);
        }

        private float ResolveGroundSpeed(bool hasMovement, bool sprintHeld, float deltaTime)
        {
            if (!hasMovement || !sprintHeld || _mustReleaseSprint)
                return _moveSpeed;

            float requested = _sprintStaminaPerSecond * deltaTime;
            float consumed = _stamina.ConsumeUpTo(requested);

            if (_stamina.Current <= 0f)
                _mustReleaseSprint = true;

            IsSprinting = consumed > 0f && !_mustReleaseSprint;

            // Si solo alcanzó para una parte del frame, la velocidad se queda a medio camino
            float fraction = consumed / requested;
            return Mathf.Lerp(_moveSpeed, _sprintSpeed, fraction);
        }

        public void ResetState()
        {
            IsSprinting = false;
            _mustReleaseSprint = false;
        }

        private void OnValidate()
        {
            _moveSpeed = Mathf.Max(0f, _moveSpeed);
            _sprintSpeed = Mathf.Max(_moveSpeed, _sprintSpeed);
            _sprintStaminaPerSecond = Mathf.Max(0.01f, _sprintStaminaPerSecond);
            _jumpHeight = Mathf.Max(0.01f, _jumpHeight);
            _jumpStaminaCost = Mathf.Max(0f, _jumpStaminaCost);
        }
    }
}
