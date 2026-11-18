using System.Collections.Generic;
using FinalVFX.Combat;
using FinalVFX.Core;
using UnityEngine;
using Unity.Profiling;

namespace FinalVFX.Boss
{
    // Pool de capacidad fija. La creación solo se realiza durante la precarga.
    public sealed class BossHazardPool : MonoBehaviour
    {
        private BossFirePillar _prefab;
        private Stack<BossFirePillar> _available;
        private List<BossFirePillar> _active;
        private Transform _staging;
        private BossAbility _prewarmData;
        private int _perFrame;
        private int _created;
        private bool _initialized;
        private BossFogBarrier _fog;
        private ArenaIntroFog _fogVisual;
        private float _opacity, _targetOpacity, _fromOpacity, _fadeTime, _fadeDuration, _fadeOut;
        private int _peakActive;
        private int _skipped;
        private static readonly ProfilerMarker PrewarmMarker = new ProfilerMarker("BossPool.Prewarm");
        private static readonly ProfilerMarker RentMarker = new ProfilerMarker("BossPool.Rent");
        private static readonly ProfilerMarker ReturnMarker = new ProfilerMarker("BossPool.Return");

        public int Capacity { get; private set; }
        public int ActiveCount => _active == null ? 0 : _active.Count;
        public int AvailableCount => _available == null ? 0 : _available.Count;
        public bool Ready => _initialized && _created == Capacity;

        public static int RecommendedCapacity(BossAbility data, int maximum)
        {
            maximum = Mathf.Max(1, maximum);
            if (data == null) return 1;
            // Una oleada extra cubre el orden de Update en el frame de devolución.
            double lifetime = Mathf.Max(0f, data.PillarWarning) + Mathf.Max(0f, data.PillarLifetime);
            double interval = Mathf.Max(0.05f, data.WaveInterval);
            double waves = System.Math.Ceiling(lifetime / interval) + 1d;
            double required = System.Math.Max(1, data.PillarsPerWave) * waves;
            return (int)System.Math.Min(maximum, System.Math.Max(1d, required));
        }

        public void Initialize(BossFirePillar pillarPrefab, BossFogBarrier fogPrefab,
            BossAbility data, int capacity, int perFrame)
        {
            if (_initialized) return;
            Capacity = Mathf.Max(1, capacity);
            _perFrame = Mathf.Max(1, perFrame);
            _prefab = pillarPrefab;
            _prewarmData = data;
            _available = new Stack<BossFirePillar>(Capacity);
            _active = new List<BossFirePillar>(Capacity);
            var staging = new GameObject("Prewarm_Inactive");
            staging.transform.SetParent(transform, false);
            staging.SetActive(false);
            _staging = staging.transform;

            _fog = Instantiate(fogPrefab, transform);
            _fog.name = "Fog_Persistent";
            _fog.gameObject.SetActive(true);
            _fog.enabled = false; // Solo el componente barrera; el objeto sigue disponible.
            _fogVisual = _fog.GetComponent<ArenaIntroFog>();
            if (!_fogVisual) _fogVisual = _fog.gameObject.AddComponent<ArenaIntroFog>();
            _fogVisual.SetOpacity(0f);
            _initialized = true;
        }

        private void Update()
        {
            if (!_initialized || FinalVFX.Menu.PauseMenu.IsPaused || Time.deltaTime <= 0f) return;
            if (_created < Capacity)
            {
                using (PrewarmMarker.Auto())
                {
                    int count = Mathf.Min(_perFrame, Capacity - _created);
                    for (int i = 0; i < count; i++)
                    {
                        // El padre inactivo evita OnEnable/Play mientras se prepara el objeto.
                        var pillar = Instantiate(_prefab, _staging);
                        pillar.PrepareForPool(this, _prewarmData);
                        pillar.transform.SetParent(transform, false);
                        _available.Push(pillar);
                        _created++;
                    }
                }
            }
            if (_opacity == _targetOpacity) return;
            _fadeTime += Time.deltaTime;
            float t = _fadeDuration <= 0f ? 1f : Mathf.Clamp01(_fadeTime / _fadeDuration);
            _opacity = Mathf.Lerp(_fromOpacity, _targetOpacity, Mathf.SmoothStep(0f, 1f, t));
            if (t >= 1f) _opacity = _targetOpacity;
            _fogVisual.SetOpacity(_opacity);
        }

        public bool TrySpawn(Vector3 position, BossAbility data, HealthComponent target, CharacterController body)
        {
            if (!Ready || _available.Count == 0) return false;
            using (RentMarker.Auto())
            {
                var pillar = _available.Pop();
                pillar.PoolIndex = _active.Count;
                _active.Add(pillar);
                _peakActive = Mathf.Max(_peakActive, _active.Count);
                pillar.transform.SetPositionAndRotation(position, Quaternion.identity);
                pillar.Initialize(data, target, body);
            }
            return true;
        }

        public void Release(BossFirePillar pillar)
        {
            if (!pillar || pillar.Owner != this || _active == null) return;
            int index = pillar.PoolIndex;
            if (index < 0 || index >= _active.Count || _active[index] != pillar) return;
            using (ReturnMarker.Auto())
            {
                int lastIndex = _active.Count - 1;
                var last = _active[lastIndex];
                _active[index] = last;
                last.PoolIndex = index;
                _active.RemoveAt(lastIndex);
                pillar.PoolIndex = -1;
                pillar.ResetForPool();
                _available.Push(pillar);
            }
        }

        public void NoteSkipped(int count) => _skipped += Mathf.Max(0, count);

        public void ShowFog(Vector3 bossPosition, BossAbility data)
        {
            if (!_fog) return;
            _fog.transform.SetPositionAndRotation(bossPosition + Vector3.up * (data.FogHeight * 0.5f), Quaternion.identity);
            _fog.Configure(data.FogRadius, data.FogHeight);
            _fog.enabled = true;
            _fadeOut = Mathf.Max(0f, data.FogFadeOut);
            FadeTo(1f, data.FogFadeIn);
        }

        private void FadeTo(float target, float seconds)
        {
            if (_targetOpacity == target) return; // Cancel/Finish no reinician un fundido en curso.
            _targetOpacity = target;
            _fromOpacity = _opacity;
            _fadeTime = 0f;
            _fadeDuration = Mathf.Max(0f, seconds);
            if (_fadeDuration <= 0f)
            {
                _opacity = target;
                if (_fogVisual) _fogVisual.SetOpacity(_opacity);
            }
        }

        public void ClearHazards(bool immediate = false)
        {
            if (_active != null)
                while (_active.Count > 0)
                {
                    var last = _active[_active.Count - 1];
                    if (!last) _active.RemoveAt(_active.Count - 1);
                    else Release(last);
                }
            if (_fog) _fog.enabled = false;
            if (immediate)
            {
                _opacity = _targetOpacity = _fromOpacity = 0f;
                _fadeTime = _fadeDuration = 0f;
                if (_fogVisual) _fogVisual.SetOpacity(0f);
            }
            else FadeTo(0f, _fadeOut);
        }

        private void OnDisable() => ClearHazards(true);

        [ContextMenu("Mostrar estadísticas del pool")]
        private void PrintStats()
        {
            Debug.Log($"[BossPool] Ready={Ready} | creados={_created}/{Capacity} | activos={ActiveCount} | libres={AvailableCount} | pico={_peakActive} | omitidos por capacidad={_skipped} | opacidad={_opacity:F2}", this);
        }
    }
}
