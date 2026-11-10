using System;
using FinalVFX.Combat;
using UnityEngine;

namespace FinalVFX.Boss
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BossWanderMotor))]
    [RequireComponent(typeof(GuardianHealth))]
    public class BossController : MonoBehaviour
    {
        private enum BehaviourState
        {
            None,
            Wandering,
            Chasing
        }

        [SerializeField] private Transform _player;
        [SerializeField] private BossAISettings _settings;
        [SerializeField] private bool _combatEnabled = true;
        [SerializeField] private int _decisionSeed = 12345;

        [Header("Ataque básico melee")]
        [SerializeField] private DirectDamage _meleeDamage;
        [SerializeField, Min(0.01f)] private float _windup = 0.55f;
        [SerializeField, Min(0.01f)] private float _active = 0.18f;
        [SerializeField, Min(0f)] private float _recovery = 0.55f;
        private GuardianHealth _health;
        private CharacterController _playerBody;
        private BoxCollider _meleeBox;
        private bool _doingMelee;
        private bool _hitPlayer;
        private bool _resumeAfterStun;
        private float _meleeStart;
        private float _previousElapsed;
        private BossWanderMotor _motor;
        private BossDecisionSelector _selector;
        private BehaviourState _state;
        private float _nextDecision;

        public event Action<BossAttackId> AttackSelected;

        private void Awake()
        {
            _motor = GetComponent<BossWanderMotor>();
            _health = GetComponent<GuardianHealth>();

            if (_player == null || _settings == null)
            {
                Debug.LogError(
                    $"{name}: asigna Player y Settings en BossController.",
                    this);

                enabled = false;
                return;
            }

            _playerBody = _player.GetComponent<CharacterController>();
            _meleeBox = _meleeDamage ? _meleeDamage.GetComponent<BoxCollider>() : null;
            if (!_playerBody || !_meleeBox || !_meleeDamage.ManualMelee ||
                !_meleeBox.transform.IsChildOf(transform))
            {
                Debug.LogError("Asigna el Player raíz y un hijo con BoxCollider y DirectDamage en Manual Melee.", this);
                enabled = false;
                return;
            }

            _meleeBox.enabled = false;
            _selector = new BossDecisionSelector(
                _settings, _decisionSeed);
        }

        private void OnEnable()
        {
            _health.StunStarted += OnStun;
            _health.Died += StopAction;
        }

        private void StopAction()
        {
            _doingMelee = false;
            _state = BehaviourState.None;
            _motor.Stop();
        }

        private void OnStun(float distance)
        {
            StopAction();
            _resumeAfterStun = true;
            if (!_playerBody || !_playerBody.enabled || !_playerBody.gameObject.activeInHierarchy || distance <= 0f)
                return;

            Vector3 away = Vector3.ProjectOnPlane(_playerBody.transform.position - transform.position, Vector3.up);
            if (away.sqrMagnitude < 0.0001f)
                away = transform.forward;

            _playerBody.Move(away.normalized * distance);
        }

        private void BeginMelee(Vector3 direction)
        {
            StopAction();
            if (direction.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(direction);
            _doingMelee = true;
            _hitPlayer = false;
            _previousElapsed = 0f;
            _meleeStart = Time.time;
        }

        private void TickMelee()
        {
            float elapsed = Time.time - _meleeStart;
            float end = _windup + _active;
            if (!_hitPlayer && elapsed >= _windup && (elapsed <= end || _previousElapsed < _windup))
            {
                Transform box = _meleeBox.transform;
                Vector3 scale = box.lossyScale;
                Vector3 half = Vector3.Scale(_meleeBox.size,
                    new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z))) * 0.5f;
                Physics.SyncTransforms();
                foreach (Collider other in Physics.OverlapBox(box.TransformPoint(_meleeBox.center), half,
                    box.rotation, _meleeDamage.TargetLayers, QueryTriggerInteraction.Collide))
                {
                    if (other.transform != _player && !other.transform.IsChildOf(_player))
                        continue;
                    if (_meleeDamage.TryMeleeHit(other))
                    {
                        _hitPlayer = true;
                        break;
                    }
                }
            }
            _previousElapsed = elapsed;
            if (elapsed >= end + _recovery)
                _doingMelee = false;
        }

        private void Update()
        {
            if (Time.deltaTime <= 0f)
                return;

            if (!_combatEnabled)
            {
                _motor.Stop();
                _state = BehaviourState.None;
                return;
            }

            if (_health.IsDead || _health.Stunned)
                return;

            if (_resumeAfterStun)
            {
                _resumeAfterStun = false;
                _nextDecision = Time.time + _settings.DecisionInterval;
            }

            if (_player == null ||
                !_player.gameObject.activeInHierarchy)
            {
                _doingMelee = false;
                ChangeState(BehaviourState.Wandering);
                _motor.Tick();
                return;
            }

            if (_doingMelee)
            {
                TickMelee();
                return;
            }

            Vector3 difference =
                _player.position - transform.position;

            difference.y = 0f;
            float distance = difference.magnitude;

            if (distance > _settings.VisionDistance)
            {
                ChangeState(BehaviourState.Wandering);
                _motor.Tick();
                return;
            }

            ChangeState(BehaviourState.Chasing);

            _motor.TickChase(
                _player.position,
                _settings.ChaseStoppingDistance);

            if (Time.time < _nextDecision)
                return;

            _nextDecision = Time.time +
                Mathf.Max(0.1f, _settings.DecisionInterval);

            if (_selector.TryChoose(
                distance, Time.time, out BossAttackId attack))
            {
                string range = distance <= _settings.MeleeDistance
                    ? "Melee"
                    : "Dentro de visión";

                Debug.Log(
                    $"[Boss] {range} | " +
                    $"Distancia: {distance:F1} m | " +
                    $"Ataque: {attack}",
                    this);

                if (attack == BossAttackId.AtaqueBasico)
                    BeginMelee(difference);

                AttackSelected?.Invoke(attack);
            }
            else
            {
                Debug.Log(
                    "[Boss] Sin ataques disponibles.",
                    this);
            }
        }

        private void ChangeState(BehaviourState nextState)
        {
            if (_state == nextState)
                return;

            _state = nextState;

            if (nextState == BehaviourState.Wandering)
            {
                _motor.BeginWander();

                Debug.Log(
                    "[Boss] Buscando al jugador.",
                    this);
            }
            else if (nextState == BehaviourState.Chasing)
            {
                _motor.BeginChase();

                Debug.Log(
                    "[Boss] Jugador detectado: persiguiendo.",
                    this);
            }
        }

        public void SetCombatEnabled(bool enabled)
        {
            _combatEnabled = enabled;
            _state = BehaviourState.None;

            if (!enabled)
                StopAction();
        }

        private void OnDisable()
        {
            if (_health)
            {
                _health.StunStarted -= OnStun;
                _health.Died -= StopAction;
            }
            _doingMelee = false;
            if (_motor != null)
                _motor.Stop();

            _state = BehaviourState.None;
        }
    }
}
