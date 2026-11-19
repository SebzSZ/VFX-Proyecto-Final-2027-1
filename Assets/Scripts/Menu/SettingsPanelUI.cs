using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FinalVFX.Menu
{
    public sealed class SettingsPanelUI : MonoBehaviour
    {
        [SerializeField] private TMP_Dropdown _resolutionDropdown;
        [SerializeField] private Toggle _fullscreenToggle;
        [SerializeField] private Toggle _vSyncToggle;
        [SerializeField] private Slider _masterSlider;
        [SerializeField] private Slider _musicSlider;
        [SerializeField] private Slider _sfxSlider;
        [SerializeField] private TMP_Text _statusText;
        [SerializeField, Min(0.1f)] private float _statusDuration = 3f;

        private bool _editing;
        private bool _listenersAdded;
        private float _statusUntil;

        public GameObject FirstSelected => _resolutionDropdown.gameObject;
        public bool IsDropdownOpen => _resolutionDropdown.IsExpanded;

        public bool Refresh()
        {
            CancelPreview();
            var settings = GameSettings.Instance;

            if (settings == null)
            {
                return false;
            }

            EnsureListeners();

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

            ClearStatus();
            _editing = true;
            return true;
        }

        public void Apply()
        {
            if (!_editing || GameSettings.Instance == null)
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
            _statusUntil = Time.unscaledTime + _statusDuration;
        }

        private void EnsureListeners()
        {
            if (_listenersAdded) return;
            _resolutionDropdown.onValueChanged.AddListener(PreviewIndex);
            _fullscreenToggle.onValueChanged.AddListener(PreviewToggle);
            _vSyncToggle.onValueChanged.AddListener(PreviewToggle);
            _masterSlider.onValueChanged.AddListener(PreviewVolume);
            _musicSlider.onValueChanged.AddListener(PreviewVolume);
            _sfxSlider.onValueChanged.AddListener(PreviewVolume);
            _listenersAdded = true;
        }

        private void PreviewIndex(int value) => Preview();
        private void PreviewToggle(bool value) => Preview();
        private void PreviewVolume(float value) => Preview();

        private void Preview()
        {
            if (!_editing || GameSettings.Instance == null) return;
            ClearStatus();
            GameSettings.Instance.Preview(
                _resolutionDropdown.value, _fullscreenToggle.isOn, _vSyncToggle.isOn,
                _masterSlider.value, _musicSlider.value, _sfxSlider.value);
        }

        public void CancelPreview()
        {
            if (_editing && GameSettings.Instance != null)
                GameSettings.Instance.RestoreSavedSettings();
            _editing = false;
            ClearStatus();
        }

        private void ClearStatus()
        {
            _statusUntil = 0f;
            if (_statusText != null) _statusText.text = "";
        }

        private void Update()
        {
            if (_statusUntil > 0f && Time.unscaledTime >= _statusUntil)
                ClearStatus();
        }

        private void OnDisable() => CancelPreview();

        private void OnDestroy()
        {
            if (!_listenersAdded) return;
            if (_resolutionDropdown != null) _resolutionDropdown.onValueChanged.RemoveListener(PreviewIndex);
            if (_fullscreenToggle != null) _fullscreenToggle.onValueChanged.RemoveListener(PreviewToggle);
            if (_vSyncToggle != null) _vSyncToggle.onValueChanged.RemoveListener(PreviewToggle);
            if (_masterSlider != null) _masterSlider.onValueChanged.RemoveListener(PreviewVolume);
            if (_musicSlider != null) _musicSlider.onValueChanged.RemoveListener(PreviewVolume);
            if (_sfxSlider != null) _sfxSlider.onValueChanged.RemoveListener(PreviewVolume);
        }

        public void CloseDropdown()
        {
            _resolutionDropdown.Hide();
        }
    }
}
