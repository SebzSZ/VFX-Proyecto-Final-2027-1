using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace FinalVFX.Audio
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class SoundEffectPool : MonoBehaviour
    {
        private static SoundEffectPool _instance;
        public static SoundEffectPool Instance
        {
            get
            {
                if (!_instance)
                    _instance = FindFirstObjectByType<SoundEffectPool>();
                if (!_instance)
                    _instance = new GameObject("SoundEffectPool").AddComponent<SoundEffectPool>();
                return _instance;
            }
        }

        [SerializeField, Min(1)] private int _initialSize = 5;
        [SerializeField] private AudioMixerGroup _outputGroup;
        [SerializeField, Range(0f, 1f)] private float _volume = 1f;

        private sealed class Voice
        {
            public AudioSource source;
            public ulong id;
            public bool paused;
        }

        // A handle identifies a playback, not a reusable AudioSource.
        public readonly struct Handle
        {
            private readonly SoundEffectPool _owner;
            private readonly ulong _id;
            internal Handle(SoundEffectPool owner, ulong id) { _owner = owner; _id = id; }
            public void Stop()
            {
                if (_owner) _owner.Stop(_id);
            }
        }

        private readonly List<Voice> _voices = new List<Voice>();
        private ulong _nextId;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _instance = null;

        private void Awake()
        {
            if (_instance && _instance != this)
            {
                Destroy(this);
                return;
            }
            _instance = this;
            for (int i = 0; i < Mathf.Max(1, _initialSize); i++) CreateVoice();
        }

        private Voice CreateVoice()
        {
            var child = new GameObject($"SFX_{_voices.Count + 1:00}");
            child.SetActive(false);
            child.transform.SetParent(transform, false);
            var source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.enabled = false;
            var voice = new Voice { source = source };
            _voices.Add(voice);
            return voice;
        }

        public Handle Play(AudioClip clip, bool loop = false)
        {
            if (!clip || !isActiveAndEnabled) return default;

            Voice voice = _voices.Find(item => item.id == 0);
            if (voice == null) voice = CreateVoice();
            voice.id = ++_nextId;
            voice.source.clip = clip;
            voice.source.loop = loop;
            voice.source.volume = _volume;
            voice.source.outputAudioMixerGroup = _outputGroup;
            voice.source.gameObject.SetActive(true);
            voice.source.enabled = true;
            voice.source.Play();
            voice.paused = Time.timeScale <= 0f;
            if (voice.paused) voice.source.Pause();
            return new Handle(this, voice.id);
        }

        private void Update()
        {
            foreach (Voice voice in _voices)
            {
                if (voice.id == 0) continue;
                bool paused = Time.timeScale <= 0f;
                if (paused != voice.paused)
                {
                    if (paused) voice.source.Pause();
                    else voice.source.UnPause();
                    voice.paused = paused;
                }
                // Paused sources must not be returned to the pool.
                if (!paused && !AudioListener.pause && !voice.source.isPlaying)
                    Release(voice);
            }
        }

        private void Stop(ulong id)
        {
            if (id == 0) return;
            foreach (Voice voice in _voices)
                if (voice.id == id) { Release(voice); return; }
        }

        private static void Release(Voice voice)
        {
            voice.source.Stop();
            voice.source.clip = null;
            voice.source.loop = false;
            voice.source.enabled = false;
            voice.source.gameObject.SetActive(false);
            voice.id = 0;
            voice.paused = false;
        }

        private void OnDisable()
        {
            foreach (Voice voice in _voices) Release(voice);
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
    }
}
