using FinalVFX.Player;
using UnityEngine;

namespace FinalVFX.Combat
{
    [DefaultExecutionOrder(200)]
    [RequireComponent(typeof(BladeRibbonTrail))]
    public sealed class WeaponUltimateVisual : MonoBehaviour
    {
        [System.Serializable]
        private sealed class MaterialSlot
        {
            public Renderer renderer;
            [Min(0)] public int materialIndex;
        }

        [SerializeField] private MaterialSlot[] _bladeMaterials;
        private PlayerUltimateEffects _owner;
        private HealthComponent _health;
        private BladeRibbonTrail _trail;
        private MaterialPropertyBlock _block;
        private bool _wasActive;
        private static readonly int UltimateWeight = Shader.PropertyToID("_UltimateWeight");
        private static readonly int EffectTime = Shader.PropertyToID("_EffectTime");

        private void Awake()
        {
            _trail = GetComponent<BladeRibbonTrail>();
            _block = new MaterialPropertyBlock();
            WriteGlow(0f);
        }

        private void Start()
        {
            _owner = GetComponentInParent<PlayerUltimateEffects>();
            _health = GetComponentInParent<HealthComponent>();
            if (_health) { _health.Died += ResetVisual; _health.Restored += ResetVisual; }

            if (_bladeMaterials == null)
                return;
            foreach (MaterialSlot slot in _bladeMaterials)
            {
                if (!ValidSlot(slot))
                    continue;
                Material material = slot.renderer.sharedMaterials[slot.materialIndex];
            }
        }

        private bool ValidSlot(MaterialSlot slot) =>
            slot != null && slot.renderer && slot.materialIndex >= 0 &&
            slot.materialIndex < slot.renderer.sharedMaterials.Length;

        private bool Hidden()
        {
            if (_bladeMaterials == null)
                return false;
            foreach (MaterialSlot slot in _bladeMaterials)
            {
                if (ValidSlot(slot) && slot.renderer.forceRenderingOff)
                    return true;
            }
            return false;
        }

        private void LateUpdate()
        {
            bool hidden = Hidden();
            bool active = _owner && _owner.Active && !hidden && (!_health || !_health.IsDead);
            if (active != _wasActive)
            {
                _trail.SetEmitting(active);
                _wasActive = active;
            }
            WriteGlow(active ? 1f : 0f);
            if ((_health && _health.IsDead) || hidden || !_owner || !_owner.isActiveAndEnabled || !_owner.gameObject.activeInHierarchy)
                _trail.Clear();
            _trail.Tick(Time.time);
        }

        private void WriteGlow(float weight)
        {
            if (_bladeMaterials == null)
                return;
            foreach (MaterialSlot slot in _bladeMaterials)
            {
                if (!ValidSlot(slot))
                    continue;
                _block.Clear();
                slot.renderer.GetPropertyBlock(_block, slot.materialIndex);
                _block.SetFloat(UltimateWeight, weight);
                _block.SetFloat(EffectTime, Time.time);
                slot.renderer.SetPropertyBlock(_block, slot.materialIndex);
            }
        }

        private void OnDestroy()
        {
            if (_health) { _health.Died -= ResetVisual; _health.Restored -= ResetVisual; }
        }

        private void OnDisable() => ResetVisual();

        private void ResetVisual()
        {
            _wasActive = false;
            if (_block != null)
                WriteGlow(0f);
            if (_trail)
            {
                _trail.SetEmitting(false);
                _trail.Clear();
            }
        }
    }
}
