using System;
using System.Collections.Generic;
using UnityEngine;

namespace FinalVFX.Boss
{
    public sealed class BossDecisionSelector
    {
        private readonly BossAISettings _settings;
        private readonly System.Random _random;
        private readonly Dictionary<BossAttackId, float> _readyAt = new Dictionary<BossAttackId, float>();
        private readonly List<BossAttackRule> _candidates = new List<BossAttackRule>();
        private readonly List<float> _weights = new List<float>();
        private readonly HashSet<BossAttackId> _seen = new HashSet<BossAttackId>();
        private BossAttackId? _lastAttack;

        public BossDecisionSelector(BossAISettings settings, int seed)
        {
            _settings = settings;
            _random = new System.Random(seed);
        }

        public bool TryChoose(
            float distance,
            float now,
            out BossAttackId attack)
        {
            attack = default;
            _candidates.Clear();
            _weights.Clear();
            _seen.Clear();

            if (_settings.Attacks == null || distance > _settings.VisionDistance)
                return false;

            bool isMelee = distance <= _settings.MeleeDistance;

            float farDistance = Mathf.Max(
                _settings.NearDistance + 0.1f,
                _settings.FarDistance);

            float distanceFactor = Mathf.InverseLerp(
                _settings.NearDistance, farDistance, distance);

            float totalWeight = 0f;

            foreach (BossAttackRule rule in _settings.Attacks)
            {
                if (rule == null || !_seen.Add(rule.Id))
                    continue;

                if (_readyAt.TryGetValue(rule.Id, out float readyAt)
                    && now < readyAt)
                    continue;

                if (rule.Id == BossAttackId.AtaqueBasico && !isMelee)
                    continue;

                float weight = isMelee
                    ? Mathf.Max(0f, rule.MeleeWeight)
                    : Mathf.Lerp(
                        Mathf.Max(0f, rule.NearWeight),
                        Mathf.Max(0f, rule.FarWeight),
                        distanceFactor);

                if (rule.Id != BossAttackId.AtaqueBasico &&
                    _lastAttack.HasValue &&
                    _lastAttack.Value == rule.Id)
                {
                    weight *= Mathf.Clamp01(_settings.RepeatMultiplier);
                }

                if (weight <= 0f)
                    continue;

                _candidates.Add(rule);
                _weights.Add(weight);
                totalWeight += weight;
            }

            if (_candidates.Count == 0)
                return false;

            double roll = _random.NextDouble() * totalWeight;
            int selectedIndex = _candidates.Count - 1;

            for (int i = 0; i < _candidates.Count; i++)
            {
                roll -= _weights[i];

                if (roll < 0d)
                {
                    selectedIndex = i;
                    break;
                }
            }

            BossAttackRule selected = _candidates[selectedIndex];
            attack = selected.Id;

            _readyAt[attack] = now + Mathf.Max(0f, selected.Cooldown);
            _lastAttack = attack;
            return true;
        }
    }
}