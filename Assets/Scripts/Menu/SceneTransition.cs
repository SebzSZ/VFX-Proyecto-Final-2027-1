using System.Collections;
using FinalVFX.Audio;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FinalVFX.Menu
{
    public sealed class SceneTransition : MonoBehaviour
    {
        [SerializeField] private SceneMusic _music;
        [Header("Fade")]
        [SerializeField] private CanvasGroup _blackScreen;
        [SerializeField] private CanvasGroup _content;
        [SerializeField, Min(0.01f)] private float _fadeSeconds = 0.6f;

        [Header("Optional automatic advance")]
        [SerializeField] private bool _advanceAutomatically;
        [SerializeField, Min(0f)] private float _holdSeconds = 10f;
        [SerializeField] private string _nextScene = "SCN_Arena";

        public bool IsBusy { get; private set; }

        private void Awake()
        {
            IsBusy = true;
            _blackScreen.alpha = 1f;
            _blackScreen.blocksRaycasts = true;

            if (_content != null)
                _content.interactable = false;
        }

        private IEnumerator Start()
        {
            if (_music != null)
            {
                float deadline = Time.realtimeSinceStartup + 30;
                while (!_music.Ready && !_music.Failed && Time.realtimeSinceStartup < deadline)
                    yield return null;
                if (!_music.Ready)
                {
                    Debug.LogError("SceneTransition: no se pudo cargar la música.", this);
                    _music = null;
                }
                else _music.Begin();
            }
            yield return FadeTo(0f);
            SetBusy(false);

            if (_advanceAutomatically)
            {
                yield return new WaitForSecondsRealtime(_holdSeconds);
                LoadScene(_nextScene);
            }
        }

        public void LoadScene(string sceneName)
        {
            if (IsBusy)
                return;

            if (string.IsNullOrWhiteSpace(sceneName) ||
                !Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError(
                    $"SceneTransition: agrega '{sceneName}' a Scene List.",
                    this);
                return;
            }

            SetBusy(true);
            StartCoroutine(LoadRoutine(sceneName));
        }

        public void Quit()
        {
            if (IsBusy)
                return;

            SetBusy(true);
            StartCoroutine(QuitRoutine());
        }

        private IEnumerator LoadRoutine(string sceneName)
        {
            if (_music != null) _music.FadeOut(_fadeSeconds);
            yield return FadeTo(1f);
            if (_music != null) _music.Stop();

            yield return null;

            Time.timeScale = 1f;

            yield return SceneManager.LoadSceneAsync(
                sceneName, LoadSceneMode.Single);
        }

        private IEnumerator QuitRoutine()
        {
            if (_music != null) _music.FadeOut(_fadeSeconds);
            yield return FadeTo(1f);
            if (_music != null) _music.Stop();
            yield return new WaitForSecondsRealtime(0.1f);

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private IEnumerator FadeTo(float target)
        {
            float initial = _blackScreen.alpha;
            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, _fadeSeconds);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;

                _blackScreen.alpha = Mathf.Lerp(
                    initial, target, Mathf.Clamp01(elapsed / duration));


                yield return null;
            }

            _blackScreen.alpha = target;

        }

        private void SetBusy(bool busy)
        {
            IsBusy = busy;
            _blackScreen.blocksRaycasts = busy;

            if (_content != null)
                _content.interactable = !busy;
        }
    }
}

