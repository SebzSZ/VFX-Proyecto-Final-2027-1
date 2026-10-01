using System.Collections.Generic;
using UnityEngine;

namespace FinalVFX.Combat
{
    [RequireComponent(typeof(Collider))]
    public class FloorDamage : MonoBehaviour
    {
        [SerializeField] private LayerMask _targetLayers;
        [SerializeField, Min(0f)] private float _damage = 25f;
        [SerializeField, Min(0.01f)] private float _interval = 1f;

        private readonly Dictionary<HealthComponent, float> _nextDamageAt = new Dictionary<HealthComponent, float>();
        private Collider _triggerCollider;

        private void Start()
        {
            _triggerCollider = GetComponent<Collider>();
        }

        private void FixedUpdate()
        {
            Collider[] colliders = Physics.OverlapBox(
                _triggerCollider.bounds.center,
                _triggerCollider.bounds.extents,
                Quaternion.identity,
                _targetLayers
            );

            foreach (Collider other in colliders)
            {
                TryDamage(other);
            }
        }

        private void TryDamage(Collider other)
        {
            HealthComponent health = other.GetComponentInParent<HealthComponent>();
            if (health == null || health.IsDead)
                return;

            if (_nextDamageAt.TryGetValue(health, out float nextHit) && Time.time < nextHit)
                return;

            _nextDamageAt[health] = Time.time + _interval;
            health.ReceiveDamage(_damage);
        }

        private void OnDisable()
        {
            _nextDamageAt.Clear();
        }
    }
}
