using System;
using FinalVFX.Combat;
using UnityEngine;

namespace FinalVFX.Boss
{
    public sealed class GuardianHealth : HealthComponent
    {
        [SerializeField, Min(0.05f)] private float _stunSeconds;
        [SerializeField, Min(0f)] private float _pushDistance;
        private int _nextQuarter = 3;
        private float _stunnedUntil;
        public int Phase => _nextQuarter <= 1 ? 2 : 1;
        public bool Stunned => Time.time < _stunnedUntil;
        public event Action<float> StunStarted;

        protected override float DamageFloor => Stunned ? Current : Maximum * (_nextQuarter / 4f);

        protected override void OnDamageCommitted()
        {
            if (_nextQuarter == 0 || Current > DamageFloor)
                return;

            _nextQuarter--;
            _stunnedUntil = Time.time + _stunSeconds;
            StunStarted?.Invoke(_pushDistance);
        }
    }
}
