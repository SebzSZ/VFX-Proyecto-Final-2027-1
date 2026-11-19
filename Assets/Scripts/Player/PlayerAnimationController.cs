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

        [Header("Combate")]
        [SerializeField] private AnimationClip _combatPlaceholder;

        private AnimatorOverrideController _combatOverrides;
        private RuntimeAnimatorController _originalController;
        private int _combatLayer = -1;
        private static readonly int CombatState = Animator.StringToHash("Combat.Action");

        private PlayerMotor _motor;
        private PlayerController _player;
        private PlayerDodge _dodge;
        private PlayerEquipment _equipment;
        private int _weaponLayer = -1;

        private bool _ready;
        private bool _poseSuppressed;

        private static readonly int Speed =
            Animator.StringToHash("Speed");

        private static readonly int Grounded =
            Animator.StringToHash("Grounded");

        private static readonly int Dodging =
            Animator.StringToHash("Dodging");

        private static readonly int WeaponType =
            Animator.StringToHash("WeaponType");

        private static readonly int SwordState =
            Animator.StringToHash("Weapon Pose.SwordPose");

        private static readonly int ScytheState =
            Animator.StringToHash("Weapon Pose.ScythePose");


        private void Awake()
        {
            _motor = GetComponent<PlayerMotor>();
            _player = GetComponent<PlayerController>();
            _dodge = GetComponent<PlayerDodge>();
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
            ConfigureCombatLayer();
            _ready = true;
        }

        private void ConfigureCombatLayer()
        {
            _combatLayer = _animator.GetLayerIndex("Combat");
            _originalController = _animator.runtimeAnimatorController;
            if (_combatLayer < 0 || !_animator.HasState(_combatLayer, CombatState) ||
                !_combatPlaceholder ||
                System.Array.IndexOf(_originalController.animationClips, _combatPlaceholder) < 0)
            {
                Debug.LogWarning("Configura Combat.Action y su clip CombatPlaceholder.", this);
                return;
            }

            _combatOverrides = new AnimatorOverrideController(_originalController);
            _animator.runtimeAnimatorController = _combatOverrides;
            _animator.SetLayerWeight(_combatLayer, 0f);
        }

        public bool PlayCombatClip(AnimationClip clip)
        {
            if (!clip || !_combatOverrides || !isActiveAndEnabled)
            {
                StopCombatClip();
                return false;
            }

            _combatOverrides[_combatPlaceholder] = clip;
            _animator.SetLayerWeight(_combatLayer, 1f);
            _animator.Play(CombatState, _combatLayer, 0f);
            return true;
        }

        public void StopCombatClip()
        {
            if (_combatOverrides)
                _animator.SetLayerWeight(_combatLayer, 0f);
        }

        private void OnDestroy()
        {
            if (!_combatOverrides)
                return;

            if (_animator && _animator.runtimeAnimatorController == _combatOverrides)
                _animator.runtimeAnimatorController = _originalController;

            Destroy(_combatOverrides);
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

            if (_player != null && _player.IsSequenceLocked)
            {
                _animator.SetFloat(Speed, 0f);
                _animator.SetBool(Grounded, true);
                _animator.SetBool(Dodging, false);
                return;
            }

            Vector3 velocity = _motor.Velocity;
            velocity.y = 0f;

            float speed = velocity.magnitude;

            if (speed < _movementThreshold)
                speed = 0f;

            _animator.SetFloat(
                Speed, speed, _speedDamping, Time.deltaTime);

            bool inGround = _motor.IsGrounded;
            _animator.SetBool(
                Grounded, inGround);

            bool isDodging = _dodge.IsDodging;
            _animator.SetBool(
                Dodging, isDodging);
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
