using FinalVFX.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace FinalVFX.UI
{
    [RequireComponent(typeof(Text))]
    public class ConsumableCounter : MonoBehaviour
    {
        [SerializeField] private HealingConsumable _consumable;
        private Text _text;

        private void Awake() => _text = GetComponent<Text>();

        private void OnEnable()
        {
            _consumable.UsesChanged += Refresh;
            Refresh(_consumable.RemainingUses, _consumable.MaximumUses);
        }

        private void Start() => Refresh(_consumable.RemainingUses, _consumable.MaximumUses);

        private void OnDisable()
        {
            _consumable.UsesChanged -= Refresh;
        }

        private void Refresh(int remaining, int maximum) => _text.text = remaining.ToString();
    }
}
