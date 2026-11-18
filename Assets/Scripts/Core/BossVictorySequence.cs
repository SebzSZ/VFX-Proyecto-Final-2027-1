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
        [SerializeField, Min(0f)] private float _fadeSeconds = 0.6f;
        [SerializeField, Min(0f)] private float _blackHoldSeconds = 0.4f;
        [SerializeField] private string _endScene = "SCN_End";

        private bool _ending;
        private bool _ownsFreeze;
        private bool _previousAudioPause;

        private void Awake()
        {
            if (!_bossHealth || !_music || !_blackScreen)
            {
                Debug.LogError(
                    "BossVictorySequence: asigna Boss Health, Music y Black Screen.",
                    this);
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
            {
                Debug.LogError(
                    $"Agrega '{_endScene}' a la Scene List del build.",
                    this);
                return;
            }

            _ending = true;

            // Desactivar PauseMenu primero: su OnDisable puede restaurar tiempo.
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

            // Este objeto debe estar en la raíz y contener solo este script.
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

            // Garantiza un frame negro incluso con duración de fundido cero.
            yield return null;

            if (_blackHoldSeconds > 0f)
                yield return new WaitForSecondsRealtime(_blackHoldSeconds);

            // La arena permanece congelada durante toda la carga.
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