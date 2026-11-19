using System;
using UnityEngine;

namespace FinalVFX.Combat
{
    [RequireComponent(typeof(HealthComponent))]
    public class HealingConsumable : MonoBehaviour
    {
        [SerializeField] private int _maximumUses;
        [SerializeField] private float _healAmount;
        [SerializeField] private float _cooldown;

        private HealthComponent _health;
        private float _readyAt;

        public int RemainingUses { get; private set; }
        public int MaximumUses => _maximumUses;
        public float CooldownRemaining => Mathf.Max(0f, _readyAt - Time.time);
        public bool CanUse => RemainingUses > 0 && CooldownRemaining <= 0f && _health.CanHeal;

        public event Action<int, int> UsesChanged;
        public event Action<float> Used;

        private void Awake()
        {
            _health = GetComponent<HealthComponent>();
            RemainingUses = MaximumUses;
        }

        public bool TryUse()
        {
            if (!CanUse)
                return false;

            RemainingUses--;
            _readyAt = Time.time + _cooldown;
            float healed = _health.Heal(_healAmount);
            UsesChanged?.Invoke(RemainingUses, MaximumUses);
            Used?.Invoke(healed);
            return true;
        }

        public void RestoreFull()
        {
            _readyAt = 0f;
            if (RemainingUses == MaximumUses)
                return;

            RemainingUses = MaximumUses;
            UsesChanged?.Invoke(RemainingUses, MaximumUses);
        }
    }
}
