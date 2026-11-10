using UnityEngine;
using FinalVFX.Player;

namespace FinalVFX.Combat
{
    [RequireComponent(typeof(Collider))]
    public class DirectDamage : MonoBehaviour
    {
        [SerializeField] private LayerMask _targetLayers;
        [SerializeField, Min(0f)] private float _damage = 25f;
        [SerializeField, Min(0f)] private float _invulnerabilityDuration = 0.5f;

        // El valor por defecto conserva el daño por contacto de los objetos de prueba.
        [SerializeField] private bool _manualMelee;
        [Tooltip("Para el melee, asigna la raíz del Guardián.")]
        [SerializeField] private Transform _pushOrigin;
        public bool ManualMelee => _manualMelee;
        public LayerMask TargetLayers => _targetLayers;

        public bool TryMeleeHit(Collider other)
        {
            return _manualMelee && ApplyDamage(other);
        }

        private void OnTriggerEnter(Collider other) => TryInflictDamage(other);
        private void OnTriggerStay(Collider other) => TryInflictDamage(other);

        private void TryInflictDamage(Collider other)
        {
            if (!_manualMelee)
                ApplyDamage(other);
        }

        private bool ApplyDamage(Collider other)
        {
            if (!isActiveAndEnabled || (_targetLayers.value & (1 << other.gameObject.layer)) == 0)
                return false;

            HealthComponent health = other.GetComponentInParent<HealthComponent>();
            if (health == null)
                return false;

            if (health.ReceiveDamage(_damage, _invulnerabilityDuration) <= 0f)
                return false;

            if (!health.IsDead && health.TryGetComponent(out PlayerController player))
            {
                Vector3 origin = _pushOrigin ? _pushOrigin.position : transform.position;
                player.ReactToDirectDamage(origin);
            }
            return true;
        }
    }
}
