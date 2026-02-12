using TMPro;
using UnityEngine;

namespace Assets.Scripts.CardEngine.Cards.Views
{
    public sealed class CardFaceText3DView : MonoBehaviour
    {
        [SerializeField] private TextMeshPro _nameText;
        [SerializeField] private TextMeshPro _descriptionText;

        public void Set(Card card, bool hidden)
        {
            if (_nameText != null)
                _nameText.text = hidden ? string.Empty : (card?.Name ?? string.Empty);
            if (_descriptionText != null)
                _descriptionText.text = hidden ? string.Empty : (card?.EffectText ?? string.Empty);
        }
    }
}
