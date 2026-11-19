using UnityEngine;
namespace FinalVFX.Core
{
    [RequireComponent(typeof(MeshRenderer))]
    public sealed class ArenaIntroFog : MonoBehaviour
    {
        private MeshRenderer _renderer;
        private MaterialPropertyBlock _block;
        private static readonly int Opacity = Shader.PropertyToID("_Opacity");
        private void Awake() => SetOpacity(1);
        public void SetOpacity(float value)
        {
            if (!_renderer) _renderer = GetComponent<MeshRenderer>();
            if (_block == null) _block = new MaterialPropertyBlock();
            value = Mathf.Clamp01(value);
            _renderer.GetPropertyBlock(_block);
            _block.SetFloat(Opacity, value);
            _renderer.SetPropertyBlock(_block);
            _renderer.enabled = value > 0;
        }
    }
}
