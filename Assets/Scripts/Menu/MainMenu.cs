using FinalVFX.Core;
using FinalVFX.Menu;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FinalVFX.Menu
{
    [DefaultExecutionOrder(-200)]
    public sealed class MainMenu : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject _mainPanel;
        [SerializeField] private GameObject _settingsPanel;

        [Header("Navigation")]
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private SceneTransition _transition;
        [SerializeField] private string _storyScene = "SCN_Story";
        [SerializeField] private InputActionReference _cancelReference;
        private InputAction _cancelAction;

        [Header("Settings")]
        [SerializeField] private SettingsPanelUI _settingsUI;

        private void Awake()
        {
            _cancelAction = _cancelReference.action.Clone();

            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            _mainPanel.SetActive(true);
            _settingsPanel.SetActive(false);
        }

        private IEnumerator Start()
        {
            while (_transition.IsBusy)
                yield return null;

            Select(_playButton.gameObject);
        }

        public void Play()
        {
            _transition.LoadScene(_storyScene);
        }

        public void Quit()
        {
            _transition.Quit();
        }

        public void OpenSettings()
        {
            if (_transition.IsBusy || !_settingsUI.Refresh())
                return;

            _mainPanel.SetActive(false);
            _settingsPanel.SetActive(true);
            Select(_settingsUI.FirstSelected);
        }

        public void ApplySettings()
        {
            if (!_transition.IsBusy)
                _settingsUI.Apply();
        }

        public void CloseSettings()
        {
            if (_transition.IsBusy)
                return;

            _settingsUI.CancelPreview();
            _settingsUI.CloseDropdown();

            _settingsPanel.SetActive(false);
            _mainPanel.SetActive(true);
            Select(_settingsButton.gameObject);
        }

        private static void Select(GameObject target)
        {
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(target);
        }

        private void OnEnable()
        {
            _cancelAction.Enable();
        }

        private void Update()
        {
            if (!Application.isFocused ||
                _transition.IsBusy ||
                !_settingsPanel.activeInHierarchy ||
                !_cancelAction.WasPressedThisFrame())
            {
                return;
            }

            if (_settingsUI.IsDropdownOpen)
                _settingsUI.CloseDropdown();
            else
                CloseSettings();
        }

        private void OnDisable()
        {
            _cancelAction?.Disable();
        }

        private void OnDestroy()
        {
            _cancelAction?.Dispose();
        }
    }
}
