using System;
using System.Collections.Generic;
using FinalVFX.Combat;
using FinalVFX.Player;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Unity.Profiling;

namespace FinalVFX.Boss
{
    [RequireComponent(typeof(BossWanderMotor), typeof(GuardianHealth), typeof(NavMeshAgent))]
    public sealed class BossController : MonoBehaviour
    {
        public enum ActionPhase { Locomotion, Preparation, Execution, Recovery, Stunned, Dead }
        [SerializeField] private Transform _player;
        [SerializeField] private BossAISettings _settings;
        [SerializeField] private bool _combatEnabled;
        [SerializeField] private int _decisionSeed;
        [Header("Habilidades y animación")]
        [SerializeField] private BossAbilitySettings _abilities;
        [SerializeField] private Animator _animator;
        [SerializeField] private Transform _rayOrigin;
        [SerializeField] private DirectDamage _meleeDamage;
        [Header("Escenario")]
        [SerializeField] private Transform _arenaCenter;
        [SerializeField, Min(0.1f)] private float _arenaRadius;
        [SerializeField] private LayerMask _groundMask;
        [SerializeField] private LayerMask _obstacleMask;
        [SerializeField, Min(0.1f)] private float _groundProbeHeight;
        [Header("Recursos")]
        [SerializeField] private BossFogBarrier _fogPrefab;
        [SerializeField] private BossFirePillar _pillarPrefab;
        [Header("Precarga de Lanza Primordial")]
        [Tooltip("Límite de pilares simultáneos. Se precarga la cantidad calculada a partir de las oleadas, hasta este máximo. Se configura antes de Play.")]
        [SerializeField, Min(1)] private int _pillarPoolMaximum;
        [Tooltip("Pilares creados por frame durante la precarga inicial. El jefe espera a que termine la precarga antes de seleccionar ataques.")]
        [SerializeField, Min(1)] private int _pillarPrewarmPerFrame;
        [Header("Prueba desde el menú contextual")]
        [SerializeField] private BossAttackId _testAttack;

        private GuardianHealth _health;
        private HealthComponent _playerHealth;
        private PlayerController _playerController;
        private CharacterController _playerBody;
        private BossWanderMotor _motor;
        private NavMeshAgent _agent;
        private BossDecisionSelector _selector;
        private BoxCollider _meleeBox;
        private BossAbility _attack;
        private float _phaseTime, _nextDecision, _nextWave;
        private bool _hitPlayer, _arrived, _wasStunned, _manualPosition;
        private int _movementState;
        private Vector3 _origin, _end, _forward, _lastGround;
        private BossHazardPool _hazards;
        private readonly List<Vector3> _wavePositions = new List<Vector3>();
        private static readonly ProfilerMarker SpawnWaveMarker = new ProfilerMarker("Boss.Lanza.SpawnWave");
        private AnimatorOverrideController _overrides;
        private readonly Dictionary<BossAttackId, int> _attackHashes = new Dictionary<BossAttackId, int>();
        private static readonly int Speed = Animator.StringToHash("Speed");
        private static readonly int Locomotion = Animator.StringToHash("Base Layer.Locomotion");
        private static readonly int Stunned = Animator.StringToHash("Base Layer.Stunned");
        public ActionPhase Phase { get; private set; }
        public event Action<BossAttackId> AttackSelected;

        private NavMeshQueryFilter Filter => new NavMeshQueryFilter
        { agentTypeID = _agent.agentTypeID, areaMask = _agent.areaMask };
        private bool PlayerReady => _player && _player.gameObject.activeInHierarchy && _playerBody &&
            _playerBody.enabled && _playerHealth && !_playerHealth.IsDead;

        private void Awake()
        {
            _motor = GetComponent<BossWanderMotor>();
            _agent = GetComponent<NavMeshAgent>();
            _health = GetComponent<GuardianHealth>();
            _playerHealth = _player.GetComponent<HealthComponent>();
            _playerController = _player.GetComponent<PlayerController>();
            _playerBody = _player.GetComponent<CharacterController>();
            _meleeBox = _meleeDamage ? _meleeDamage.GetComponent<BoxCollider>() : null;
 
            foreach (BossAttackId id in Enum.GetValues(typeof(BossAttackId)))
            {
                _attackHashes[id] = Animator.StringToHash("Base Layer." + id);
                BossAbility a = _abilities.Find(id);
                if (a == null || a.Preparation <= 0f || a.Execution <= 0f || a.Recovery <= 0f)
                {
                    enabled = false;
                    return;
                }
            }
            _meleeBox.enabled = false;
            _animator.applyRootMotion = false;
            ApplyClips();
            _selector = new BossDecisionSelector(_settings, _decisionSeed);
            _lastGround = transform.position;
            InitializeHazards();
        }


        private void InitializeHazards()
        {
            BossAbility data = _abilities.Find(BossAttackId.LanzaPrimordial);
            int capacity = BossHazardPool.RecommendedCapacity(data, _pillarPoolMaximum);
            var host = new GameObject(name + "_HazardPool");
            SceneManager.MoveGameObjectToScene(host, gameObject.scene);
            _hazards = host.AddComponent<BossHazardPool>();
            _hazards.Initialize(_pillarPrefab, _fogPrefab, data, capacity, _pillarPrewarmPerFrame);
            _wavePositions.Capacity = capacity;
        }

        private void OnEnable()
        {
            if (!_health) return;
            _health.StunStarted += OnStun;
            _health.Died += OnDeath;
        }
        private void Start() { if (enabled) PlayLocomotion(); }

        private void ApplyClips()
        {
            _overrides = new AnimatorOverrideController(_animator.runtimeAnimatorController);
            var replacements = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            _overrides.GetOverrides(replacements);
            for (int i = 0; i < replacements.Count; i++)
            {
                string slot = replacements[i].Key.name;
                AnimationClip clip = slot == "Slot_Idle" ? _abilities.Idle : slot == "Slot_Walk" ? _abilities.Walk : slot == "Slot_Stunned" ? _abilities.Stunned : null;
                foreach (BossAttackId id in Enum.GetValues(typeof(BossAttackId)))
                    if (slot == "Slot_" + id) clip = _abilities.Find(id)?.Clip;
                if (clip)
                    replacements[i] = new KeyValuePair<AnimationClip, AnimationClip>(replacements[i].Key, clip);
            }
            _overrides.ApplyOverrides(replacements);
            _animator.runtimeAnimatorController = _overrides;
        }

        private void Update()
        {
            if (FinalVFX.Menu.PauseMenu.IsPaused || Time.deltaTime <= 0f) return;
            if (!_combatEnabled || _health.IsDead) { _motor.Stop(); SetSpeed(0f); return; }
            if (_health.Stunned) { SetSpeed(0f); return; }
            if (_wasStunned)
            {
                _wasStunned = false;
                Finish();
            }
            if (!PlayerReady)
            {
                if (_attack != null) Cancel();
                _motor.Stop();
                PlayLocomotion();
                SetSpeed(0f);
                return;
            }
            if (_attack != null) { TickAttack(Time.deltaTime); return; }
            Phase = ActionPhase.Locomotion;
            float distance = Planar(_player.position - transform.position).magnitude;
            if (distance > _settings.VisionDistance)
            {
                if (_movementState != 1) { _motor.BeginWander(); _movementState = 1; }
                _motor.Tick();
            }
            else
            {
                if (_movementState != 2) { _motor.BeginChase(); _movementState = 2; }
                _motor.TickChase(_player.position, _settings.ChaseStoppingDistance);
                if (Time.time >= _nextDecision && _hazards && _hazards.Ready)
                {
                    _nextDecision = Time.time + _settings.DecisionInterval;
                    if (_selector.TryChoose(distance, Time.time, out var id)) Begin(id);
                }
            }
            SetSpeed(_attack == null ? Planar(_agent.velocity).magnitude : 0f);
        }

        private void Begin(BossAttackId id)
        {
            if (!_hazards || !_hazards.Ready) return;
            BossAbility data = _abilities.Find(id);
            if (data == null || !PlayerReady || !_agent.isActiveAndEnabled || !_agent.isOnNavMesh) return;
            Cancel();
            _attack = data;
            _phaseTime = 0f;
            _hitPlayer = _arrived = false;
            _origin = _lastGround = transform.position;
            _agent.updateRotation = false;
            Phase = ActionPhase.Preparation;
            FacePlayer();
            if (id == BossAttackId.Blasfemia)
            {
                _agent.updatePosition = false;
                _manualPosition = true;
            }
            _animator.speed = data.Clip ? data.Clip.length / Mathf.Max(0.01f, data.Total) : 1f;
            _animator.Play(_attackHashes[id], 0, 0f);
            SetSpeed(0f);
            AttackSelected?.Invoke(id);
        }

        private void TickAttack(float dt)
        {
            for (int transition = 0; transition < 4 && _attack != null && dt > 0f; transition++)
            {
                float duration = Phase == ActionPhase.Preparation ? _attack.Preparation :
                    Phase == ActionPhase.Execution ? _attack.Execution : _attack.Recovery;
                float slice = Mathf.Min(dt, Mathf.Max(0f, duration - _phaseTime));
                _phaseTime += slice;
                dt -= slice;
                if (Phase == ActionPhase.Preparation)
                {
                    if (_attack.Id == BossAttackId.BarridoCelestial || _attack.Id == BossAttackId.AnteMi ||
                        _attack.Id == BossAttackId.Blasfemia) FacePlayer();
                    if (_attack.Id == BossAttackId.Blasfemia)
                        transform.position = _origin + Vector3.up * (_attack.RiseHeight * Mathf.SmoothStep(0f, 1f, _phaseTime / duration));
                }
                else if (Phase == ActionPhase.Execution) ExecuteTick();
                if (_phaseTime + 0.00001f < duration) break;
                _phaseTime = 0f;
                if (Phase == ActionPhase.Preparation)
                {
                    Phase = ActionPhase.Execution;
                    EnterExecution();
                }
                else if (Phase == ActionPhase.Execution)
                {
                    ClearHazards();
                    Phase = ActionPhase.Recovery;
                }
                else Finish();
            }
        }

        private void EnterExecution()
        {
            _forward = transform.forward;
            _origin = transform.position;
            switch (_attack.Id)
            {
                case BossAttackId.AnteMi:
                    Shoot();
                    break;
                case BossAttackId.EnGarde:
                    AreaHit(false, true);
                    break;
                case BossAttackId.LanzaPrimordial:
                    Push(_attack.PushDistance);
                    _hazards.ShowFog(transform.position, _attack);
                    _nextWave = 0f;
                    SpawnWave();
                    _nextWave = Mathf.Max(0.05f, _attack.WaveInterval);
                    break;
                case BossAttackId.Blasfemia:
                    PlanDive();
                    break;
            }
        }

        private void ExecuteTick()
        {
            switch (_attack.Id)
            {
                case BossAttackId.BarridoCelestial:
                    if (!_arrived)
                    {
                        float fraction = Mathf.Clamp01(_phaseTime / (_attack.Execution * Mathf.Clamp(_attack.DashFraction, 0.05f, 1f)));
                        Vector3 desired = _origin + _forward * (_attack.TravelDistance * fraction);
                        bool blocked = MoveGround(desired);
                        _arrived = blocked || fraction >= 1f;
                    }
                    if (_arrived) AreaHit(true, false);
                    break;
                case BossAttackId.AtaqueBasico:
                    MeleeHit();
                    break;
                case BossAttackId.Blasfemia:
                    float t = _phaseTime + 0.00001f >= _attack.Execution ? 1f : Mathf.Clamp01(_phaseTime / _attack.Execution);
                    transform.position = Vector3.Lerp(_origin, _end, t);
                    if (t >= 1f && !_arrived)
                    {
                        _arrived = true;
                        _lastGround = _end;
                        RestorePosition();
                        AreaHit(false, false);
                    }
                    break;
                case BossAttackId.LanzaPrimordial:
                    if (_phaseTime >= _nextWave && _phaseTime + _attack.PillarWarning < _attack.Execution)
                    {
                        SpawnWave();
                        _nextWave = _phaseTime + Mathf.Max(0.05f, _attack.WaveInterval);
                    }
                    break;
            }
        }

        private bool MoveGround(Vector3 desired)
        {
            Vector3 from = transform.position;
            Vector3 delta = Planar(desired - from);
            float distance = delta.magnitude;
            if (distance < 0.00001f) return false;
            Vector3 direction = delta / distance;
            float allowed = distance;
            float radius = Mathf.Max(0.05f, _agent.radius);
            Vector3 bottom = from + Vector3.up * (radius + 0.1f);
            Vector3 top = from + Vector3.up * Mathf.Max(radius + 0.1f, _agent.height - radius);
            Physics.SyncTransforms();
            int mask = _obstacleMask | (1 << _playerBody.gameObject.layer);
            foreach (var hit in Physics.CapsuleCastAll(bottom, top, radius, direction, distance + 0.03f, mask, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;
                allowed = Mathf.Min(allowed, Mathf.Max(0f, hit.distance - 0.03f));
            }
            Vector3 target = from + direction * allowed;
            if (NavMesh.Raycast(from, target, out var edge, Filter))
                target = Vector3.MoveTowards(edge.position, from, 0.03f);
            if (!NavMesh.SamplePosition(target, out var sample, 0.3f, Filter)) return true;
            bool blocked = Vector3.Distance(sample.position, desired) > 0.05f;
            if (!_agent.Warp(sample.position)) return true;
            _lastGround = transform.position;
            return blocked;
        }

        private void PlanDive()
        {
            Vector3 planar = Planar(_player.position - _origin);
            planar = Vector3.ClampMagnitude(planar, _attack.TravelDistance);
            Vector3 aim = new Vector3(_origin.x + planar.x, _lastGround.y, _origin.z + planar.z);
            if (Physics.Raycast(aim + Vector3.up * _groundProbeHeight, Vector3.down, out var floor,
                _groundProbeHeight * 2f, _groundMask, QueryTriggerInteraction.Ignore)) aim = floor.point;
            Vector3 vector = aim - _origin;
            _end = _lastGround;
            if (vector.sqrMagnitude > 0.001f && Physics.Raycast(_origin + Vector3.up * 0.03f,
                vector.normalized, out var hit, vector.magnitude + 0.2f, _groundMask | _obstacleMask,
                QueryTriggerInteraction.Ignore) && Vector3.Dot(hit.normal, Vector3.up) > 0.5f &&
                NavMesh.SamplePosition(hit.point, out var nav, 0.3f, Filter) &&
                !NavMesh.Raycast(_lastGround, nav.position, out _, Filter))
                _end = nav.position;
            _forward = (_end - _origin).normalized;
        }

        private void Shoot()
        {
            Physics.SyncTransforms();
            Vector3 origin = _rayOrigin.position;
            Vector3 direction = (_playerBody.bounds.center - origin).normalized;
            RaycastHit closest = default;
            float distance = float.PositiveInfinity;
            int mask = _obstacleMask | (1 << _playerBody.gameObject.layer);
            foreach (var hit in Physics.RaycastAll(origin, direction, _attack.RayDistance, mask, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform) || hit.distance >= distance) continue;
                closest = hit;
                distance = hit.distance;
            }
            if (!closest.collider || closest.collider.GetComponentInParent<PlayerController>() != _playerController || !Damage()) return;
            Vector3 toBoss = Planar(transform.position - _player.position);
            float travel = Mathf.Max(0f, toBoss.magnitude - _attack.PullStopDistance);
            _playerController.ApplyBossControl(toBoss.normalized * travel, _attack.ForceSeconds, _attack.StunSeconds);
        }

        private bool InArea(bool frontal)
        {
            Bounds b = _playerBody.bounds;
            Vector3 delta = Planar(b.center - transform.position);
            float r = _attack.Radius + Mathf.Max(b.extents.x, b.extents.z);
            return delta.sqrMagnitude <= r * r && b.max.y >= transform.position.y &&
                b.min.y <= transform.position.y + _attack.Height &&
                (!frontal || Vector3.Dot(delta, transform.forward) >= 0f);
        }
        private bool Damage() => PlayerReady && _playerHealth.ReceiveHit(_attack.Damage, _attack.HitInvulnerability).Total > 0f;
        private void AreaHit(bool frontal, bool push)
        {
            if (_hitPlayer || !PlayerReady || !InArea(frontal)) return;
            _hitPlayer = true;
            if (Damage() && push) Push(_attack.PushDistance);
        }
        private void Push(float distance)
        {
            if (!PlayerReady) return;
            Vector3 away = Planar(_player.position - transform.position);
            if (away.sqrMagnitude < 0.001f) away = transform.forward;
            _playerController.ApplyBossControl(away.normalized * distance, _attack.ForceSeconds, 0f);
        }
        private void MeleeHit()
        {
            if (_hitPlayer) return;
            Transform box = _meleeBox.transform;
            Vector3 s = box.lossyScale;
            Vector3 half = Vector3.Scale(_meleeBox.size, new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z))) * 0.5f;
            Physics.SyncTransforms();
            foreach (var c in Physics.OverlapBox(box.TransformPoint(_meleeBox.center), half, box.rotation,
                _meleeDamage.TargetLayers, QueryTriggerInteraction.Collide))
                if (c.GetComponentInParent<PlayerController>() == _playerController && _meleeDamage.TryMeleeHit(c))
                { _hitPlayer = true; break; }
        }

        private void SpawnWave()
        {
            using (SpawnWaveMarker.Auto())
            {
                if (!_hazards || !_hazards.Ready) return;
                _wavePositions.Clear();
                int requested = Mathf.Max(1, _attack.PillarsPerWave);
                int count = Mathf.Min(requested, _hazards.AvailableCount);
                _hazards.NoteSkipped(requested - count);
                int nearCount = Mathf.CeilToInt(count * Mathf.Clamp01(_attack.NearPillarFraction));
                float innerRadius = _attack.FogRadius + _attack.PillarRadius + 0.1f;
                float outerRadius = Mathf.Max(innerRadius + 0.1f, _attack.PillarNearRadius);
                for (int i = 0; i < count; i++)
                {
                    for (int attempt = 0; attempt < 30; attempt++)
                    {
                        Vector3 p;
                        bool nearBoss = i < nearCount;
                        if (nearBoss)
                        {
                            float angle = UnityEngine.Random.value * Mathf.PI * 2f;
                            float radius = Mathf.Sqrt(Mathf.Lerp(innerRadius * innerRadius,
                                outerRadius * outerRadius, UnityEngine.Random.value));
                            p = transform.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                        }
                        else
                        {
                            Vector2 offset = UnityEngine.Random.insideUnitCircle * _arenaRadius;
                            p = _arenaCenter.position + new Vector3(offset.x, 0f, offset.y);
                        }
                        if (!Physics.Raycast(p + Vector3.up * _groundProbeHeight, Vector3.down, out var ground,
                            _groundProbeHeight * 2f, _groundMask, QueryTriggerInteraction.Ignore)) continue;
                        if (!NavMesh.SamplePosition(ground.point, out var nav, 0.4f, Filter)) continue;
                        p = nav.position;
                        if (nearBoss && Planar(p - transform.position).magnitude > outerRadius) continue;
                        if (Planar(p - _arenaCenter.position).magnitude + _attack.PillarRadius > _arenaRadius ||
                            Planar(p - transform.position).magnitude < _attack.FogRadius + _attack.PillarRadius) continue;
                        bool overlaps = false;
                        float separationSquared = 4f * _attack.PillarRadius * _attack.PillarRadius;
                        for (int j = 0; j < _wavePositions.Count; j++)
                        {
                            if (Planar(_wavePositions[j] - p).sqrMagnitude < separationSquared)
                            { overlaps = true; break; }
                        }
                        if (overlaps) continue;
                        if (!_hazards.TrySpawn(p, _attack, _playerHealth, _playerBody)) return;
                        _wavePositions.Add(p);
                        break;
                    }
                }
            }
        }

        private void FacePlayer()
        {
            Vector3 delta = Planar(_player.position - transform.position);
            if (delta.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(delta);
        }
        private static Vector3 Planar(Vector3 v) { v.y = 0f; return v; }
        private void SetSpeed(float speed) { if (_animator) _animator.SetFloat(Speed, speed); }
        private void PlayLocomotion()
        {
            if (!_animator || !_animator.runtimeAnimatorController) return;
            _animator.speed = 1f;
            if (!_animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Locomotion"))
                _animator.Play(Locomotion, 0, 0f);
        }
        private void RestorePosition()
        {
            if (!_manualPosition) return;
            _manualPosition = false;
            transform.position = _lastGround;
            if (_agent && _agent.isActiveAndEnabled) _agent.Warp(_lastGround);
            if (_agent) _agent.updatePosition = true;
        }
        private void ClearHazards()
        {
            if (_hazards) _hazards.ClearHazards();
        }
        private void Cancel()
        {
            ClearHazards();
            RestorePosition();
            _attack = null;
            _movementState = 0;
            if (_motor) _motor.Stop();
            if (_agent) _agent.updateRotation = true;
            SetSpeed(0f);
        }
        private void Finish()
        {
            Cancel();
            Phase = ActionPhase.Locomotion;
            _nextDecision = Time.time + _settings.DecisionInterval;
            PlayLocomotion();
        }
        private void OnStun(float distance)
        {
            Cancel();
            _wasStunned = true;
            Phase = ActionPhase.Stunned;
            _animator.speed = 1f;
            _animator.Play(Stunned, 0, 0f);
            if (PlayerReady)
            {
                Vector3 away = Planar(_player.position - transform.position);
                if (away.sqrMagnitude < 0.001f) away = transform.forward;
                _playerController.ApplyBossControl(away.normalized * distance, 0.25f, 0f);
            }
        }
        private void OnDeath()
        {
            Cancel();
            Phase = ActionPhase.Dead;
            if (_animator) _animator.speed = 0f;
        }
        public void SetCombatEnabled(bool value)
        {
            _combatEnabled = value;
            Cancel();
            if (_health && !_health.IsDead && !_health.Stunned) PlayLocomotion();
        }

        private void OnDisable()
        {
            if (_health) { _health.StunStarted -= OnStun; _health.Died -= OnDeath; }
            Cancel();
            if (_hazards) _hazards.ClearHazards(true);
        }
        private void OnDestroy()
        {
            if (_hazards)
            {
                _hazards.ClearHazards(true);
                Destroy(_hazards.gameObject);
            }
            if (_overrides) Destroy(_overrides);
        }
    }
}

