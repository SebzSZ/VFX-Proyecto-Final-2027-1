using UnityEngine;

namespace FinalVFX.Combat
{
    public sealed class WeaponDisplay : MonoBehaviour
    {
        [SerializeField] private WeaponDefinition _definition;
        [SerializeField] private Transform _focusPoint;
        [SerializeField] private Renderer[] _renderers;
        [Tooltip("Dissolve materials, in renderer order and then material slot order.")]
        [SerializeField] private Material[] _dissolveMaterials;
        private bool _dissolving;
        private static readonly int DissolveId = Shader.PropertyToID("_DissolveForce");
        private static readonly int AutoDissolveId = Shader.PropertyToID("_AutoDissolve");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private static readonly int SelectionId =
            Shader.PropertyToID("_Selection");

        private static readonly int VisibilityId =
            Shader.PropertyToID("_Visibility");

        private MaterialPropertyBlock _block;
        private int[] _materialCounts;
        private Material[][] _selectionMaterials;
        private Color[][] _surfaceColors;
        private float _selection;
        private float _visibility = 1f;

        public WeaponDefinition Definition => _definition;

        public Transform FocusPoint =>
            _focusPoint != null ? _focusPoint : transform;

        private void Awake()
        {
            _block = new MaterialPropertyBlock();

            if (_renderers == null || _renderers.Length == 0)
                _renderers = GetComponentsInChildren<Renderer>(true);

            _materialCounts = new int[_renderers.Length];
            _selectionMaterials = new Material[_renderers.Length][];
            _surfaceColors = new Color[_renderers.Length][];
            int surfaceIndex = 0;

            for (int i = 0; i < _renderers.Length; i++)
            {
                Renderer target = _renderers[i];

                if (target == null)
                    continue;

                Material[] materials = target.sharedMaterials;
                _selectionMaterials[i] = materials;
                _materialCounts[i] = materials.Length;
                _surfaceColors[i] = new Color[materials.Length];
                for (int m = 0; m < materials.Length; m++, surfaceIndex++)
                {
                    // El material Dissolve de cada slot conserva el color original del arma.
                    Material surface = _dissolveMaterials != null &&
                        surfaceIndex < _dissolveMaterials.Length &&
                        _dissolveMaterials[surfaceIndex] != null
                        ? _dissolveMaterials[surfaceIndex] : materials[m];
                    _surfaceColors[i][m] = surface != null && surface.HasProperty(BaseColorId)
                        ? surface.GetColor(BaseColorId) : Color.white;
                }

                foreach (Material material in materials)
                {
                    if (material == null ||
                        !material.HasProperty(SelectionId) ||
                        !material.HasProperty(VisibilityId))
                    {
                        Debug.LogWarning(
                            "El material de exhibición debe exponer " +
                            "_Selection y _Visibility.",
                            target);
                    }
                }
            }

            Apply();
        }

        public void SetSelected(bool selected)
        {
            _selection = selected ? 1f : 0f;
            Apply();
        }

        public void SetVisibility(float visibility)
        {
            _visibility = Mathf.Clamp01(visibility);
            Apply();
        }

        public void BeginDisappear(bool selected)
        {
            RestoreSelectionMaterials();
            SetSelected(false);
            if (selected || _dissolveMaterials == null) return;
            UseDissolveMaterials();
        }

        public void BeginAppear()
        {
            RestoreSelectionMaterials();
            _selection = 0f;
            _visibility = 0f;
            UseDissolveMaterials();
            Apply();
        }

        public void FinishAppear()
        {
            _visibility = 1f;
            RestoreSelectionMaterials();
            Apply();
        }

        private void RestoreSelectionMaterials()
        {
            for (int r = 0; r < _renderers.Length; r++)
                if (_renderers[r] != null)
                    _renderers[r].sharedMaterials = _selectionMaterials[r];
            _dissolving = false;
        }

        private void UseDissolveMaterials()
        {
            if (_dissolveMaterials == null) return;
            int count = 0;
            foreach (int slots in _materialCounts) count += slots;
            if (_dissolveMaterials.Length != count) return;
            foreach (Material material in _dissolveMaterials)
                if (material == null || !material.HasProperty(DissolveId)) return;

            int index = 0;
            for (int r = 0; r < _renderers.Length; r++)
            {
                if (_renderers[r] == null) continue;
                var materials = new Material[_materialCounts[r]];
                for (int m = 0; m < materials.Length; m++)
                    materials[m] = _dissolveMaterials[index++];
                _renderers[r].sharedMaterials = materials;
            }
            _dissolving = true;
            Apply();
        }

        private void Apply()
        {
            for (int r = 0; r < _renderers.Length; r++)
            {
                Renderer target = _renderers[r];

                if (target == null)
                    continue;

                for (int m = 0; m < _materialCounts[r]; m++)
                {
                    _block.Clear();
                    target.GetPropertyBlock(_block, m);

                    _block.SetFloat(SelectionId, _selection);
                    _block.SetFloat(VisibilityId, _visibility);
                    _block.SetColor(BaseColorId, _surfaceColors[r][m]);
                    if (_dissolving)
                    {
                        _block.SetFloat(AutoDissolveId, 0f);
                        _block.SetFloat(DissolveId, 1f - _visibility);
                    }

                    target.SetPropertyBlock(_block, m);
                }
            }
        }
    }
}
