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
            if (!isActiveAndEnabled || (_targetLayers.value & (1 << other.gameObject.layer)) == 0)
                return;

            HealthComponent health = other.GetComponentInParent<HealthComponent>();
            if (health == null)
                return;

            health.ReceiveDamage(_damage, _invulnerabilityDuration);
        }
    }
}
