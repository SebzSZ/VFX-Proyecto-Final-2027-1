using System;
using UnityEngine;

namespace FinalVFX.Boss
{
    public enum BossAttackId
    {
        BarridoCelestial = 0,
        AnteMi = 1,
        EnGarde = 2,
        LanzaPrimordial = 3,
        AtaqueBasico = 4,
        Blasfemia = 5
    }

    [Serializable]
    public class BossAttackRule
    {
        public BossAttackId Id;
        [Min(0f)] public float NearWeight;
        [Min(0f)] public float FarWeight;
        [Min(0f)] public float Cooldown;
        [Min(0f)] public float MeleeWeight;
    }

    [CreateAssetMenu(fileName = "BossAISettings", menuName = "Final VFX/Boss/AI Settings")]
    public class BossAISettings : ScriptableObject
    {
        [Header("Detección y combate cercano")]
        [Min(0.1f)] public float VisionDistance;
        [Min(0.1f)] public float MeleeDistance;
        [Min(0.1f)] public float ChaseStoppingDistance;

        [Header("Distancia al jugador")]
        [Min(0f)] public float NearDistance;
        [Min(0.1f)] public float FarDistance;

        [Header("Decisiones")]
        [Min(0.1f)] public float DecisionInterval;

        [Tooltip("Multiplica el peso del último ataque seleccionado.")]
        [Range(0f, 1f)] public float RepeatMultiplier;

        public BossAttackRule[] Attacks =
        {
            new BossAttackRule
            {
                Id = BossAttackId.BarridoCelestial,
                NearWeight = 2f, FarWeight = 5f, Cooldown = 6f
            },
            new BossAttackRule
            {
                Id = BossAttackId.AnteMi,
                NearWeight = 1f, FarWeight = 6f, Cooldown = 7f
            },
            new BossAttackRule
            {
                Id = BossAttackId.EnGarde,
                NearWeight = 6f, FarWeight = 1f, Cooldown = 8f
            },
            new BossAttackRule
            {
                Id = BossAttackId.LanzaPrimordial,
                NearWeight = 2f, FarWeight = 2f, Cooldown = 10f
            }
        };

        private void OnValidate()
        {
            MeleeDistance = Mathf.Max(0.2f, MeleeDistance);

            ChaseStoppingDistance = Mathf.Clamp(
                ChaseStoppingDistance, 0.1f, MeleeDistance - 0.1f);

            NearDistance = Mathf.Max(MeleeDistance, NearDistance);
            FarDistance = Mathf.Max(NearDistance + 0.1f, FarDistance);
            VisionDistance = Mathf.Max(FarDistance, VisionDistance);

            DecisionInterval = Mathf.Max(0.1f, DecisionInterval);
        }
    }
}
