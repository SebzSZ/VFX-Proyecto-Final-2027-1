using UnityEngine;
using UnityEngine.VFX;
using FinalVFX.Combat;

namespace FinalVFX.Boss
{
    public sealed class BossFirePillar : MonoBehaviour
    {
        [SerializeField] private VisualEffect _fire;
        [SerializeField] private LineRenderer _warning;
        private BossAbility _data;
        private HealthComponent _target;
        private CharacterController _body;
        private float _elapsed, _nextHit;
        private bool _ignited, _leased;
        private Vector3 _previousCenter;
        private Vector3[] _ring;
        private float _cachedRadius = -1f, _cachedHeight = -1f;
        private static readonly int BreathLifetime = Shader.PropertyToID("BreathLifetime");
        private static readonly int BreathSpeed = Shader.PropertyToID("BreathSpeed");
        public BossHazardPool Owner { get; private set; }
        public int PoolIndex { get; internal set; } = -1;

        public void PrepareForPool(BossHazardPool owner, BossAbility data)
        {
            Owner = owner;
            _ring = new Vector3[48];
            _warning.positionCount = _ring.Length;
            _warning.loop = true;
            _warning.useWorldSpace = false;
            if (data != null) ConfigureVisuals(data);
            ResetForPool();
        }

        private void ConfigureVisuals(BossAbility data)
        {
            if (_ring == null) _ring = new Vector3[48];
            if (_cachedRadius != data.PillarRadius)
            {
                _cachedRadius = data.PillarRadius;
                for (int i = 0; i < _ring.Length; i++)
                {
                    float angle = i * Mathf.PI * 2f / _ring.Length;
                    _ring[i] = new Vector3(Mathf.Cos(angle) * _cachedRadius, 0.04f, Mathf.Sin(angle) * _cachedRadius);
                }
                _warning.positionCount = _ring.Length;
                _warning.loop = true;
                _warning.useWorldSpace = false;
                _warning.SetPositions(_ring);
            }
            if (_cachedHeight != data.PillarHeight)
            {
                _cachedHeight = data.PillarHeight;
                if (_fire.HasFloat(BreathLifetime)) _fire.SetFloat(BreathLifetime, 0.6f);
                if (_fire.HasFloat(BreathSpeed)) _fire.SetFloat(BreathSpeed, _cachedHeight / 0.6f);
            }
        }

        public void Initialize(BossAbility data, HealthComponent target, CharacterController body)
        {
            // Reiniciar todos los valores transitorios antes de reactivar el objeto.
            _data = data;
            _target = target;
            _body = body;
            _elapsed = _nextHit = 0f;
            _ignited = false;
            _previousCenter = body ? body.bounds.center : transform.position;
            ConfigureVisuals(data);
            _fire.gameObject.SetActive(false);
            _warning.gameObject.SetActive(true);
            _leased = true;
            gameObject.SetActive(true);
        }

        public void ResetForPool()
        {
            _leased = false;
            _data = null;
            _target = null;
            _body = null;
            _elapsed = _nextHit = 0f;
            _ignited = false;
            _previousCenter = default;
            if (_fire)
            {
                if (_fire.isActiveAndEnabled) _fire.Stop();
                _fire.gameObject.SetActive(false);
            }
            if (_warning) _warning.gameObject.SetActive(false);
            gameObject.SetActive(false);
        }

        private void ReturnToPool()
        {
            if (Owner) Owner.Release(this);
            else ResetForPool();
        }

        private void OnDisable()
        {
            if (_leased) ReturnToPool();
        }

        private void Update()
        {
            if (!_leased || _data == null || FinalVFX.Menu.PauseMenu.IsPaused || Time.deltaTime <= 0f) return;
            _elapsed += Time.deltaTime;
            if (_elapsed >= _data.PillarWarning + _data.PillarLifetime)
            {
                ReturnToPool();
                return;
            }
            if (_elapsed < _data.PillarWarning)
            {
                if (_body) _previousCenter = _body.bounds.center;
                return;
            }
            if (!_ignited)
            {
                _ignited = true;
                _warning.gameObject.SetActive(false);
                _fire.gameObject.SetActive(true);
                // Reinit limpia partículas y el tiempo de simulación de la utilización anterior.
                // Reinit envía el evento inicial del VFX (OnPlay en este prefab).
                _fire.Reinit();
                if (_body) _previousCenter = _body.bounds.center;
            }
            if (!_body || !_body.enabled || !_target || _target.IsDead) return;
            Bounds b = _body.bounds;
            Vector3 p = transform.position;
            Vector2 from = new Vector2(_previousCenter.x - p.x, _previousCenter.z - p.z);
            Vector2 to = new Vector2(b.center.x - p.x, b.center.z - p.z);
            Vector2 segment = to - from;
            float t = segment.sqrMagnitude > 0.00001f ? Mathf.Clamp01(-Vector2.Dot(from, segment) / segment.sqrMagnitude) : 1f;
            Vector2 horizontal = from + segment * t;
            float centerY = Mathf.Lerp(_previousCenter.y, b.center.y, t);
            _previousCenter = b.center;
            float radius = _data.PillarRadius + Mathf.Max(b.extents.x, b.extents.z);
            if (_elapsed >= _nextHit && horizontal.sqrMagnitude <= radius * radius &&
                centerY + b.extents.y >= p.y && centerY - b.extents.y <= p.y + _data.PillarHeight)
            {
                if (_target.ReceiveHit(_data.Damage, _data.HitInvulnerability).Total > 0f)
                    _nextHit = _elapsed + Mathf.Max(0.05f, _data.PillarHitInterval);
            }
        }
    }
}
