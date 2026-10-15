using FinalVFX.Combat;
using UnityEngine;

namespace FinalVFX.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(HealthComponent))]
    [RequireComponent(typeof(StaminaComponent))]
    public sealed class PlayerEquipment : MonoBehaviour
    {
        [SerializeField] private Transform _weaponSocket;

        private HealthComponent _health;
        private StaminaComponent _stamina;
        private GameObject _weaponInstance;
        private Renderer[] _weaponRenderers = new Renderer[0];

        public WeaponDefinition EquippedWeapon { get; private set; }
        public bool HasSocket => _weaponSocket != null;

        private void Awake()
        {
            _health = GetComponent<HealthComponent>();
            _stamina = GetComponent<StaminaComponent>();
        }

        public bool TryEquip(WeaponDefinition definition)
        {
            if (definition == null ||
                definition.EquippedPrefab == null ||
                _weaponSocket == null)
            {
                Debug.LogError(
                    "Falta el perfil, prefab o WeaponSocket del arma.",
                    this);
                return false;
            }

            if (_weaponInstance != null)
            {
                _weaponInstance.SetActive(false);
                Destroy(_weaponInstance);
            }

            _weaponInstance = Instantiate(
                definition.EquippedPrefab, _weaponSocket, false);

            Transform weapon = _weaponInstance.transform;
            weapon.localPosition = definition.LocalPosition;
            weapon.localRotation = definition.LocalRotation;
            weapon.localScale = definition.LocalScale;

            _weaponRenderers =
                _weaponInstance.GetComponentsInChildren<Renderer>(true);

            EquippedWeapon = definition;

            _health.SetMaximumAndRestore(definition.MaximumHealth);
            _stamina.SetMaximumAndRestore(definition.MaximumStamina);

            return true;
        }

        public void SetVisible(bool visible)
        {
            foreach (Renderer target in _weaponRenderers)
            {
                if (target != null)
                    target.forceRenderingOff = !visible;
            }
        }
    }
}