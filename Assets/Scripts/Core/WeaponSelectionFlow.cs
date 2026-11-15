using System.Collections;
using FinalVFX.Combat;
using FinalVFX.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FinalVFX.Core
{
    [DisallowMultipleComponent]
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

        [Header("Sequence components")]
        [SerializeField] private WeaponSelectionInput _input;
        [SerializeField] private WeaponSelectionPresentation _presentation;
        [SerializeField] private WeaponSelectionCamera _camera;

        [Header("Player")]
        [SerializeField] private PlayerController _player;
        [SerializeField] private PlayerEquipment _equipment;
        [SerializeField] private InvulnerabilityComponent _invulnerability;
        [SerializeField] private CharacterController _characterController;
        [SerializeField] private Transform _startPos;

        [Header("Arena introduction")]
        [SerializeField] private ArenaIntroDirector _arenaIntro;

        [Header("Encounter")]
        [SerializeField] private CanvasGroup _blackScreen;
        [SerializeField] private GameObject _gameplayHUD;
        [SerializeField] private GameObject _encounterActors;
        [SerializeField, Min(0.01f)] private float _fadeInDuration = 0.5f;
        [SerializeField, Min(0f)] private float _beforeThirdPersonDelay = 0.3f;
        [SerializeField, Min(0.01f)] private float _deathFadeDuration = 0.8f;

        private FlowState _state = FlowState.Entering;
        private int _selectedIndex = -1;
        private bool _configured;
        private bool _started;

        private void Awake()
        {
            if (!ValidateSetup())
            {
                enabled = false;
                return;
            }
            // Explicit initialization avoids depending on component Awake ordering.
            _input.Initialize();
            _presentation.Initialize();
            _camera.Initialize();
            _configured = true;
            _blackScreen.alpha = 1f;
            _blackScreen.blocksRaycasts = true;
            _blackScreen.interactable = false;
            _gameplayHUD.SetActive(false);
            _encounterActors.SetActive(false);
        }

        private bool ValidateSetup()
        {
            Object[] required = { _input, _presentation, _camera, _player, _equipment,
                _invulnerability, _characterController, _startPos, _blackScreen,
                _gameplayHUD, _encounterActors, _arenaIntro };
            foreach (Object reference in required)
            {
                if (reference != null) continue;
                Debug.LogError("WeaponSelectionFlow tiene referencias sin asignar.", this);
                return false;
            }
            if (!_equipment.HasSocket)
            {
                Debug.LogError("PlayerEquipment necesita un WeaponSocket.", _equipment);
                return false;
            }
            return _input.ValidateSetup() && _presentation.ValidateSetup() && _camera.ValidateSetup() && _arenaIntro.ValidateSetup();
        }

        private void OnEnable()
        {
            if (!_configured) return;
            _input.SelectionRequested += Select;
            _input.ConfirmRequested += ConfirmSelection;
            _camera.VisibilityChanged += _equipment.SetVisible;
            _camera.Failed += Fail;
            _player.Died += HandleDeath;
        }

        private IEnumerator Start()
        {
            _started = true;
            _player.SetSequenceLocked(true);
            _player.SetGameplayEnabled(false);
            _invulnerability.SetWindow(this, Time.time, float.PositiveInfinity);

            _characterController.enabled = false;
            _player.transform.SetPositionAndRotation(_startPos.position, _startPos.rotation);
            _characterController.enabled = true;

            _camera.PrepareSelection();
            _presentation.BeginAppear();
            yield return _camera.EnterSelection(_presentation.Midpoint);
            if (_state == FlowState.Failed) yield break;

            yield return FadeScreen(0f, _fadeInDuration);
            yield return _presentation.Appear();
            SetSelection(0);
            yield return _camera.AimInitially(_presentation.FocusPoint(0));

            _presentation.ShowSelection();
            _input.BeginSelection();
            _state = FlowState.Selecting;
        }

        private void Update()
        {
            if (_state != FlowState.Selecting) return;
            // Aim before reading navigation, exactly as in the original sequence.
            if (_input.CanRead)
                _camera.AimAtSelection(_presentation.FocusPoint(_selectedIndex));
            _input.Tick();
        }

        private void LateUpdate()
        {
            if (_configured)
                _presentation.TickNames(_state == FlowState.Selecting, _selectedIndex);
        }

        private void Select(int index)
        {
            if (_state == FlowState.Selecting)
                SetSelection(index);
        }

        private void SetSelection(int index)
        {
            _selectedIndex = index;
            _presentation.Select(index);
        }

        private void ConfirmSelection()
        {
            if (_state != FlowState.Selecting ||
                !_equipment.TryEquip(_presentation.Definition(_selectedIndex)))
                return;

            _equipment.SetVisible(false);
            _state = FlowState.Transitioning;
            _input.EndSelection();
            _presentation.BeginDisappear(_selectedIndex);
            StartCoroutine(EnterGameplay());
        }

        private IEnumerator EnterGameplay()
        {
            yield return _presentation.Disappear();
            if (_beforeThirdPersonDelay > 0f)
                yield return new WaitForSeconds(_beforeThirdPersonDelay);
            yield return _camera.EnterGameplay();
            if (_state == FlowState.Failed) yield break;

            _invulnerability.RemoveWindow(this);
            _state = FlowState.Playing;
            _gameplayHUD.SetActive(true);
            _player.SetSequenceLocked(false);
            _player.SetGameplayEnabled(true);
            _encounterActors.SetActive(true);
            _arenaIntro.Arm();
        }

        private void HandleDeath()
        {
            if (_state != FlowState.Playing)
                return;

            _state = FlowState.Restarting;

            _player.SetSequenceLocked(true);

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
                elapsed += Time.deltaTime;

                float progress = Mathf.SmoothStep(
                    0f, 1f, Mathf.Clamp01(elapsed / duration));

                _blackScreen.alpha =
                    Mathf.Lerp(initial, target, progress);

                yield return null;
            }

            _blackScreen.alpha = target;
            _blackScreen.blocksRaycasts = target > 0f;
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
            if (!_configured) return;
            _player.Died -= HandleDeath;
            _input.SelectionRequested -= Select;
            _input.ConfirmRequested -= ConfirmSelection;
            _input.EndSelection();
            StopAllCoroutines();
            if (_started)
            {
                _invulnerability.RemoveWindow(this);
                _camera.SetPlayerVisible(true);
            }
            _camera.VisibilityChanged -= _equipment.SetVisible;
            _camera.Failed -= Fail;
        }
    }
}

