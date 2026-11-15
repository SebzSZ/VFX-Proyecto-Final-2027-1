using FinalVFX.Core;
using FinalVFX.Menu;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FinalVFX.Menu
{
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

        [Header("Settings")]
        [SerializeField] private TMP_Dropdown _resolutionDropdown;
        [SerializeField] private Toggle _fullscreenToggle;
        [SerializeField] private Toggle _vSyncToggle;
        [SerializeField] private Slider _masterSlider;
        [SerializeField] private Slider _musicSlider;
        [SerializeField] private Slider _sfxSlider;
        [SerializeField] private TMP_Text _statusText;

        private void Awake()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            _mainPanel.SetActive(true);
            _settingsPanel.SetActive(false);
        }

        private void Start()
        {
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
            if (_transition.IsBusy)
                return;

            var settings = GameSettings.Instance;

            if (settings == null)
            {
                Debug.LogError("MainMenu: falta GameSettings en la escena.", this);
                return;
            }

            var options = new List<string>();
            int selectedIndex = 0;

            for (int i = 0; i < settings.Resolutions.Count; i++)
            {
                var size = settings.Resolutions[i];
                options.Add($"{size.x} × {size.y}");

                if (size == settings.Resolution)
                    selectedIndex = i;
            }

            _resolutionDropdown.ClearOptions();
            _resolutionDropdown.AddOptions(options);
            _resolutionDropdown.SetValueWithoutNotify(selectedIndex);
            _resolutionDropdown.RefreshShownValue();

            _fullscreenToggle.SetIsOnWithoutNotify(settings.Fullscreen);
            _vSyncToggle.SetIsOnWithoutNotify(settings.VSync);
            _masterSlider.SetValueWithoutNotify(settings.MasterVolume);
            _musicSlider.SetValueWithoutNotify(settings.MusicVolume);
            _sfxSlider.SetValueWithoutNotify(settings.SfxVolume);

            if (_statusText != null)
                _statusText.text = "";

            _mainPanel.SetActive(false);
            _settingsPanel.SetActive(true);
            Select(_resolutionDropdown.gameObject);
        }

        public void ApplySettings()
        {
            if (_transition.IsBusy || GameSettings.Instance == null)
                return;

            GameSettings.Instance.Apply(
                _resolutionDropdown.value,
                _fullscreenToggle.isOn,
                _vSyncToggle.isOn,
                _masterSlider.value,
                _musicSlider.value,
                _sfxSlider.value);

            if (_statusText != null)
                _statusText.text = "Cambios aplicados y guardados.";
        }

        public void CloseSettings()
        {
            if (_transition.IsBusy)
                return;

            _settingsPanel.SetActive(false);
            _mainPanel.SetActive(true);
            Select(_settingsButton.gameObject);
        }

        private static void Select(GameObject target)
        {
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(target);
        }
    }
}