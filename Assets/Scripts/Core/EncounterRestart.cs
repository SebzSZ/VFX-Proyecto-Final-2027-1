using FinalVFX.Combat;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FinalVFX.Core
{
    public class EncounterRestart : MonoBehaviour
    {
        [SerializeField] private HealthComponent _playerHealth;
        [SerializeField, Min(0f)] private float _restartDelay = 2f;

        private Coroutine _restart;

        private void OnEnable()
        {
            _playerHealth.Died += BeginRestart;
        }

        private void OnDisable()
        {
            _playerHealth.Died -= BeginRestart;
            if (_restart != null)
            {
                StopCoroutine(_restart);
                _restart = null;
            }
        }

        public void BeginRestart()
        {
            if (_restart != null)
                return;

            _restart = StartCoroutine(Restart());
        }

        private IEnumerator Restart()
        {
            yield return new WaitForSecondsRealtime(_restartDelay);
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync(gameObject.scene.path, LoadSceneMode.Single);
        }
    }
}
