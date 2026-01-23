using Assets.Scripts.CardEngine.Game;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.Scripts.CardEngine.UI
{
    public sealed class OptionalEffectOptionUICardClickHandler : MonoBehaviour, IUICardClickHandler
    {
        private OptionalEffectPromptUI _ui;
        private OptionalEffectOption _option;

        public void Bind(OptionalEffectPromptUI ui, OptionalEffectOption option)
        {
            _ui = ui;
            _option = option;
        }

        public bool HandleClick(UICardView view, PointerEventData eventData)
        {
            if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
                return false;

            if (view != null && view.CardData != null)
                CardPreviewController.Show(view.CardData);

            _ui?.SelectOption(_option);
            return true;
        }
    }
}
