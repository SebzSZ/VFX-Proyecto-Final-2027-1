using System.Collections.Generic;
using FinalVFX.Boss;
using FinalVFX.Combat;
using UnityEngine;

namespace FinalVFX.Player
{
    [RequireComponent(typeof(PlayerCombat), typeof(PlayerEquipment), typeof(HealthComponent))]
    [RequireComponent(typeof(PlayerHitVolumes))]
    public sealed class PlayerUltimateEffects : MonoBehaviour
    {
        [SerializeField] private ParticleSystem _scytheAura;
        [SerializeField] private LayerMask _auraTargets = 1 << 9;
        private PlayerCombat _combat;
        private PlayerEquipment _equipment;
        private PlayerHitVolumes _hits;
        private HealthComponent _health;
        private WeaponDefinition _activeWeapon;
        private float _endsAt;
        private float _nextPulse;
        private readonly HashSet<HealthComponent> _pulseTargets = new HashSet<HealthComponent>();

        public bool Active => isActiveAndEnabled && _activeWeapon && !_health.IsDead && Time.time < _endsAt;
        public float BasicDamageMultiplier => Active &&
            _activeWeapon.PoseType == WeaponDefinition.WeaponPoseType.Sword
            ? Mathf.Max(1f, _activeWeapon.UltimateEffect.swordBasicMultiplier) : 1f;

        private void Awake()
        {
            _combat = GetComponent<PlayerCombat>();
            _equipment = GetComponent<PlayerEquipment>();
            _hits = GetComponent<PlayerHitVolumes>();
            _health = GetComponent<HealthComponent>();
            if (_scytheAura)
            {
                var main = _scytheAura.main;
                main.playOnAwake = false;
                main.useUnscaledTime = false;
                _scytheAura.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private void OnEnable()
        {
            _combat.UltimateExecuted += Activate;
            _hits.BasicDamaged += OnBasicDamaged;
            _equipment.WeaponChanged += OnWeaponChanged;
            _health.Died += ResetEffects;
            _health.Restored += ResetEffects;
        }

        private void OnDisable()
        {
            _combat.UltimateExecuted -= Activate;
            _hits.BasicDamaged -= OnBasicDamaged;
            _equipment.WeaponChanged -= OnWeaponChanged;
            _health.Died -= ResetEffects;
            _health.Restored -= ResetEffects;
            ResetEffects();
        }

        private void OnWeaponChanged(WeaponDefinition weapon) => ResetEffects();

        private void Activate(WeaponDefinition weapon)
        {
            if (!isActiveAndEnabled || _health.IsDead || !weapon || weapon != _equipment.EquippedWeapon)
                return;

            FinishEffect(true);
            _activeWeapon = weapon;
            _endsAt = Time.time + Mathf.Max(0.1f, weapon.UltimateEffect.duration);
            _nextPulse = Time.time + Mathf.Max(0.05f, weapon.UltimateEffect.auraInterval);
            if (weapon.PoseType == WeaponDefinition.WeaponPoseType.Scythe)
            {
                if (_scytheAura)
                    _scytheAura.Play(true);
            }
        }

        private void Update()
        {
            if (!_activeWeapon || Time.deltaTime <= 0f)
                return;
            if (!Active)
            {
                FinishEffect(false);
                return;
            }

            if (_activeWeapon.PoseType != WeaponDefinition.WeaponPoseType.Scythe ||
                Time.time < _nextPulse)
                return;

            var settings = _activeWeapon.UltimateEffect;
            _nextPulse = Time.time + Mathf.Max(0.05f, settings.auraInterval);
            Vector3 center = transform.position + Vector3.up * settings.auraCenterHeight;
            _pulseTargets.Clear();
            Physics.SyncTransforms();
            foreach (Collider target in CombatArea.QueryCylinder(center,
                settings.auraRadius, settings.auraHeight, _auraTargets))
            {
                if (!CombatArea.IntersectsCylinder(target, center, settings.auraRadius, settings.auraHeight))
                    continue;
                var health = target.GetComponentInParent<HealthComponent>();
                if (health && health != _health && _pulseTargets.Add(health))
                    health.ReceiveDamage(settings.auraDamage);
            }
        }

        private void OnBasicDamaged(HealthComponent target, float applied)
        {
            if (!Active || !(target is GuardianHealth) || applied <= 0f)
                return;
            var settings = _activeWeapon.UltimateEffect;
            if (_activeWeapon.PoseType == WeaponDefinition.WeaponPoseType.Sword)
                _health.AddShield(settings.swordShieldPerBasic);
            else if (_activeWeapon.PoseType == WeaponDefinition.WeaponPoseType.Scythe)
                _health.Heal(settings.scytheHealingPerBasic);
        }

        private void FinishEffect(bool clearParticles)
        {
            _activeWeapon = null;
            _pulseTargets.Clear();
            if (_scytheAura)
                _scytheAura.Stop(true, clearParticles
                    ? ParticleSystemStopBehavior.StopEmittingAndClear
                    : ParticleSystemStopBehavior.StopEmitting);
        }

        private void ResetEffects()
        {
            FinishEffect(true);
            _health.ClearShield();
        }

        private void OnDrawGizmosSelected()
        {
            var equipment = GetComponent<PlayerEquipment>();
            if (!equipment || !equipment.EquippedWeapon ||
                equipment.EquippedWeapon.PoseType != WeaponDefinition.WeaponPoseType.Scythe)
                return;
            var settings = equipment.EquippedWeapon.UltimateEffect;
            Vector3 center = transform.position + Vector3.up * settings.auraCenterHeight;
            Vector3 vertical = Vector3.up * settings.auraHeight * 0.5f;
            Gizmos.color = Color.magenta;
            Vector3 previous = Vector3.right * settings.auraRadius;
            for (int i = 1; i <= 40; i++)
            {
                Vector3 next = Quaternion.AngleAxis(i * 9f, Vector3.up) *
                    Vector3.right * settings.auraRadius;
                Gizmos.DrawLine(center + previous + vertical, center + next + vertical);
                Gizmos.DrawLine(center + previous - vertical, center + next - vertical);
                if (i % 10 == 0)
                    Gizmos.DrawLine(center + next - vertical, center + next + vertical);
                previous = next;
            }
        }
    }
}
