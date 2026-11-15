using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;

namespace FinalVFX.Menu
{
    public sealed class PausePresentation : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private AudioMixer _mixer;
        [SerializeField] private Volume _blurVolume;

        [Header("Transition")]
        [SerializeField, Min(0.01f)]
        private float _transitionSeconds = 0.25f;

        [Header("Paused audio")]
        [SerializeField, Range(-30f, 0f)]
        private float _pausedVolumeDb = -6f;

        [SerializeField, Range(100f, 22000f)]
        private float _pausedCutoffHz = 1400f;

        private float _amount;
        private float _target;
        private bool _initialized;
        private bool _audioReady;

        private void Awake()
        {
            if (_blurVolume != null)
                _blurVolume.weight = 0f;
        }

        private void Start()
        {
            _audioReady =
                _mixer != null &&
                _mixer.GetFloat("PauseVolume", out _) &&
                _mixer.GetFloat("PauseCutoff", out _);

            if (!_audioReady)
            {
                Debug.LogError(
                    "PausePresentation: asigna MIX_Game y expón " +
                    "PauseVolume y PauseCutoff.",
                    this);
            }

            _initialized = true;
            Apply();
        }

        public void SetPaused(bool paused)
        {
            _target = paused ? 1f : 0f;
        }

        private void Update()
        {
            if (!_initialized || Mathf.Approximately(_amount, _target))
                return;

            _amount = Mathf.MoveTowards(
                _amount,
                _target,
                Time.unscaledDeltaTime /
                Mathf.Max(0.01f, _transitionSeconds));

            Apply();
        }

        private void Apply()
        {
            float blend = Mathf.SmoothStep(0f, 1f, _amount);

            if (_blurVolume != null)
                _blurVolume.weight = blend;

            if (!_audioReady)
                return;

            _mixer.SetFloat(
                "PauseVolume",
                Mathf.Lerp(0f, _pausedVolumeDb, blend));

            // Interpolación logarítmica para un cambio de frecuencia suave.
            float cutoff = Mathf.Exp(
                Mathf.Lerp(
                    Mathf.Log(22000f),
                    Mathf.Log(Mathf.Clamp(_pausedCutoffHz, 100f, 22000f)),
                    blend));

            _mixer.SetFloat("PauseCutoff", cutoff);
        }

        public void ResetImmediately()
        {
            _target = 0f;
            _amount = 0f;

            if (_blurVolume != null)
                _blurVolume.weight = 0f;

            if (_initialized)
                Apply();
        }

        private void OnDisable()
        {
            ResetImmediately();
        }
    }
}