using UnityEngine;

namespace FinalVFX.Combat
{
    [CreateAssetMenu(
        fileName = "SO_Weapon",
        menuName = "Final VFX/Weapon Definition")]
    public sealed class WeaponDefinition : ScriptableObject
    {
        [SerializeField] private string _displayName;
        [SerializeField] private GameObject _equippedPrefab;

        [Header("Player stats")]
        [SerializeField, Min(1f)] private float _maximumHealth = 100f;
        [SerializeField, Min(1f)] private float _maximumStamina = 100f;

        [Header("Transform relative to WeaponSocket")]
        [SerializeField] private Vector3 _localPosition;
        [SerializeField] private Vector3 _localEulerAngles;
        [SerializeField] private Vector3 _localScale = Vector3.one;

        public string DisplayName => _displayName;
        public GameObject EquippedPrefab => _equippedPrefab;
        public float MaximumHealth => _maximumHealth;
        public float MaximumStamina => _maximumStamina;
        public Vector3 LocalPosition => _localPosition;
        public Quaternion LocalRotation => Quaternion.Euler(_localEulerAngles);
        public Vector3 LocalScale => _localScale;
    }
}