using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

namespace FinalVFX.Audio
{
    [DisallowMultipleComponent]
    public sealed class ArenaMusic : MonoBehaviour
    {
        [SerializeField] private AudioClip _lobbyTheme, _bossIntro, _bossTheme;
        [SerializeField] private AudioMixerGroup _musicGroup;
        [SerializeField, Min(0.05f)] private float _scheduleLead = 0.15f;
        public const double IntroSeconds = 20.0;
        private AudioSource _lobby, _intro, _boss;
        private double _lobbyStart;
        public bool Ready { get; private set; }
        public bool Committed { get; private set; }
        public double IntroStart { get; private set; }
        public double BossStart { get; private set; }
        public bool Configured => _lobbyTheme && _bossIntro && _bossTheme && _musicGroup;
        private static double Duration(AudioClip clip) => (double)clip.samples / clip.frequency;

        private IEnumerator Start()
        {
            if (!Configured) { Debug.LogError("ArenaMusic: asigna los tres clips y el grupo Music.", this); yield break; }
            foreach (var clip in new[] { _lobbyTheme, _bossIntro, _bossTheme })
            {
                clip.LoadAudioData();
                float deadline = Time.realtimeSinceStartup + 30;
                while (clip.loadState != AudioDataLoadState.Loaded)
                {
                    if (clip.loadState == AudioDataLoadState.Failed || Time.realtimeSinceStartup > deadline)
                    { Debug.LogError("No se pudo cargar " + clip.name, this); yield break; }
                    yield return null;
                }
            }
            // A shorter clip would produce silence before BossTheme. Do not hide it.
            if (Duration(_bossIntro) < IntroSeconds - 1.0 / _bossIntro.frequency)
            { Debug.LogError("BossIntro debe tener al menos 20 segundos de audio. Revisa el recurso importado.", this); yield break; }
            if (Math.Abs(Duration(_bossIntro) - IntroSeconds) > 0.002)
                Debug.LogWarning("BossIntro se cortará exactamente a los 20 s. Para continuidad musical, exporta un corte de 20 s.", this);
            _lobby = CreateSource(_lobbyTheme, true);
            _intro = CreateSource(_bossIntro, false);
            _boss = CreateSource(_bossTheme, true);
            _lobbyStart = AudioSettings.dspTime + _scheduleLead;
            _lobby.PlayScheduled(_lobbyStart);
            Ready = true;
        }

        private AudioSource CreateSource(AudioClip clip, bool loop)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.clip = clip;
            source.outputAudioMixerGroup = _musicGroup;
            source.loop = loop;
            source.spatialBlend = 0;
            source.pitch = 1;
            source.volume = 1;
            source.dopplerLevel = 0;
            source.priority = 0;
            return source;
        }

        public double CurrentLoopEnd()
        {
            double duration = Duration(_lobbyTheme);
            double cycle = Math.Max(0, Math.Floor((AudioSettings.dspTime - _lobbyStart) / duration));
            return _lobbyStart + (cycle + 1) * duration;
        }

        // Call once, after the first camera is ready and the screen is black.
        public void CommitIntro(double requestedBoundary)
        {
            if (!Ready || Committed) throw new InvalidOperationException("ArenaMusic no está disponible.");
            double duration = Duration(_lobbyTheme);
            double earliest = AudioSettings.dspTime + _scheduleLead;
            IntroStart = requestedBoundary;
            if (IntroStart < earliest)
            {
                IntroStart += Math.Ceiling((earliest - IntroStart) / duration) * duration;
                Debug.LogWarning("Entrada demasiado cerca del final del loop: se espera el siguiente final para preparar audio y cámara sin cortar la música.", this);
            }
            BossStart = IntroStart + IntroSeconds;
            _lobby.SetScheduledEndTime(IntroStart);
            _intro.PlayScheduled(IntroStart);
            _intro.SetScheduledEndTime(BossStart);
            _boss.PlayScheduled(BossStart);
            Committed = true;
            Debug.Log($"ArenaMusic DSP: intro={IntroStart:F6}, boss={BossStart:F6}", this);
        }

        public double SkipToBoss(float fadeSeconds)
        {
            double now = AudioSettings.dspTime;
            if (BossStart - now <= Math.Max(_scheduleLead, fadeSeconds)) return BossStart;
            BossStart = now + Math.Max(_scheduleLead, fadeSeconds);
            _boss.Stop(); // Cancel the previously scheduled start.
            _intro.SetScheduledEndTime(BossStart);
            _boss.PlayScheduled(BossStart);
            return BossStart;
        }

        public void StopAll()
        {
            Ready = false;
            if (_lobby) _lobby.Stop();
            if (_intro) _intro.Stop();
            if (_boss) _boss.Stop();
        }
        private void OnDisable() => StopAll();

        [ContextMenu("Inspect clip durations")]
        private void InspectDurations()
        {
            foreach (var clip in new[] { _lobbyTheme, _bossIntro, _bossTheme })
                if (clip) Debug.Log($"{clip.name}: {Duration(clip):F6} s, {clip.samples} samples, {clip.frequency} Hz", this);
        }
    }
}
