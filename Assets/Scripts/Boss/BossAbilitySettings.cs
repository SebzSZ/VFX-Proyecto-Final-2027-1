using System;
using UnityEngine;

namespace FinalVFX.Boss
{
    [Serializable]
    public sealed class BossAbility
    {
        public BossAttackId Id;
        public AnimationClip Clip;
        [Min(0.01f)] public float Preparation;
        [Min(0.01f)] public float Execution;
        [Min(0.01f)] public float Recovery;
        [Min(0f)] public float Damage;
        [Min(0f)] public float HitInvulnerability;
        [Min(0.1f)] public float Radius;
        [Min(0.1f)] public float Height;
        [Min(0f)] public float TravelDistance;
        [Min(0f)] public float PushDistance;
        [Min(0.01f)] public float ForceSeconds;
        [Min(0f)] public float StunSeconds;
        [Min(0.1f)] public float RayDistance;
        [Min(0.1f)] public float PullStopDistance;
        [Range(0.05f, 1f)] public float DashFraction;
        [Min(0.1f)] public float RiseHeight;
        [Min(0.1f)] public float FogRadius;
        [Min(0.1f)] public float FogHeight;
        [Tooltip("Segundos que tarda la niebla en subir de opacidad al comenzar Execution.")]
        [Min(0f)] public float FogFadeIn;
        [Tooltip("Segundos que tarda la niebla en desaparecer al terminar o interrumpirse el ataque. El bloqueo se libera inmediatamente.")]
        [Min(0f)] public float FogFadeOut;
        [Tooltip("Radio máximo desde el jefe para los pilares cercanos, en metros.")]
        [Min(0.1f)] public float PillarNearRadius;
        [Tooltip("Fracción de pilares de cada oleada que se coloca cerca del jefe. El resto utiliza toda la arena.")]
        [Range(0f, 1f)] public float NearPillarFraction;
        [Min(0.05f)] public float WaveInterval;
        [Min(1)] public int PillarsPerWave;
        [Min(0.01f)] public float PillarWarning;
        [Min(0.01f)] public float PillarLifetime;
        [Min(0.1f)] public float PillarRadius;
        [Min(0.1f)] public float PillarHeight;
        [Min(0.05f)] public float PillarHitInterval;
        public float Total => Preparation + Execution + Recovery;
    }

    [CreateAssetMenu(menuName = "Final VFX/Boss/Ability Settings", fileName = "SO_BossAbilities")]
    public sealed class BossAbilitySettings : ScriptableObject
    {
        public AnimationClip Idle;
        public AnimationClip Walk;
        public AnimationClip Stunned;
        [Min(0.1f)] public float WalkBlendSpeed;
        public BossAbility[] Abilities =
        {
            new BossAbility { Id = BossAttackId.BarridoCelestial },
            new BossAbility { Id = BossAttackId.AnteMi, Execution = 0.45f },
            new BossAbility { Id = BossAttackId.EnGarde, Execution = 0.4f },
            new BossAbility { Id = BossAttackId.LanzaPrimordial, Preparation = 1.2f, Execution = 9f, Recovery = 1f },
            new BossAbility { Id = BossAttackId.AtaqueBasico, Preparation = 0.55f, Execution = 0.18f, Recovery = 0.55f },
            new BossAbility { Id = BossAttackId.Blasfemia, Preparation = 1.2f, Execution = 0.7f, Recovery = 1f }
        };

        public BossAbility Find(BossAttackId id)
        {
            if (Abilities != null)
                foreach (var ability in Abilities)
                    if (ability != null && ability.Id == id) return ability;
            return null;
        }
    }
}

