using System;
using System.Collections;
using FinalVFX.Player;
using Unity.Cinemachine;
using UnityEngine;

namespace FinalVFX.Core
{
    [DisallowMultipleComponent]
    public sealed class WeaponSelectionCamera : MonoBehaviour
    {
        [SerializeField] private PlayerCameraController _cameraController;
        [SerializeField] private CinemachineBrain _brain;
        [SerializeField] private Camera _outputCamera;
        [SerializeField] private CinemachineCamera _selectionCamera;
        [SerializeField] private CinemachineCamera _gameplayCamera;
        [SerializeField] private Transform _firstPersonPose;
        [SerializeField, Min(0.1f)] private float _lookSharpness = 12f;
        [SerializeField] private Renderer[] _playerRenderers;
        [SerializeField] private Transform _headPoint;
        [SerializeField, Min(0.1f)] private float _revealDistance = 0.7f;
        [SerializeField, Min(0.01f)] private float _initialAimDuration = 0.5f;
        [SerializeField, Min(1f)] private float _cameraTimeout = 15f;

        public event Action<bool> VisibilityChanged;
        public event Action<string> Failed;

        public bool ValidateSetup()
        {
            if (_cameraController != null && _brain != null && _outputCamera != null &&
                _selectionCamera != null && _gameplayCamera != null &&
                _selectionCamera != _gameplayCamera && _firstPersonPose != null && _headPoint != null)
                return true;
            Debug.LogError("WeaponSelectionCamera: revisa controlador, Brain, camaras distintas, pose inicial y cabeza.", this);
            return false;
        }

        public void Initialize()
        {
            _selectionCamera.Priority = 100;
            _gameplayCamera.Priority = 10;
        }

        public void PrepareSelection()
        {
            _cameraController.ResetOrientation();
            _cameraController.FollowTarget();
            _selectionCamera.transform.SetPositionAndRotation(
                _firstPersonPose.position, _firstPersonPose.rotation);
            _selectionCamera.PreviousStateIsValid = false;
            _gameplayCamera.PreviousStateIsValid = false;
            SetPlayerVisible(false);
        }

        public IEnumerator EnterSelection(Vector3 midpoint)
        {
            _selectionCamera.transform.LookAt(midpoint, Vector3.up);
            yield return WaitForCamera(_selectionCamera);
        }

        public IEnumerator AimInitially(Transform focusPoint)
        {
            Quaternion initialRotation = _selectionCamera.transform.rotation;
            Quaternion targetRotation = Quaternion.LookRotation(
                focusPoint.position - _selectionCamera.transform.position, Vector3.up);
            float elapsed = 0f;
            while (elapsed < _initialAimDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                _selectionCamera.transform.rotation = Quaternion.Slerp(initialRotation,
                    targetRotation, Mathf.SmoothStep(0f, 1f,
                        Mathf.Clamp01(elapsed / _initialAimDuration)));
                yield return null;
            }
        }

        public void AimAtSelection(Transform focusPoint)
        {
            Transform cameraTransform = _selectionCamera.transform;

            Vector3 direction =
                focusPoint.position -
                cameraTransform.position;

            if (direction.sqrMagnitude < 0.0001f)
                return;

            Quaternion target =
                Quaternion.LookRotation(direction, Vector3.up);

            float blend = 1f - Mathf.Exp(
                    -_lookSharpness * Time.unscaledDeltaTime);

            cameraTransform.rotation = Quaternion.Slerp(
                cameraTransform.rotation, target, blend);
        }

        public IEnumerator EnterGameplay()
        {
            _gameplayCamera.Priority = 200;

            // Permitir que el Brain evalúe el cambio de prioridad.
            yield return null;

            float elapsed = 0f;
            bool playerRevealed = false;

            while (true)
            {
                elapsed += Time.unscaledDeltaTime;

                if (!playerRevealed &&
                    Vector3.Distance(
                        _outputCamera.transform.position,
                        _headPoint.position) >= _revealDistance)
                {
                    SetPlayerVisible(true);
                    playerRevealed = true;
                }

                bool cameraReady =
                    ReferenceEquals(_brain.ActiveVirtualCamera, _gameplayCamera) &&
                    !_brain.IsBlending;

                if (cameraReady)
                    break;

                if (elapsed > _cameraTimeout)
                {
                    Fail("No terminó la transición a tercera persona.");
                    yield break;
                }

                yield return null;
            }

            SetPlayerVisible(true);
        }

        private IEnumerator WaitForCamera(CinemachineCamera target)
        {
            float elapsed = 0f;
            yield return null;

            while (!ReferenceEquals(_brain.ActiveVirtualCamera, target) ||
                   _brain.IsBlending)
            {
                elapsed += Time.unscaledDeltaTime;

                if (elapsed > _cameraTimeout)
                {
                    Fail("No se pudo activar la cámara de selección.");
                    yield break;
                }

                yield return null;
            }
        }

        public void SetPlayerVisible(bool visible)
        {
            if (_playerRenderers != null)
            {
                foreach (Renderer target in _playerRenderers)
                {
                    if (target != null)
                        target.forceRenderingOff = !visible;
                }
            }

            VisibilityChanged?.Invoke(visible);
        }

        private void Fail(string message) => Failed?.Invoke(message);
    }
}
