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

        public GameObject FirstSelected => _resolutionDropdown.gameObject;

        public bool Refresh()
        {
            var settings = GameSettings.Instance;

            if (settings == null)
            {
                Debug.LogError(
                    "SettingsPanelUI: falta GameSettings en la escena.",
                    this);
                return false;
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

            return true;
        }

        public void Apply()
        {
            if (GameSettings.Instance == null)
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

        public void CloseDropdown()
        {
            _resolutionDropdown.Hide();
        }
    }
}