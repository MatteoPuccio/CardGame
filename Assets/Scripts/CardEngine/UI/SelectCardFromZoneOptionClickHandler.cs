using Assets.Scripts.CardEngine.Cards;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.Scripts.CardEngine.UI
{
    public sealed class SelectCardFromZoneOptionClickHandler : MonoBehaviour, IPointerClickHandler
    {
        private SelectCardFromZonePromptUI _ui;
        private Card _card;

        public Card Card => _card;

        public void Bind(SelectCardFromZonePromptUI ui, Card card)
        {
            _ui = ui;
            _card = card;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_ui == null || _card == null)
                return;

            _ui.Toggle(_card);
        }
    }
}
