using System.Collections;
using FinalVFX.Combat;
using FinalVFX.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FinalVFX.Menu
{
    [DefaultExecutionOrder(-200)]
    public sealed class PauseMenu : MonoBehaviour
    {
        public static bool IsPaused { get; private set; }
        public static bool PausePressedThisFrame { get; private set; }

        [Header("Player")]
        [SerializeField] private PlayerController _player;
        [SerializeField] private FinalVFX.Player.PlayerInputManager _playerInput;
        [SerializeField] private HealthComponent _playerHealth;

        [Header("UI")]
        [SerializeField] private GameObject _pauseRoot;
        [SerializeField] private GameObject _mainPanel;
        [SerializeField] private GameObject _settingsPanel;
        [SerializeField] private SettingsPanelUI _settingsUI;
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private CanvasGroup _blackScreen;

        [Header("Pause")]
        [SerializeField] private InputActionReference _pauseReference;
        [SerializeField] private InputActionReference _cancelReference;

        [SerializeField]
        private UnityEngine.InputSystem.UI.InputSystemUIInputModule _uiInputModule;

        private InputAction _pauseAction;
        private InputAction _cancelAction;

        [Header("Return to menu")]
        [SerializeField] private string _menuScene = "SCN_MainMenu";
        [SerializeField, Min(0.01f)] private float _fadeSeconds = 0.6f;

        [Header("Arena introduction")]
        [SerializeField]
        private FinalVFX.Core.ArenaIntroDirector _arenaIntro;

        [Header("Pause effects")]
        [SerializeField] private PausePresentation _presentation;

        private bool _ownsPause;
        private bool _resuming;
        private bool _leaving;
        private bool _inputWasEnabled;
        private float _previousTimeScale;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            IsPaused = false;
            PausePressedThisFrame = false;
        }

        private void Awake()
        {
            _pauseAction = _pauseReference.action.Clone();
            _cancelAction = _cancelReference.action.Clone();

            // Durante el juego o la selección de armas no hay menú interactivo.
            _uiInputModule.enabled = false;

            _pauseRoot.SetActive(false);
            _settingsPanel.SetActive(false);

            _blackScreen.alpha = 0f;
            _blackScreen.blocksRaycasts = false;
            _blackScreen.interactable = false;
        }

        private void OnEnable()
        {
            _pauseAction.Enable();
            _cancelAction.Enable();
        }

        private void Update()
        {
            PausePressedThisFrame =
                Application.isFocused &&
                _pauseAction.WasPressedThisFrame();

            if (_leaving || _resuming || !Application.isFocused)
                return;

            bool cancelPressed = _cancelAction.WasPressedThisFrame();

            if (!_ownsPause)
            {
                if (PausePressedThisFrame)
                    Pause();

                return;
            }

            if (!PausePressedThisFrame && !cancelPressed)
                return;

            if (_settingsPanel.activeInHierarchy)
            {
                if (_settingsUI.IsDropdownOpen)
                    _settingsUI.CloseDropdown();
                else
                    BackToPause();

                return;
            }

            Resume();
        }

        private void LateUpdate()
        {
            // Mantiene el cursor disponible incluso si otro sistema
            // actualiza su estado mientras el juego está pausado.
            if (_ownsPause && !_leaving)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        private void Pause()
        {
            if (_ownsPause ||
                Time.timeScale <= 0f ||
                _playerHealth.IsDead ||
                (_arenaIntro != null && _arenaIntro.BlocksPause))
            {
                return;
            }

            _previousTimeScale = Time.timeScale;
            _inputWasEnabled = _playerInput.enabled;

            _ownsPause = true;
            IsPaused = true;

            _playerInput.enabled = false;
            Time.timeScale = 0f;
            _presentation.SetPaused(true);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            _settingsPanel.SetActive(false);
            _mainPanel.SetActive(true);
            _pauseRoot.SetActive(true);

            _uiInputModule.enabled = true;

            Select(_resumeButton.gameObject);
        }

        public void Resume()
        {
            if (!_ownsPause || _resuming || _leaving)
                return;

            _resuming = true;
            _settingsUI.CancelPreview();
            _settingsUI.CloseDropdown();
            _uiInputModule.enabled = false;
            _pauseRoot.SetActive(false);
            Select(null);

            StartCoroutine(ResumeRoutine());
        }

        private IEnumerator ResumeRoutine()
        {
            // Conserva la pausa durante el frame de la interacción.
            // Evita convertir el clic/Submit de Reanudar en un ataque.
            yield return null;

            RestoreGameplay();
            _resuming = false;
        }

        public void OpenSettings()
        {
            if (!_ownsPause || _resuming || _leaving)
                return;

            if (!_settingsUI.Refresh())
                return;

            _mainPanel.SetActive(false);
            _settingsPanel.SetActive(true);
            Select(_settingsUI.FirstSelected);
        }

        public void BackToPause()
        {
            if (!_ownsPause || _resuming || _leaving)
                return;

            _settingsUI.CancelPreview();
            _settingsUI.CloseDropdown();
            _settingsPanel.SetActive(false);
            _mainPanel.SetActive(true);
            Select(_settingsButton.gameObject);
        }

        public void ReturnToMenu()
        {
            if (!_ownsPause || _resuming || _leaving)
                return;

            if (string.IsNullOrWhiteSpace(_menuScene) ||
                !Application.CanStreamedLevelBeLoaded(_menuScene))
            {
                Debug.LogError(
                    $"PauseMenu: agrega '{_menuScene}' a Scene List.",
                    this);
                return;
            }

            _leaving = true;
            _uiInputModule.enabled = false;
            _settingsUI.CancelPreview();
            _settingsUI.CloseDropdown();
            Select(null);

            // Bloquea la interfaz durante el fundido.
            _blackScreen.blocksRaycasts = true;

            StartCoroutine(ReturnRoutine());
        }

        private IEnumerator ReturnRoutine()
        {
            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, _fadeSeconds);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;

                _blackScreen.alpha = Mathf.SmoothStep(
                    0f, 1f, Mathf.Clamp01(elapsed / duration));

                yield return null;
            }

            _blackScreen.alpha = 1f;
            yield return null;
            _presentation.ResetImmediately();

            // Solo el controlador sobrevive temporalmente a la carga.
            // La interfaz y la arena se destruyen normalmente.
            DontDestroyOnLoad(gameObject);

            yield return SceneManager.LoadSceneAsync(
                _menuScene, LoadSceneMode.Single);

            // Restaurar después de cargar evita reactivar el combate
            // mientras todavía se está abandonando la arena.
            _ownsPause = false;
            IsPaused = false;
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            Destroy(gameObject);
        }

        private void RestoreGameplay()
        {
            if (!_ownsPause)
                return;

            _ownsPause = false;
            IsPaused = false;

            Time.timeScale = _previousTimeScale;
            _presentation.SetPaused(false);

            if (_playerInput != null)
                _playerInput.enabled = _inputWasEnabled;

            if (_player != null)
                _player.RefreshCursorState();
        }

        private static void Select(GameObject target)
        {
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(target);
        }

        private void OnDisable()
        {
            _pauseAction?.Disable();
            _cancelAction?.Disable();

            if (_uiInputModule != null)
                _uiInputModule.enabled = false;

            StopAllCoroutines();
            RestoreGameplay();

            if (_pauseRoot != null)
                _pauseRoot.SetActive(false);

            _resuming = false;

            if (_presentation != null)
                _presentation.ResetImmediately();
        }

        private void OnDestroy()
        {
            _pauseAction?.Dispose();
            _cancelAction?.Dispose();
        }
    }
}