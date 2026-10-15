using System.Collections;
using FinalVFX.Combat;
using FinalVFX.Player;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace FinalVFX.Core
{
    public sealed class WeaponSelectionFlow : MonoBehaviour
    {
        private enum FlowState
        {
            Entering,
            Selecting,
            Transitioning,
            Playing,
            Restarting,
            Failed
        }

        [Header("Player")]
        [SerializeField] private PlayerController _player;
        [SerializeField] private PlayerCameraController _cameraController;
        [SerializeField] private PlayerEquipment _equipment;
        [SerializeField] private HealthComponent _health;
        [SerializeField] private InvulnerabilityComponent _invulnerability;
        [SerializeField] private CharacterController _characterController;
        [SerializeField] private Transform _startPos;

        [Header("Exactly two displays: left, right")]
        [SerializeField] private WeaponDisplay[] _displays;

        [Header("Selection input")]
        [SerializeField] private InputActionReference _navigateReference;
        [SerializeField] private InputActionReference _confirmReference;
        [SerializeField, Min(1f)] private float _mouseSelectionDistance = 30f;

        [Header("Cameras")]
        [SerializeField] private CinemachineBrain _brain;
        [SerializeField] private Camera _outputCamera;
        [SerializeField] private CinemachineCamera _selectionCamera;
        [SerializeField] private CinemachineCamera _gameplayCamera;
        [SerializeField] private Transform _firstPersonPose;
        [SerializeField, Min(0.1f)] private float _lookSharpness = 12f;

        [Header("Hide body while camera is inside it")]
        [SerializeField] private Renderer[] _playerRenderers;
        [SerializeField] private Transform _headPoint;
        [SerializeField, Min(0.1f)] private float _revealDistance = 0.7f;

        [Header("Scene objects")]
        [SerializeField] private CanvasGroup _blackScreen;
        [SerializeField] private GameObject _selectionUI;
        [SerializeField] private GameObject _gameplayHUD;
        [SerializeField] private GameObject _encounterActors;

        [Header("Durations")]
        [SerializeField, Min(0.01f)] private float _fadeInDuration = 0.5f;
        [SerializeField, Min(0.01f)] private float _weaponFadeDuration = 1f;
        [SerializeField, Min(0.01f)] private float _weaponAppearDuration = 1.5f;
        [SerializeField, Min(0.01f)] private float _initialAimDuration = 0.5f;
        [SerializeField, Min(0f)] private float _beforeThirdPersonDelay = 0.3f;
        [SerializeField, Min(0.01f)] private float _deathFadeDuration = 0.8f;
        [SerializeField, Min(1f)] private float _cameraTimeout = 15f;

        private InputAction _navigate;
        private InputAction _confirm;
        private InputAction _mouseNavigate;
        private float _mouseTravel;
        private float _lastMouseMotion;
        private FlowState _state = FlowState.Entering;

        private int _selectedIndex = -1;
        private bool _inputArmed;
        private bool _navigationHeld;
        private bool _configured;
        private bool _started;

        private void Awake()
        {
            if (!ValidateSetup())
            {
                enabled = false;
                return;
            }

            _configured = true;

            // Instancias propias: no alteran las acciones de otros sistemas.
            _navigate = _navigateReference.action.Clone();
            _confirm = _confirmReference.action.Clone();
            _mouseNavigate = new InputAction("WeaponSelectionMouse", InputActionType.Value,
                "<Mouse>/delta", expectedControlType: "Vector2");

            _blackScreen.alpha = 1f;
            _blackScreen.blocksRaycasts = true;
            _blackScreen.interactable = false;

            _selectionCamera.Priority = 100;
            _gameplayCamera.Priority = 10;

            _selectionUI.SetActive(false);
            _gameplayHUD.SetActive(false);
            _encounterActors.SetActive(false);
        }

        private bool ValidateSetup()
        {
            Object[] required =
            {
                _player, _cameraController, _equipment, _health,
                _invulnerability, _characterController, _startPos,
                _navigateReference, _confirmReference,
                _brain, _outputCamera, _selectionCamera,
                _gameplayCamera, _firstPersonPose, _headPoint,
                _blackScreen, _selectionUI, _gameplayHUD,
                _encounterActors
            };

            foreach (Object reference in required)
            {
                if (reference == null)
                {
                    Debug.LogError(
                        "WeaponSelectionFlow tiene referencias sin asignar.",
                        this);
                    return false;
                }
            }

            if (_navigateReference.action == null ||
                _confirmReference.action == null ||
                !_equipment.HasSocket ||
                _selectionCamera == _gameplayCamera ||
                _displays == null ||
                _displays.Length != 2)
            {
                Debug.LogError(
                    "Revisa acciones, socket, cámaras y las dos exhibiciones.",
                    this);
                return false;
            }

            foreach (WeaponDisplay display in _displays)
            {
                if (display == null ||
                    display.Definition == null ||
                    display.Definition.EquippedPrefab == null)
                {
                    Debug.LogError(
                        "Cada exhibición necesita un perfil y prefab equipado.",
                        this);
                    return false;
                }
            }

            return true;
        }

        private void OnEnable()
        {
            if (_configured)
                _health.Died += HandleDeath;
        }

        private IEnumerator Start()
        {
            _started = true;

            _player.SetSequenceLocked(true);
            _player.SetGameplayEnabled(false);

            _invulnerability.SetWindow(
                this, Time.time, float.PositiveInfinity);

            _characterController.enabled = false;
            _player.transform.SetPositionAndRotation(
                _startPos.position, _startPos.rotation);
            _characterController.enabled = true;

            _cameraController.ResetOrientation();
            _cameraController.FollowTarget();

            _selectionCamera.transform.SetPositionAndRotation(
                _firstPersonPose.position, _firstPersonPose.rotation);

            // Descartar posiciones anteriores al teletransporte inicial.
            _selectionCamera.PreviousStateIsValid = false;
            _gameplayCamera.PreviousStateIsValid = false;

            SetPlayerVisible(false);

            foreach (WeaponDisplay display in _displays)
            {
                display.gameObject.SetActive(true);
                display.BeginAppear();
            }

            // Vista central sin selección mientras se materializan ambas armas.
            Vector3 midpoint = (_displays[0].FocusPoint.position +
                _displays[1].FocusPoint.position) * 0.5f;
            _selectionCamera.transform.LookAt(midpoint, Vector3.up);

            yield return WaitForCamera(_selectionCamera);

            if (_state == FlowState.Failed)
                yield break;

            yield return FadeScreen(0f, _fadeInDuration);

            yield return AnimateWeapons(true, _weaponAppearDuration);
            foreach (WeaponDisplay display in _displays)
                display.FinishAppear();

            Select(0);
            Quaternion initialRotation = _selectionCamera.transform.rotation;
            Quaternion swordRotation = Quaternion.LookRotation(
                _displays[0].FocusPoint.position - _selectionCamera.transform.position,
                Vector3.up);
            float aimElapsed = 0f;
            while (aimElapsed < _initialAimDuration)
            {
                aimElapsed += Time.unscaledDeltaTime;
                _selectionCamera.transform.rotation = Quaternion.Slerp(initialRotation,
                    swordRotation, Mathf.SmoothStep(0f, 1f,
                        Mathf.Clamp01(aimElapsed / _initialAimDuration)));
                yield return null;
            }

            _selectionUI.SetActive(true);
            _navigate.Enable();
            _confirm.Enable();
            _mouseNavigate.Enable();

            _inputArmed = false;
            _state = FlowState.Selecting;
        }

        private void Update()
        {
            if (_state != FlowState.Selecting)
                return;

            if (!Application.isFocused || Time.timeScale <= 0f)
            {
                _inputArmed = false;
                _mouseTravel = 0f;
                return;
            }

            AimAtSelection(false);

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
                Select(horizontal < 0f ? 0 : 1);
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
                        Select(_mouseTravel < 0f ? 0 : 1);
                        _mouseTravel = 0f;
                    }
                }
            }

            if (_confirm.WasPressedThisFrame())
                ConfirmSelection();
        }

        private void Select(int index)
        {
            _selectedIndex = index;

            for (int i = 0; i < _displays.Length; i++)
                _displays[i].SetSelected(i == index);
        }

        private void AimAtSelection(bool immediate)
        {
            Transform cameraTransform = _selectionCamera.transform;

            Vector3 direction =
                _displays[_selectedIndex].FocusPoint.position -
                cameraTransform.position;

            if (direction.sqrMagnitude < 0.0001f)
                return;

            Quaternion target =
                Quaternion.LookRotation(direction, Vector3.up);

            float blend = immediate
                ? 1f
                : 1f - Mathf.Exp(
                    -_lookSharpness * Time.unscaledDeltaTime);

            cameraTransform.rotation = Quaternion.Slerp(
                cameraTransform.rotation, target, blend);
        }

        private void ConfirmSelection()
        {
            if (!_equipment.TryEquip(
                _displays[_selectedIndex].Definition))
            {
                return;
            }

            // Se equipa inmediatamente, pero permanece oculta
            // hasta que la cámara salga del cuerpo.
            _equipment.SetVisible(false);

            _state = FlowState.Transitioning;
            _navigate.Disable();
            _confirm.Disable();
            _mouseNavigate.Disable();
            _selectionUI.SetActive(false);

            for (int i = 0; i < _displays.Length; i++)
                _displays[i].BeginDisappear(i == _selectedIndex);

            StartCoroutine(EnterGameplay());
        }

        private IEnumerator EnterGameplay()
        {
            // Completar la desaparición desde la vista de selección antes del blend.
            yield return AnimateWeapons(false, _weaponFadeDuration);
            foreach (WeaponDisplay display in _displays)
                display.gameObject.SetActive(false);
            if (_beforeThirdPersonDelay > 0f)
                yield return new WaitForSecondsRealtime(_beforeThirdPersonDelay);

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

            foreach (WeaponDisplay display in _displays)
                display.gameObject.SetActive(false);

            SetPlayerVisible(true);

            _invulnerability.RemoveWindow(this);
            _state = FlowState.Playing;

            _gameplayHUD.SetActive(true);
            _player.SetSequenceLocked(false);
            _player.SetGameplayEnabled(true);
            _encounterActors.SetActive(true);
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

        private IEnumerator AnimateWeapons(bool appearing, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.SmoothStep(0f, 1f,
                    Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration)));
                foreach (WeaponDisplay display in _displays)
                    display.SetVisibility(appearing ? progress : 1f - progress);
                yield return null;
            }
            foreach (WeaponDisplay display in _displays)
                display.SetVisibility(appearing ? 1f : 0f);
        }

        private void HandleDeath()
        {
            if (_state != FlowState.Playing)
                return;

            _state = FlowState.Restarting;

            _player.SetSequenceLocked(true);
            _player.SetGameplayEnabled(false);

            _gameplayHUD.SetActive(false);
            _encounterActors.SetActive(false);

            StartCoroutine(RestartScene());
        }

        private IEnumerator RestartScene()
        {
            yield return FadeScreen(1f, _deathFadeDuration);

            Time.timeScale = 1f;

            yield return SceneManager.LoadSceneAsync(
                gameObject.scene.path, LoadSceneMode.Single);
        }

        private IEnumerator FadeScreen(float target, float duration)
        {
            float initial = _blackScreen.alpha;
            float elapsed = 0f;

            _blackScreen.blocksRaycasts = true;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;

                float progress = Mathf.SmoothStep(
                    0f, 1f, Mathf.Clamp01(elapsed / duration));

                _blackScreen.alpha =
                    Mathf.Lerp(initial, target, progress);

                yield return null;
            }

            _blackScreen.alpha = target;
            _blackScreen.blocksRaycasts = target > 0f;
        }

        private void SetPlayerVisible(bool visible)
        {
            if (_playerRenderers != null)
            {
                foreach (Renderer target in _playerRenderers)
                {
                    if (target != null)
                        target.forceRenderingOff = !visible;
                }
            }

            if (_equipment != null)
                _equipment.SetVisible(visible);
        }

        private void Fail(string message)
        {
            _state = FlowState.Failed;

            Debug.LogError(
                message +
                " Revisa cámaras activas, prioridades, canales y blends.",
                this);
        }

        private void OnDisable()
        {
            if (_configured)
                _health.Died -= HandleDeath;

            _navigate?.Disable();
            _confirm?.Disable();
            _mouseNavigate?.Disable();
            StopAllCoroutines();

            if (_started)
            {
                _invulnerability.RemoveWindow(this);
                SetPlayerVisible(true);
            }
        }

        private void OnDestroy()
        {
            _navigate?.Dispose();
            _confirm?.Dispose();
            _mouseNavigate?.Dispose();
        }
    }
}
