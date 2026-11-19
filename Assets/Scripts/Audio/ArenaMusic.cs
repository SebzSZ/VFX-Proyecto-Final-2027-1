using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

namespace FinalVFX.Audio
{
    public sealed class ArenaMusic : MonoBehaviour
    {
        [SerializeField] private AudioClip _lobbyTheme, _bossIntro, _bossTheme;
        [SerializeField] private AudioMixerGroup _musicGroup;
        [SerializeField] private float _scheduleLead;
        public const double IntroSeconds = 20.0;
        private AudioSource _lobby, _intro, _boss;
        private double _lobbyStart;
        public bool Ready { get; private set; }
        public bool Committed { get; private set; }
        public double IntroStart { get; private set; }
        public double BossStart { get; private set; }
        public bool Configured => _lobbyTheme && _bossIntro && _bossTheme && _musicGroup;
        private static double Duration(AudioClip clip) => (double)clip.samples / clip.frequency;

        private void Start()
        {
            foreach (var clip in new[] { _lobbyTheme, _bossIntro, _bossTheme })
            {
                clip.LoadAudioData();
                float deadline = Time.realtimeSinceStartup + 30;
            }

            if (Duration(_bossIntro) < IntroSeconds - 1.0 / _bossIntro.frequency)
            {
                return;
            }

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

        public void CommitIntro(double requestedBoundary)
        {
            double duration = Duration(_lobbyTheme);
            double earliest = AudioSettings.dspTime + _scheduleLead;
            IntroStart = requestedBoundary;
            if (IntroStart < earliest)
            {
                IntroStart += Math.Ceiling((earliest - IntroStart) / duration) * duration;
            }
            BossStart = IntroStart + IntroSeconds;
            _lobby.SetScheduledEndTime(IntroStart);
            _intro.PlayScheduled(IntroStart);
            _intro.SetScheduledEndTime(BossStart);
            _boss.PlayScheduled(BossStart);
            Committed = true;
        }

        public double SkipToBoss(float fadeSeconds)
        {
            double now = AudioSettings.dspTime;
            if (BossStart - now <= Math.Max(_scheduleLead, fadeSeconds)) return BossStart;
            BossStart = now + Math.Max(_scheduleLead, fadeSeconds);
            _boss.Stop();
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
    }
}
