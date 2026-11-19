using UnityEngine;

namespace FinalVFX.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class SceneMusic : MonoBehaviour
    {
        [SerializeField] private float _fadeInSeconds;
        private AudioSource _source;
        private float _elapsed, _duration, _from, _to;
        private bool _fading, _entering;
        public bool Ready => _source && _source.clip && _source.clip.loadState == AudioDataLoadState.Loaded;
        public bool Failed => !_source || !_source.clip || _source.clip.loadState == AudioDataLoadState.Failed;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _source.Stop();
            _source.playOnAwake = false;
            _source.spatialBlend = 0;
            _source.pitch = 1;
            _source.volume = 0;
            if (_source.clip) _source.clip.LoadAudioData();
        }

        public void Begin()
        {
            _source.volume = 0;
            _source.Play();
            StartFade(1, _fadeInSeconds, true);
        }

        public void FadeOut(float seconds) => StartFade(0, seconds, false);

        private void StartFade(float target, float seconds, bool entering)
        {
            _from = _source.volume;
            _to = target;
            _elapsed = 0;
            _duration = Mathf.Max(0.01f, seconds);
            _entering = entering;
            _fading = true;
        }

        private void Update()
        {
            if (!_fading) return;
            _elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(_elapsed / _duration);
            float curve = Mathf.SmoothStep(0, 1, t);
            if (_entering) curve *= curve;
            _source.volume = Mathf.Lerp(_from, _to, curve);
            if (t >= 1) _fading = false;
        }

        public void SetGain(float gain)
        {
            _fading = false;
            _source.volume = Mathf.Clamp01(gain);
        }

        public void Stop()
        {
            _fading = false;
            if (_source) { _source.volume = 0; _source.Stop(); }
        }

        private void OnDisable() => Stop();
    }
}
