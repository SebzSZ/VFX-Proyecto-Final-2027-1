using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FinalVFX.Core
{
    [DisallowMultipleComponent]
    public sealed class WeaponSelectionInput : MonoBehaviour
    {
        [SerializeField] private InputActionReference _mouseNavigateReference;
        [SerializeField] private InputActionReference _navigateReference;
        [SerializeField] private InputActionReference _confirmReference;
        [SerializeField, Min(1f)] private float _mouseSelectionDistance = 30f;

        private InputAction _navigate;
        private InputAction _confirm;
        private InputAction _mouseNavigate;
        private float _mouseTravel;
        private float _lastMouseMotion;
        private bool _inputArmed;
        private bool _navigationHeld;
        private bool _listening;

        public bool CanRead => Application.isFocused && Time.timeScale > 0f;
        public event Action<int> SelectionRequested;
        public event Action ConfirmRequested;

        public bool ValidateSetup()
        {
            if (_navigateReference != null &&
                _navigateReference.action != null &&
                _confirmReference != null &&
                _confirmReference.action != null &&
                _mouseNavigateReference != null &&
                _mouseNavigateReference.action != null)
            {
                return true;
            }

            Debug.LogError(
                "WeaponSelectionInput necesita Navigate, Confirm y MouseNavigate.",
                this);

            return false;
        }

        public void Initialize()
        {
            _navigate = _navigateReference.action.Clone();
            _confirm = _confirmReference.action.Clone();
            _mouseNavigate = _mouseNavigateReference.action.Clone();
        }

        public void BeginSelection()
        {
            _navigate.Enable();
            _confirm.Enable();
            _mouseNavigate.Enable();
            _inputArmed = false;
            _listening = true;
        }

        public void EndSelection()
        {
            _listening = false;
            _navigate?.Disable();
            _confirm?.Disable();
            _mouseNavigate?.Disable();
        }

        public void Tick()
        {
            if (!_listening)
                return;

            if (!CanRead)
            {
                _inputArmed = false;
                _mouseTravel = 0f;
                return;
            }

            float horizontal = _navigate.ReadValue<Vector2>().x;

            // Exigir reposo al entrar o recuperar foco.
            if (!_inputArmed)
            {
                if (Mathf.Abs(horizontal) < 0.25f &&
                    !_confirm.IsPressed())
                {
                    _inputArmed = true;
                    _navigationHeld = false;
                    _mouseTravel = 0f;
                }

                return;
            }

            if (Mathf.Abs(horizontal) < 0.25f)
                _navigationHeld = false;

            if (!_navigationHeld && Mathf.Abs(horizontal) > 0.6f)
            {
                SelectionRequested?.Invoke(horizontal < 0f ? 0 : 1);
                _navigationHeld = true;
                _mouseTravel = 0f;
            }
            else if (Mathf.Abs(horizontal) < 0.25f)
            {
                float delta = _mouseNavigate.ReadValue<Vector2>().x;
                if (Mathf.Abs(delta) > 0.01f)
                {
                    if (Time.unscaledTime - _lastMouseMotion > 0.2f ||
                        Mathf.Sign(delta) != Mathf.Sign(_mouseTravel))
                        _mouseTravel = 0f;
                    _lastMouseMotion = Time.unscaledTime;
                    _mouseTravel += delta;
                    if (Mathf.Abs(_mouseTravel) >= _mouseSelectionDistance)
                    {
                        SelectionRequested?.Invoke(_mouseTravel < 0f ? 0 : 1);
                        _mouseTravel = 0f;
                    }
                }
            }

            if (_confirm.WasPressedThisFrame())
                ConfirmRequested?.Invoke();
        }

        private void OnDisable() => EndSelection();

        private void OnDestroy()
        {
            _navigate?.Dispose();
            _confirm?.Dispose();
            _mouseNavigate?.Dispose();
        }
    }
}
