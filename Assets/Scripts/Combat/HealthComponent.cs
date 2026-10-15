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

        public event Action<float, float> Changed;
        public event Action<float> Damaged;

        public event Action<float> Healed;
        public event Action Died;

        private void Awake()
        {
            _invulnerability = GetComponent<InvulnerabilityComponent>();
            Current = Maximum;
        }

        public float ReceiveDamage(float amount)
        {
            return ReceiveDamage(amount, 0f);
        }

        public float ReceiveDamage(float amount, float minimumInvulnerabilityDuration)
        {
            if (amount <= 0f || IsDead || _invulnerability.IsInvulnerable)
                return 0f;

            float applied = Mathf.Min(amount, Current);
            Current -= applied;
            bool died = IsDead;

            float protectionDuration = Mathf.Max(_damageInvulnerabilityDuration, minimumInvulnerabilityDuration);
            if (!died && protectionDuration > 0f)
                _invulnerability.SetWindow(this, Time.time, Time.time + protectionDuration);

            Changed?.Invoke(Current, Maximum);
            Damaged?.Invoke(applied);

            if (died)
                Died?.Invoke();

            return applied;
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
            if (Current == Maximum)
                return;

            Current = Maximum;
            Changed?.Invoke(Current, Maximum);
        }

        public void SetMaximumAndRestore(float maximum)
        {
            _maxHealth = maximum;
            Current = Maximum;
            Changed?.Invoke(Current, Maximum);
        }
    }
}
