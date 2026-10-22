using FinalVFX.Combat;
using UnityEngine;
using static FinalVFX.Combat.WeaponDefinition;

namespace FinalVFX.Player
{
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerMotor))]
    [RequireComponent(typeof(PlayerEquipment))]
    public sealed class PlayerAnimationController : MonoBehaviour
    {
        [SerializeField] private Animator _animator;

        [Header("Locomotion")]
        [SerializeField, Min(0f)] private float _speedDamping = 0.1f;
        [SerializeField, Min(0f)] private float _movementThreshold = 0.1f;

        private PlayerMotor _motor;
        private PlayerEquipment _equipment;
        private int _weaponLayer = -1;

        private bool _ready;
        private bool _poseSuppressed;

        private static readonly int Speed =
            Animator.StringToHash("Speed");

        private static readonly int WeaponType =
            Animator.StringToHash("WeaponType");

        private static readonly int SwordState =
            Animator.StringToHash("Weapon Pose.WalkSword");

        private static readonly int ScytheState =
            Animator.StringToHash("Weapon Pose.WalkScythe");

        private void Awake()
        {
            _motor = GetComponent<PlayerMotor>();
            _equipment = GetComponent<PlayerEquipment>();

            if (_animator == null)
                _animator = GetComponentInChildren<Animator>(true);

            if (_animator == null ||
                _animator.runtimeAnimatorController == null)
            {
                Fail("Asigna el Animator y AC_Player.");
                return;
            }

            _weaponLayer = _animator.GetLayerIndex("Weapon Pose");

            if (_weaponLayer < 0)
            {
                Fail("Falta la capa 'Weapon Pose' en el Animator.");
                return;
            }

            if (!HasParameter(Speed, AnimatorControllerParameterType.Float) ||
                !HasParameter(
                    WeaponType, AnimatorControllerParameterType.Int))
            {
                Fail("Se necesitan Speed (Float) y WeaponType (Int).");
                return;
            }

            if (!_animator.HasState(_weaponLayer, SwordState) ||
                !_animator.HasState(_weaponLayer, ScytheState))
            {
                Fail("Faltan WalkSword o WalkScythe en Weapon Pose.");
                return;
            }

            _animator.applyRootMotion = false;
            _ready = true;
        }

        private void OnEnable()
        {
            if (!_ready)
                return;

            _equipment.WeaponChanged += HandleWeaponChanged;

            // Recupera el arma actual si se equipó antes de activarnos.
            ApplyWeapon(_equipment.EquippedWeapon, true);
        }

        private void OnDisable()
        {
            if (_equipment != null)
                _equipment.WeaponChanged -= HandleWeaponChanged;
        }

        private void Update()
        {
            if (!_ready)
                return;

            Vector3 velocity = _motor.Velocity;
            velocity.y = 0f;

            float speed = velocity.magnitude;

            if (speed < _movementThreshold)
                speed = 0f;

            _animator.SetFloat(
                Speed, speed, _speedDamping, Time.deltaTime);
        }

        private void HandleWeaponChanged(WeaponDefinition weapon)
        {
            ApplyWeapon(weapon, false);
        }

        private void ApplyWeapon(
            WeaponDefinition weapon, bool immediate)
        {
            WeaponPoseType pose = weapon != null
                ? weapon.PoseType
                : WeaponPoseType.None;

            // Si la capa estaba oculta, evita mostrar primero
            // la pose del arma anterior al volver a activarla.
            bool wasHidden =
                _animator.GetLayerWeight(_weaponLayer) <= 0f;

            _animator.SetInteger(WeaponType, (int)pose);

            if ((immediate || wasHidden) &&
                pose != WeaponPoseType.None)
            {
                int state = pose == WeaponPoseType.Sword
                    ? SwordState
                    : ScytheState;

                _animator.Play(state, _weaponLayer, 0f);
            }

            RefreshPoseWeight();
        }

        // Punto de integración para futuros ataques, esquives o muerte.
        public void SetWeaponPoseSuppressed(bool suppressed)
        {
            _poseSuppressed = suppressed;

            if (_ready)
                RefreshPoseWeight();
        }

        private void RefreshPoseWeight()
        {
            WeaponDefinition weapon = _equipment.EquippedWeapon;

            bool hasPose =
                weapon != null &&
                weapon.PoseType != WeaponPoseType.None;

            _animator.SetLayerWeight(
                _weaponLayer,
                hasPose && !_poseSuppressed ? 1f : 0f);
        }

        private bool HasParameter(
            int hash, AnimatorControllerParameterType type)
        {
            foreach (AnimatorControllerParameter parameter
                     in _animator.parameters)
            {
                if (parameter.nameHash == hash &&
                    parameter.type == type)
                    return true;
            }

            return false;
        }

        private void Fail(string message)
        {
            Debug.LogError(message, this);
            enabled = false;
        }
    }
}