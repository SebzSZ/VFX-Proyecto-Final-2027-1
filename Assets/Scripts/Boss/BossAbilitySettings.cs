using System;
using UnityEngine;

namespace FinalVFX.Boss
{
    [Serializable]
    public sealed class BossAbility
    {
        public BossAttackId Id;
        public AnimationClip Clip;
        [Min(0.01f)] public float Preparation = 0.8f;
        [Min(0.01f)] public float Execution = 0.6f;
        [Min(0.01f)] public float Recovery = 0.7f;
        [Min(0f)] public float Damage = 25f;
        [Min(0f)] public float HitInvulnerability = 0.3f;
        [Min(0.1f)] public float Radius = 3f;
        [Min(0.1f)] public float Height = 3f;
        [Min(0f)] public float TravelDistance = 7f;
        [Min(0f)] public float PushDistance = 4f;
        [Min(0.01f)] public float ForceSeconds = 0.35f;
        [Min(0f)] public float StunSeconds = 2f;
        [Min(0.1f)] public float RayDistance = 16f;
        [Min(0.1f)] public float PullStopDistance = 2f;
        [Range(0.05f, 1f)] public float DashFraction = 0.7f;
        [Min(0.1f)] public float RiseHeight = 3f;
        [Min(0.1f)] public float FogRadius = 4f;
        [Min(0.1f)] public float FogHeight = 6f;
        [Tooltip("Segundos que tarda la niebla en subir de opacidad al comenzar Execution.")]
        [Min(0f)] public float FogFadeIn = 0.35f;
        [Tooltip("Segundos que tarda la niebla en desaparecer al terminar o interrumpirse el ataque. El bloqueo se libera inmediatamente.")]
        [Min(0f)] public float FogFadeOut = 0.6f;
        [Tooltip("Radio máximo desde el jefe para los pilares cercanos, en metros.")]
        [Min(0.1f)] public float PillarNearRadius = 12f;
        [Tooltip("Fracción de pilares de cada oleada que se coloca cerca del jefe. El resto utiliza toda la arena.")]
        [Range(0f, 1f)] public float NearPillarFraction = 0.75f;
        [Min(0.05f)] public float WaveInterval = 1f;
        [Min(1)] public int PillarsPerWave = 5;
        [Min(0.01f)] public float PillarWarning = 0.7f;
        [Min(0.01f)] public float PillarLifetime = 1.2f;
        [Min(0.1f)] public float PillarRadius = 0.8f;
        [Min(0.1f)] public float PillarHeight = 5f;
        [Min(0.05f)] public float PillarHitInterval = 0.5f;
        public float Total => Preparation + Execution + Recovery;
    }

    [CreateAssetMenu(menuName = "Final VFX/Boss/Ability Settings", fileName = "SO_BossAbilities")]
    public sealed class BossAbilitySettings : ScriptableObject
    {
        public AnimationClip Idle;
        public AnimationClip Walk;
        public AnimationClip Stunned;
        [Min(0.1f)] public float WalkBlendSpeed = 2.5f;
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

