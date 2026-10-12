using UnityEngine;

namespace FinalVFX.Player
{
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(PlayerMotor))]
    [RequireComponent(typeof(PlayerController))]
    public class PlayerAnimationController : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
        [SerializeField, Min(0f)] private float _blendTime = 0.1f;
        [SerializeField, Min(0f)] private float _movementThreshold = 0.1f;

        private PlayerMotor _motor;
        private PlayerController _player;

        private static readonly int Idle =
            Animator.StringToHash("Base Layer.Idle");

        private static readonly int Jogging =
            Animator.StringToHash("Base Layer.Jogging");

        private static readonly int Run =
            Animator.StringToHash("Base Layer.Run");

        private static readonly int Jump =
            Animator.StringToHash("Base Layer.Jump");

        private static readonly int Dodge =
            Animator.StringToHash("Base Layer.Dodge");

        private int _currentState;

        private void Awake()
        {
            _motor = GetComponent<PlayerMotor>();
            _player = GetComponent<PlayerController>();

            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();

            if (_animator == null)
            {
                Debug.LogError(
                    "Asigna el Animator del modelo del jugador.", this);

                enabled = false;
                return;
            }

            _animator.applyRootMotion = false;
        }

        private void OnEnable()
        {
            _currentState = 0;
        }

        private void Update()
        {
            int nextState = Idle;

            if (_player.CanAct)
            {
                if (_player.IsDodging)
                {
                    nextState = Dodge;
                }
                else if (!_motor.IsGrounded)
                {
                    nextState = Jump;
                }
                else
                {
                    Vector3 velocity = _motor.Velocity;
                    velocity.y = 0f;

                    bool moving = velocity.sqrMagnitude >
                        _movementThreshold * _movementThreshold;

                    if (moving)
                        nextState = _player.IsSprinting ? Run : Jogging;
                }
            }
            else if (_player.IsDodging)
            {
                nextState = Dodge;
            }

            if (nextState == _currentState)
                return;

            _animator.CrossFadeInFixedTime(
                nextState, _blendTime, 0, 0f);

            _currentState = nextState;
        }
    }
}