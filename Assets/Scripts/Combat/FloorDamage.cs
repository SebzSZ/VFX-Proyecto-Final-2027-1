using System.Collections.Generic;
using UnityEngine;

namespace FinalVFX.Combat
{
    [RequireComponent(typeof(Collider), typeof(Rigidbody))]
    public class FloorDamage : MonoBehaviour
    {
        [SerializeField] private LayerMask _targetLayers;
        [SerializeField, Min(0f)] private float _damage = 25f;
        [SerializeField, Min(0.01f)] private float _interval = 1f;

        private readonly Dictionary<HealthComponent, float> _nextDamageAt = new Dictionary<HealthComponent, float>();
        private readonly List<HealthComponent> _expiredTargets = new List<HealthComponent>();

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
            Rigidbody body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
        }

        private void OnTriggerEnter(Collider other) => TryDamage(other);
        private void OnTriggerStay(Collider other) => TryDamage(other);

        private void FixedUpdate()
        {
            // Conservar la cadencia al salir y volver a entrar, pero no retener
            // objetivos una vez que su temporizador haya vencido.
            float now = Time.time;
            foreach (var entry in _nextDamageAt)
            {
                if (entry.Key == null || entry.Value <= now)
                    _expiredTargets.Add(entry.Key);
            }

            foreach (HealthComponent target in _expiredTargets)
                _nextDamageAt.Remove(target);

            _expiredTargets.Clear();
        }

        private void TryDamage(Collider other)
        {
            // Unity también puede enviar eventos de trigger a scripts deshabilitados.
            if (!isActiveAndEnabled || (_targetLayers.value & (1 << other.gameObject.layer)) == 0)
                return;

            HealthComponent health = other.GetComponentInParent<HealthComponent>();
            if (health == null)
                return;

            if (_nextDamageAt.TryGetValue(health, out float nextHit) && Time.time < nextHit)
                return;

            _nextDamageAt[health] = Time.time + _interval;
            health.ReceiveDamage(_damage);
        }

        private void OnDisable()
        {
            _nextDamageAt.Clear();
            _expiredTargets.Clear();
        }
    }
}
