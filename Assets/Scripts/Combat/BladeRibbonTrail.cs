using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace FinalVFX.Combat
{
    [DisallowMultipleComponent]
    public sealed class BladeRibbonTrail : MonoBehaviour
    {
        [SerializeField] private Transform[] _bladeProfile;
        [SerializeField] private Material _trailMaterial;
        [SerializeField, Min(0.01f)] private float _lifetime = 0.22f;
        [SerializeField, Range(4, 128)] private int _maxSamples = 64;
        [SerializeField, Min(0f)] private float _minMovement = 0.005f;
        [SerializeField, Min(0.1f)] private float _breakDistance = 2f;
        [SerializeField, Min(0.1f)] private float _fadePower = 1.5f;

        private sealed class Sample
        {
            public float time;
            public Vector3[] points;
        }

        private Sample[] _samples;
        private int _head;
        private int _count;
        private Mesh _mesh;
        private MeshRenderer _renderer;
        private Transform _meshTransform;
        private bool _ready;
        private bool _emitting;
        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<Vector2> _uvs = new List<Vector2>();
        private readonly List<Color> _colors = new List<Color>();
        private readonly List<int> _triangles = new List<int>();

        private void Awake()
        {
            if (!_trailMaterial || _bladeProfile == null || _bladeProfile.Length < 2)
            {
                Debug.LogError("Asigna Trail Material y al menos dos puntos del filo.", this);
                return;
            }
            foreach (Transform point in _bladeProfile)
            {
                if (!point || !point.IsChildOf(transform))
                {
                    Debug.LogError("Cada punto del perfil debe ser un hijo del arma.", this);
                    return;
                }
            }
            _samples = new Sample[Mathf.Clamp(_maxSamples, 4, 128)];
            for (int i = 0; i < _samples.Length; i++)
                _samples[i] = new Sample { points = new Vector3[_bladeProfile.Length] };

            var child = new GameObject("Ultimate Blade Ribbon", typeof(MeshFilter), typeof(MeshRenderer));
            child.layer = gameObject.layer;
            child.transform.SetParent(transform, false);
            _meshTransform = child.transform;
            _mesh = new Mesh { name = "Blade ribbon runtime" };
            _mesh.MarkDynamic();
            child.GetComponent<MeshFilter>().sharedMesh = _mesh;
            _renderer = child.GetComponent<MeshRenderer>();
            _renderer.sharedMaterial = _trailMaterial;
            _renderer.shadowCastingMode = ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            _renderer.enabled = false;
            _ready = true;
        }

        public void SetEmitting(bool emitting)
        {
            if (emitting && !_emitting)
                Clear();
            _emitting = emitting;
        }

        public void Clear()
        {
            _head = 0;
            _count = 0;
            if (_mesh)
                _mesh.Clear();
            if (_renderer)
                _renderer.enabled = false;
        }

        // Llamado por WeaponUltimateVisual después de evaluar la animación.
        public void Tick(float now)
        {
            if (!_ready)
                return;

            while (_count > 0 && now - _samples[_head].time >= _lifetime)
            {
                _head = (_head + 1) % _samples.Length;
                _count--;
            }
            if (_emitting && Time.deltaTime > 0f)
                Capture(now);
            Rebuild(now);
        }

        private void Capture(float now)
        {
            if (_count > 0)
            {
                Sample last = _samples[(_head + _count - 1) % _samples.Length];
                float moved = 0f;
                for (int i = 0; i < _bladeProfile.Length; i++)
                    moved = Mathf.Max(moved, Vector3.Distance(last.points[i], _bladeProfile[i].position));
                if (moved > _breakDistance)
                    Clear();
                else if (moved < _minMovement)
                    return;
            }
            if (_count == _samples.Length)
            {
                _head = (_head + 1) % _samples.Length;
                _count--;
            }
            Sample sample = _samples[(_head + _count) % _samples.Length];
            sample.time = now;
            for (int i = 0; i < _bladeProfile.Length; i++)
                sample.points[i] = _bladeProfile[i].position;
            _count++;
        }

        private void Rebuild(float now)
        {
            _vertices.Clear();
            _uvs.Clear();
            _colors.Clear();
            _triangles.Clear();
            int width = _bladeProfile.Length;
            for (int row = 0; row < _count; row++)
            {
                Sample sample = _samples[(_head + row) % _samples.Length];
                float age = Mathf.Clamp01((now - sample.time) / Mathf.Max(0.01f, _lifetime));
                float alpha = Mathf.Pow(1f - age, _fadePower);
                for (int column = 0; column < width; column++)
                {
                    _vertices.Add(_meshTransform.InverseTransformPoint(sample.points[column]));
                    _uvs.Add(new Vector2(age, column / (float)(width - 1)));
                    _colors.Add(new Color(1f, 1f, 1f, alpha));
                    if (row == 0 || column == 0)
                        continue;
                    int current = row * width + column;
                    int previous = current - width;
                    _triangles.Add(previous - 1);
                    _triangles.Add(previous);
                    _triangles.Add(current);
                    _triangles.Add(previous - 1);
                    _triangles.Add(current);
                    _triangles.Add(current - 1);
                }
            }
            _mesh.Clear();
            _mesh.SetVertices(_vertices);
            _mesh.SetUVs(0, _uvs);
            _mesh.SetColors(_colors);
            _mesh.SetTriangles(_triangles, 0);
            _mesh.RecalculateBounds();
            _renderer.enabled = _count >= 2;
        }

        private void OnDisable()
        {
            _emitting = false;
            Clear();
        }

        private void OnDestroy()
        {
            if (_mesh)
                Destroy(_mesh);
        }

        private void OnDrawGizmosSelected()
        {
            if (_bladeProfile == null)
                return;
            Gizmos.color = Color.cyan;
            for (int i = 0; i < _bladeProfile.Length; i++)
            {
                if (!_bladeProfile[i])
                    continue;
                Gizmos.DrawSphere(_bladeProfile[i].position, 0.015f);
                if (i > 0 && _bladeProfile[i - 1])
                    Gizmos.DrawLine(_bladeProfile[i - 1].position, _bladeProfile[i].position);
            }
        }
    }
}
