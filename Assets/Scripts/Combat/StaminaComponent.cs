using System;
using UnityEngine;

namespace FinalVFX.Combat
{
    public class StaminaComponent : MonoBehaviour
    {
        [SerializeField] private float _maxStamina;
        [SerializeField] private float _regenerationRate;
        [SerializeField] private float _regenerationDelay;
        private float _regenerationAllowedAt;
        private int _lastActiveFrame = -1;


        public float Current { get; private set; }
        public float Maximum => _maxStamina;

        public event Action<float, float> Changed;

        private void Awake()
        {
            Current = _maxStamina;
        }

        private void LateUpdate()
        {
            if (_lastActiveFrame == Time.frameCount || Time.time < _regenerationAllowedAt || Current >= Maximum) return;

            SetCurrent(Current + _regenerationRate * Time.deltaTime);
        }

        public bool CanSpend(float amount)
        {
            return amount >= 0f && Current >= amount;
        }

        public bool TrySpend(float amount)
        {
            if (!CanSpend(amount)) return false;

            NotifyActionActive();
            SetCurrent(Current - amount);
            return true;
        }

        public float ConsumeUpTo(float amount)
        {
            if (amount <= 0f || Current <= 0f)
            {
                return 0f;
            }

            float consumed = Mathf.Min(Current, amount);

            NotifyActionActive();
            SetCurrent(Current - consumed);
            return consumed;
        }

        public void NotifyActionActive()
        {
            _lastActiveFrame = Time.frameCount;
            _regenerationAllowedAt = Time.time + _regenerationDelay;
        }

        private void SetCurrent(float value)
        {
            float nextValue = Mathf.Clamp(value, 0f, Maximum);

            if (nextValue == Current) return;

            Current = nextValue;
            Changed?.Invoke(Current, Maximum);
        }
    }
}
