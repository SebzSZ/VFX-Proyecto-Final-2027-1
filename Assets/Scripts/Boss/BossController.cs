using System;
using UnityEngine;

namespace FinalVFX.Boss
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BossWanderMotor))]
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

        private BossWanderMotor _motor;
        private BossDecisionSelector _selector;
        private BehaviourState _state;
        private float _nextDecision;

        public event Action<BossAttackId> AttackSelected;

        private void Awake()
        {
            _motor = GetComponent<BossWanderMotor>();

            if (_player == null || _settings == null)
            {
                Debug.LogError(
                    $"{name}: asigna Player y Settings en BossController.",
                    this);

                enabled = false;
                return;
            }

            _selector = new BossDecisionSelector(
                _settings, _decisionSeed);
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

            if (_player == null ||
                !_player.gameObject.activeInHierarchy)
            {
                ChangeState(BehaviourState.Wandering);
                _motor.Tick();
                return;
            }

            Vector3 difference =
                _player.position - transform.position;

            difference.y = 0f;
            float distance = difference.magnitude;

            // Sin detección: vagar y salir antes de elegir ataques.
            if (distance > _settings.VisionDistance)
            {
                ChangeState(BehaviourState.Wandering);
                _motor.Tick();
                return;
            }

            // Con detección: perseguir y seguir tomando decisiones.
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
                    "[Guardián] Jugador detectado: persiguiendo.",
                    this);
            }
        }

        public void SetCombatEnabled(bool enabled)
        {
            _combatEnabled = enabled;
            _state = BehaviourState.None;

            if (!enabled)
                _motor.Stop();
        }

        private void OnDisable()
        {
            if (_motor != null)
                _motor.Stop();

            _state = BehaviourState.None;
        }
    }
}