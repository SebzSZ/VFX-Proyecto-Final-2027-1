using System;
using UnityEngine;

namespace FinalVFX.Combat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(InvulnerabilityComponent))]
    public class HealthComponent : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float _maxHealth = 100f;
        [SerializeField, Min(0f)] private float _damageInvulnerabilityDuration = 0f;
        private InvulnerabilityComponent _invulnerability;

        public float Current { get; private set; }
        public float Maximum => _maxHealth;
        public float Normalized => Current / Maximum;
        public bool IsDead => Current <= 0f;
        public bool CanHeal => !IsDead && Current < Maximum;
        public float Shield { get; private set; }
        public float MaximumShield => Maximum * 0.5f;

        public event Action<float, float> Changed;
        public event Action<float, float> ShieldChanged;
        public event Action<float> Damaged;
        public event Action<float> Healed;
        public event Action Died;
        public event Action Restored;

        public readonly struct DamageResult
        {
            public readonly float HealthLost;
            public readonly float ShieldLost;
            public float Total => HealthLost + ShieldLost;

            public DamageResult(float healthLost, float shieldLost)
            {
                HealthLost = healthLost;
                ShieldLost = shieldLost;
            }
        }

        protected virtual void Awake()
        {
            _invulnerability = GetComponent<InvulnerabilityComponent>();
            Current = Maximum;
        }

        // Conserva el contrato de devolver únicamente la vida descontada.
        public float ReceiveDamage(float amount) => ReceiveDamage(amount, 0f);
        public float ReceiveDamage(float amount, float minimumInvulnerabilityDuration) =>
            ReceiveHit(amount, minimumInvulnerabilityDuration).HealthLost;

        public DamageResult ReceiveHit(float amount, float minimumInvulnerabilityDuration = 0f)
        {
            if (amount <= 0f || IsDead || _invulnerability.IsInvulnerable)
                return default;

            float absorbed = Mathf.Min(amount, Shield);
            float next = Mathf.Clamp(Current - (amount - absorbed), DamageFloor, Current);
            float applied = Current - next;
            if (absorbed + applied <= 0f)
                return default;

            Shield -= absorbed;
            Current = next;
            if (applied > 0f)
                OnDamageCommitted();

            bool died = IsDead;
            float protection = Mathf.Max(_damageInvulnerabilityDuration, minimumInvulnerabilityDuration);
            if (!died && protection > 0f)
                _invulnerability.SetWindow(this, Time.time, Time.time + protection);

            if (absorbed > 0f)
                ShieldChanged?.Invoke(Shield, Maximum);
            if (applied > 0f)
            {
                Changed?.Invoke(Current, Maximum);
                Damaged?.Invoke(applied);
            }
            if (died)
                Died?.Invoke();
            return new DamageResult(applied, absorbed);
        }

        protected virtual float DamageFloor => 0f;
        protected virtual void OnDamageCommitted() { }

        public float AddShield(float amount)
        {
            if (IsDead || amount <= 0f)
                return 0f;
            float added = Mathf.Min(amount, Mathf.Max(0f, MaximumShield - Shield));
            if (added <= 0f)
                return 0f;
            Shield += added;
            ShieldChanged?.Invoke(Shield, Maximum);
            return added;
        }

        public void ClearShield()
        {
            if (Shield <= 0f)
                return;
            Shield = 0f;
            ShieldChanged?.Invoke(Shield, Maximum);
        }

        public float Heal(float amount)
        {
            if (amount <= 0f || !CanHeal)
                return 0f;
            float applied = Mathf.Min(amount, Maximum - Current);
            Current += applied;
            Changed?.Invoke(Current, Maximum);
            Healed?.Invoke(applied);
            return applied;
        }

        public void RestoreFull()
        {
            _invulnerability.Clear();
            Current = Maximum;
            Shield = 0f;
            Restored?.Invoke();
            Changed?.Invoke(Current, Maximum);
            ShieldChanged?.Invoke(Shield, Maximum);
        }

        public void SetMaximumAndRestore(float maximum)
        {
            _maxHealth = Mathf.Max(0.01f, maximum);
            RestoreFull();
        }
    }
}
