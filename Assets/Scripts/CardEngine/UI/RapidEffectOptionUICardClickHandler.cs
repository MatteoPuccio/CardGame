using Assets.Scripts.CardEngine.Game;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.Scripts.CardEngine.UI
{
    public sealed class RapidEffectOptionUICardClickHandler : MonoBehaviour, IUICardClickHandler
    {
        private RapidEffectPromptUI _ui;
        private RapidEffectOption _option;

        public void Bind(RapidEffectPromptUI ui, RapidEffectOption option)
        {
            _ui = ui;
            _option = option;
        }

        public bool HandleClick(UICardView view, PointerEventData eventData)
        {
            if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
                return false;

            _ui?.SelectOption(_option);
            return true;
        }
    }
}
