using UnityEngine;

namespace FinalVFX.Combat
{
    [RequireComponent(typeof(Collider))]
    public class DirectDamage : MonoBehaviour
    {
        [SerializeField] private LayerMask _targetLayers;
        [SerializeField, Min(0f)] private float _damage = 25f;
        [SerializeField, Min(0f)] private float _invulnerabilityDuration = 0.5f;

        private void OnTriggerEnter(Collider other) => TryInflictDamage(other);
        private void OnTriggerStay(Collider other) => TryInflictDamage(other);

        private void TryInflictDamage(Collider other)
        {
            // Filtrar por capas
            if ((_targetLayers.value & (1 << other.gameObject.layer)) == 0)
                return;

            HealthComponent health = other.GetComponentInParent<HealthComponent>();
            if (health == null || health.IsDead)
                return;

            InvulnerabilityComponent invulnerability = health.GetComponent<InvulnerabilityComponent>();

            // Si está en ventana de invulnerabilidad, no hacer daño
            if (invulnerability != null && invulnerability.IsInvulnerable)
                return;

            // Aplicar daño
            health.ReceiveDamage(_damage);

            // Activar invulnerabilidad
            if (invulnerability != null)
            {
                float now = Time.time;
                invulnerability.SetWindow(this, now, now + _invulnerabilityDuration);
            }
        }

        private void OnDisable()
        {
            // Limpieza si es necesaria
        }
    }
}