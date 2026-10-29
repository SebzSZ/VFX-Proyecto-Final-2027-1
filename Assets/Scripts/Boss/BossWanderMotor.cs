using UnityEngine;
using UnityEngine.AI;

namespace FinalVFX.Boss
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent))]
    public class BossWanderMotor : MonoBehaviour
    {
        [SerializeField] private Transform _arenaCenter;
        [SerializeField, Min(0.1f)] private float _arenaRadius = 12f;
        [SerializeField, Min(0.1f)] private float _sampleDistance = 1f;
        [SerializeField, Min(0.1f)] private float _retryDelay = 1f;
        [SerializeField, Min(1f)] private float _destinationTimeout = 10f;

        [Header("Persecución")]
        [SerializeField, Min(0.05f)]
        private float _chaseRepathInterval = 0.25f;
        private float _nextChaseRepath;
        private float _wanderStoppingDistance;

        private NavMeshAgent _agent;
        private NavMeshPath _path;
        private float _nextAttempt;
        private float _destinationExpires;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _path = new NavMeshPath();
            _wanderStoppingDistance = _agent.stoppingDistance;
        }

        public void Tick()
        {
            if (_arenaCenter == null ||
                !_agent.isActiveAndEnabled ||
                !_agent.isOnNavMesh ||
                _agent.pathPending ||
                Time.time < _nextAttempt)
                return;

            bool followingPath =
                _agent.hasPath &&
                _agent.pathStatus == NavMeshPathStatus.PathComplete &&
                _agent.remainingDistance >
                    _agent.stoppingDistance + 0.15f;

            if (followingPath && Time.time < _destinationExpires)
                return;

            _nextAttempt = Time.time + _retryDelay;

            if (TrySetDestination())
                _destinationExpires = Time.time + _destinationTimeout;
            else
                Stop();
        }

        public void Stop()
        {
            if (_agent != null &&
                _agent.isActiveAndEnabled &&
                _agent.isOnNavMesh)
            {
                _agent.isStopped = true;
                _agent.ResetPath();
            }
        }

        private bool TrySetDestination()
        {
            var filter = new NavMeshQueryFilter
            {
                agentTypeID = _agent.agentTypeID,
                areaMask = _agent.areaMask
            };

            for (int attempt = 0; attempt < 12; attempt++)
            {
                Vector2 offset = Random.insideUnitCircle * _arenaRadius;

                Vector3 candidate = _arenaCenter.position +
                    new Vector3(offset.x, 0f, offset.y);

                if (!NavMesh.SamplePosition(
                        candidate, out NavMeshHit hit,
                        _sampleDistance, filter))
                    continue;

                Vector3 fromCenter = hit.position - _arenaCenter.position;
                fromCenter.y = 0f;

                if (fromCenter.sqrMagnitude >
                    _arenaRadius * _arenaRadius)
                    continue;

                Vector3 fromBoss = hit.position - transform.position;
                fromBoss.y = 0f;

                float minimumTravel = _agent.stoppingDistance + 0.5f;
                if (fromBoss.sqrMagnitude < minimumTravel * minimumTravel)
                    continue;

                if (!_agent.CalculatePath(hit.position, _path) ||
                    _path.status != NavMeshPathStatus.PathComplete)
                    continue;

                if (!_agent.SetPath(_path))
                    continue;

                _agent.isStopped = false;
                return true;
            }

            return false;
        }

        private void OnDisable()
        {
            Stop();
        }

        private void OnDrawGizmosSelected()
        {
            if (_arenaCenter == null)
                return;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(_arenaCenter.position, _arenaRadius);
        }

    public void BeginWander()
        {
            Stop();

            _agent.stoppingDistance = _wanderStoppingDistance;
            _nextAttempt = 0f;
            _destinationExpires = 0f;
        }

        public void BeginChase()
        {
            Stop();
            _nextChaseRepath = 0f;
        }

        public void TickChase(Vector3 targetPosition, float stoppingDistance)
        {
            if (!_agent.isActiveAndEnabled || !_agent.isOnNavMesh)
                return;

            _agent.stoppingDistance = Mathf.Max(0.1f, stoppingDistance);

            Vector3 difference = targetPosition - transform.position;
            difference.y = 0f;

            if (difference.sqrMagnitude <=
                _agent.stoppingDistance * _agent.stoppingDistance)
            {
                Stop();
                _nextChaseRepath = 0f;
                return;
            }

            if (Time.time < _nextChaseRepath)
                return;

            _nextChaseRepath = Time.time +
                Mathf.Max(0.05f, _chaseRepathInterval);

            var filter = new NavMeshQueryFilter
            {
                agentTypeID = _agent.agentTypeID,
                areaMask = _agent.areaMask
            };

            bool foundPosition = NavMesh.SamplePosition(
                targetPosition,
                out NavMeshHit hit,
                _sampleDistance,
                filter);

            if (!foundPosition ||
                !_agent.CalculatePath(hit.position, _path) ||
                _path.status != NavMeshPathStatus.PathComplete)
            {
                Stop();
                return;
            }

            if (!_agent.SetPath(_path))
            {
                Stop();
                return;
            }

            _agent.isStopped = false;
        }
    }
}