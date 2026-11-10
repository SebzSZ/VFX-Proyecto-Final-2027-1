using System;
using FinalVFX.Combat;
using UnityEngine;

namespace FinalVFX.Boss
{
    public sealed class GuardianHealth : HealthComponent
    {
        [SerializeField, Min(0.05f)] private float _stunSeconds = 2f;
        [SerializeField, Min(0f)] private float _pushDistance = 3f;
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
            Debug.Log($"[Guardián] Vida {Normalized:P0} · fase {Phase} · aturdido", this);
            StunStarted?.Invoke(_pushDistance);
        }
    }
}
