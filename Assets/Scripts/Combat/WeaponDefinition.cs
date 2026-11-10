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

        [Header("Animation")]
        [SerializeField] private WeaponPoseType _poseType;


        [Header("Combate")]
        [SerializeField] private BasicStep[] _basicSteps = System.Array.Empty<BasicStep>();
        [SerializeField, Min(0f)] private float _basicRestartDelay = 0.15f;
        [SerializeField] private SingleAction _secondary = new SingleAction();
        [SerializeField] private SingleAction _ultimate = new SingleAction();

        [SerializeField] private Impact _secondaryImpact = new Impact();
        public Impact SecondaryImpact => _secondaryImpact;

        [System.Serializable]
        public sealed class Impact
        {
            [Min(0f)] public float damage = 10f;
            [Range(0f, 1f)] public float from = 0.3f;
            [Range(0f, 1f)] public float until = 0.5f;
        }

        public int StepCount => _basicSteps.Length;
        public BasicStep Step(int index) => _basicSteps[index];
        public float BasicRestartDelay => _basicRestartDelay;
        public SingleAction Secondary => _secondary;
        public SingleAction Ultimate => _ultimate;

        [System.Serializable]
        public sealed class BasicStep
        {
            public Impact impact = new Impact();
            public AnimationClip clip;
            [Tooltip("Segundos si no hay clip.")]
            [Min(0.01f)] public float fallbackDuration = 0.6f;
            [Tooltip("Fracción del golpe en que se abre la ventana.")]
            [Range(0f, 1f)] public float open = 0.25f;
            [Tooltip("Fracción del golpe en que cierra la ventana.")]
            [Range(0f, 1f)] public float close = 0.7f;
            [Tooltip("Fracción del golpe en que se reproduce el siguiente.")]
            [Range(0f, 1f)] public float link = 0.8f;

            public float Duration => Mathf.Max(0.01f, clip ? clip.length : fallbackDuration);
        }

        [System.Serializable]
        public sealed class SingleAction
        {
            public AnimationClip clip;
            [Min(0f)] public float lockSeconds = 0.6f;
        }

        public string DisplayName => _displayName;
        public GameObject EquippedPrefab => _equippedPrefab;
        public float MaximumHealth => _maximumHealth;
        public float MaximumStamina => _maximumStamina;
        public Vector3 LocalPosition => _localPosition;
        public Quaternion LocalRotation => Quaternion.Euler(_localEulerAngles);
        public Vector3 LocalScale => _localScale;
        public WeaponPoseType PoseType => _poseType;

        public enum WeaponPoseType
        {
            None = 0,
            Sword = 1,
            Scythe = 2
        }
    }
}
