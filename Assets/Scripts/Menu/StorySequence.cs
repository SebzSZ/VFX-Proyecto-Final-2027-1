using System.Collections;
using TMPro;
using UnityEngine;

namespace FinalVFX.Menu
{
    public sealed class StorySequence : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TMP_Text _storyText;
        [SerializeField] private CanvasGroup _signature;
        [SerializeField] private SceneTransition _transition;

        [Header("Typewriter")]
        [SerializeField, Min(1f)]
        private float _charactersPerSecond = 35f;

        [SerializeField, Min(0f)]
        private float _initialDelay = 0.3f;

        [Header("Signature")]
        [SerializeField, Min(0f)]
        private float _delayBeforeSignature = 0.4f;

        [SerializeField, Min(0.01f)]
        private float _signatureFadeSeconds = 1.2f;

        [Header("Next scene")]
        [SerializeField, Min(0f)]
        private float _holdAfterSignature = 3f;

        [SerializeField]
        private string _nextScene = "SCN_Arena";

        private void Awake()
        {
            if (_storyText == null ||
                _signature == null ||
                _transition == null)
            {
                Debug.LogError(
                    "StorySequence: asigna Story Text, Signature y Transition.",
                    this);

                enabled = false;
                return;
            }

            _storyText.maxVisibleCharacters = 0;

            _storyText.useMaxVisibleDescender = false;

            _signature.alpha = 0f;
            _signature.interactable = false;
            _signature.blocksRaycasts = false;
        }

        private IEnumerator Start()
        {
            while (_transition.IsBusy)
                yield return null;

            if (_initialDelay > 0f)
                yield return new WaitForSecondsRealtime(_initialDelay);

            yield return RevealStory();

            if (_delayBeforeSignature > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    _delayBeforeSignature);
            }

            yield return RevealSignature();

            if (_holdAfterSignature > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    _holdAfterSignature);
            }

            _transition.LoadScene(_nextScene);
        }

        private IEnumerator RevealStory()
        {
            _storyText.ForceMeshUpdate();

            int totalCharacters = _storyText.textInfo.characterCount;
            float revealedCharacters = 0f;
            float speed = Mathf.Max(1f, _charactersPerSecond);

            while (revealedCharacters < totalCharacters)
            {
                revealedCharacters += speed * Time.unscaledDeltaTime;

                _storyText.maxVisibleCharacters = Mathf.Min(
                    Mathf.FloorToInt(revealedCharacters),
                    totalCharacters);

                yield return null;
            }

            _storyText.maxVisibleCharacters = totalCharacters;
        }

        private IEnumerator RevealSignature()
        {
            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, _signatureFadeSeconds);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;

                float progress = Mathf.Clamp01(elapsed / duration);

                _signature.alpha = Mathf.SmoothStep(0f, 1f, progress);

                yield return null;
            }

            _signature.alpha = 1f;
        }
    }
}