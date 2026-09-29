using System;
using UnityEngine;

namespace FinalVFX.Combat
{
    [DisallowMultipleComponent]
    public class StaminaComponent : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float _maxStamina = 100f;
        [SerializeField, Min(0f)] private float _regenerationRate = 25f;
        [SerializeField, Min(0f)] private float _regenerationDelay = 1f;

        private float _regenerationAllowedAt;
        private int _lastActiveFrame = -1;

        public float Current { get; private set; }
        public float Maximum => _maxStamina;

        public event Action<float, float> Changed;

        private void Awake()
        {
            Current = Maximum;
        }

        private void LateUpdate()
        {
            // No regenera si se usó stamina en este frame o si todavía está en el delay
            if (_lastActiveFrame == Time.frameCount || Time.time < _regenerationAllowedAt || Current >= Maximum)
                return;

            SetCurrent(Current + _regenerationRate * Time.deltaTime);
        }

        public bool CanSpend(float amount)
        {
            return Current >= amount;
        }

        public bool TrySpend(float amount)
        {
            if (!CanSpend(amount))
                return false;

            NotifyActionActive();
            SetCurrent(Current - amount);
            return true;
        }

        public float ConsumeUpTo(float amount)
        {
            if (amount <= 0f || Current <= 0f)
                return 0f;

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

        public void RestoreFull()
        {
            _lastActiveFrame = -1;
            _regenerationAllowedAt = 0f;
            SetCurrent(Maximum);
        }

        private void SetCurrent(float value)
        {
            float nextValue = Mathf.Clamp(value, 0f, Maximum);

            if (nextValue == Current)
                return;

            Current = nextValue;
            Changed?.Invoke(Current, Maximum);
        }

        private void OnValidate()
        {
            _maxStamina = Mathf.Max(0.01f, _maxStamina);
            _regenerationRate = Mathf.Max(0f, _regenerationRate);
            _regenerationDelay = Mathf.Max(0f, _regenerationDelay);
        }
    }
}
