using System.Collections;
using FinalVFX.Audio;
using FinalVFX.Boss;
using FinalVFX.Menu;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FinalVFX.Core
{
    public sealed class BossVictorySequence : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GuardianHealth _bossHealth;
        [SerializeField] private ArenaMusic _music;
        [SerializeField] private PauseMenu _pauseMenu;
        [SerializeField]
        private FinalVFX.Player.PlayerInputManager _playerInput;
        [SerializeField] private CanvasGroup _blackScreen;

        [Header("Transition")]
        [SerializeField] private float _fadeSeconds;
        [SerializeField] private float _blackHoldSeconds;
        [SerializeField] private string _endScene = "SCN_End";

        private bool _ending;
        private bool _ownsFreeze;
        private bool _previousAudioPause;

        private void Awake()
        {
            if (!_bossHealth || !_music || !_blackScreen)
            {
                enabled = false;
                return;
            }

            _blackScreen.alpha = 0f;
            _blackScreen.blocksRaycasts = false;
            _blackScreen.interactable = false;
        }

        private void OnEnable()
        {
            if (_bossHealth)
                _bossHealth.Died += BeginEnding;
        }

        private void OnDisable()
        {
            if (_bossHealth)
                _bossHealth.Died -= BeginEnding;
        }

        private void BeginEnding()
        {
            if (_ending)
                return;

            if (!Application.CanStreamedLevelBeLoaded(_endScene))
                return;

            _ending = true;

            if (_pauseMenu)
                _pauseMenu.enabled = false;

            if (_playerInput)
                _playerInput.enabled = false;

            _music.StopAll();

            _previousAudioPause = AudioListener.pause;
            AudioListener.pause = true;

            Time.timeScale = 0f;
            _ownsFreeze = true;

            _blackScreen.blocksRaycasts = true;

            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;

            StartCoroutine(EndingRoutine());
        }

        private IEnumerator EndingRoutine()
        {
            float elapsed = 0f;

            while (elapsed < _fadeSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                _blackScreen.alpha = Mathf.Clamp01(elapsed / _fadeSeconds);
                yield return null;
            }

            _blackScreen.alpha = 1f;

            yield return null;

            if (_blackHoldSeconds > 0f)
                yield return new WaitForSecondsRealtime(_blackHoldSeconds);

            yield return SceneManager.LoadSceneAsync(
                _endScene, LoadSceneMode.Single);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != _endScene)
                return;

            RestoreGlobals();

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            Destroy(gameObject);
        }

        private void RestoreGlobals()
        {
            if (!_ownsFreeze)
                return;

            _ownsFreeze = false;
            Time.timeScale = 1f;
            AudioListener.pause = _previousAudioPause;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            RestoreGlobals();
        }
    }
}