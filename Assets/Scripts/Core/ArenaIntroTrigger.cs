using UnityEngine;
namespace FinalVFX.Core
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class ArenaIntroTrigger : MonoBehaviour
    {
        [SerializeField] private ArenaIntroDirector _director;
        private void Reset() => GetComponent<BoxCollider>().isTrigger = true;
        private void OnTriggerEnter(Collider other) { if (_director) _director.TryEnter(other); }
        private void OnTriggerStay(Collider other) { if (_director) _director.TryEnter(other); }
    }
}
