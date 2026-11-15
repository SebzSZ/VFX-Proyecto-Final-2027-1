using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace FinalVFX.Menu
{
    public sealed class GameSettings : MonoBehaviour
    {
        public static GameSettings Instance { get; private set; }

        [SerializeField] private AudioMixer _mixer;

        private const string Prefix = "FinalVFX.Settings.";

        private readonly List<Vector2Int> _resolutions = new();

        public IReadOnlyList<Vector2Int> Resolutions => _resolutions;
        public Vector2Int Resolution { get; private set; }
        public bool Fullscreen { get; private set; }
        public bool VSync { get; private set; }
        public float MasterVolume { get; private set; }
        public float MusicVolume { get; private set; }
        public float SfxVolume { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            BuildResolutionList();
            LoadPreferences();
        }

        private void Start()
        {
            ApplyRuntimeSettings();
        }

        private void BuildResolutionList()
        {
            foreach (var mode in Screen.resolutions)
            {
                var size = new Vector2Int(mode.width, mode.height);

                if (!_resolutions.Contains(size))
                    _resolutions.Add(size);
            }

            if (_resolutions.Count == 0)
            {
                _resolutions.Add(new Vector2Int(
                    Mathf.Max(1, Screen.width),
                    Mathf.Max(1, Screen.height)));
            }

            _resolutions.Sort((a, b) =>
            {
                int comparison = a.x.CompareTo(b.x);
                return comparison != 0 ? comparison : a.y.CompareTo(b.y);
            });
        }

        private void LoadPreferences()
        {
            var desktop = new Vector2Int(
                Screen.currentResolution.width,
                Screen.currentResolution.height);

            var fallback = _resolutions.Contains(desktop)
                ? desktop
                : _resolutions[_resolutions.Count - 1];

            var saved = new Vector2Int(
                PlayerPrefs.GetInt(Prefix + "Width", fallback.x),
                PlayerPrefs.GetInt(Prefix + "Height", fallback.y));

            Resolution = _resolutions.Contains(saved) ? saved : fallback;

            Fullscreen = PlayerPrefs.GetInt(
                Prefix + "Fullscreen", Screen.fullScreen ? 1 : 0) == 1;

            VSync = PlayerPrefs.GetInt(
                Prefix + "VSync", QualitySettings.vSyncCount > 0 ? 1 : 0) == 1;

            MasterVolume = ReadVolume("Master", 1f);
            MusicVolume = ReadVolume("Music", 0.8f);
            SfxVolume = ReadVolume("Sfx", 1f);
        }

        private static float ReadVolume(string key, float fallback)
        {
            return Mathf.Clamp01(
                PlayerPrefs.GetFloat(Prefix + key, fallback));
        }

        public void Apply(
            int resolutionIndex,
            bool fullscreen,
            bool vSync,
            float master,
            float music,
            float sfx)
        {
            resolutionIndex = Mathf.Clamp(
                resolutionIndex, 0, _resolutions.Count - 1);

            Resolution = _resolutions[resolutionIndex];
            Fullscreen = fullscreen;
            VSync = vSync;
            MasterVolume = Mathf.Clamp01(master);
            MusicVolume = Mathf.Clamp01(music);
            SfxVolume = Mathf.Clamp01(sfx);

            ApplyRuntimeSettings();
            SavePreferences();
        }

        private void ApplyRuntimeSettings()
        {
            QualitySettings.vSyncCount = VSync ? 1 : 0;

            Screen.SetResolution(
                Resolution.x,
                Resolution.y,
                Fullscreen
                    ? FullScreenMode.FullScreenWindow
                    : FullScreenMode.Windowed);

            SetMixerVolume("MasterVolume", MasterVolume);
            SetMixerVolume("MusicVolume", MusicVolume);
            SetMixerVolume("SfxVolume", SfxVolume);
        }

        private void SetMixerVolume(string parameter, float value)
        {
            if (_mixer == null)
            {
                Debug.LogError("GameSettings: asigna el Audio Mixer.", this);
                return;
            }

            float decibels = value <= 0.0001f
                ? -80f
                : 20f * Mathf.Log10(value);

            if (!_mixer.SetFloat(parameter, decibels))
            {
                Debug.LogError(
                    $"GameSettings: falta exponer '{parameter}' en el mixer.",
                    this);
            }
        }

        private void SavePreferences()
        {
            PlayerPrefs.SetInt(Prefix + "Width", Resolution.x);
            PlayerPrefs.SetInt(Prefix + "Height", Resolution.y);
            PlayerPrefs.SetInt(Prefix + "Fullscreen", Fullscreen ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "VSync", VSync ? 1 : 0);
            PlayerPrefs.SetFloat(Prefix + "Master", MasterVolume);
            PlayerPrefs.SetFloat(Prefix + "Music", MusicVolume);
            PlayerPrefs.SetFloat(Prefix + "Sfx", SfxVolume);
            PlayerPrefs.Save();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}