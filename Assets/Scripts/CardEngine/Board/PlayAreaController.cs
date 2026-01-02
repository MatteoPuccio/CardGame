using System.Collections.Generic;
using UnityEngine;

using Assets.Scripts.CardEngine.Cards;



namespace Assets.Scripts.CardEngine.Board
{
    public class PlayAreaController : MonoBehaviour
    {
        [SerializeField] private PlayArea _playArea;
        [SerializeField] private CardFactory _cardFactory;
    
        private readonly Dictionary<PlayAreaZone, PlayAreaZoneView> zoneViews = new();
        private readonly Dictionary<PlayAreaZone, CardView> cardViews = new();

        public PlayArea PlayArea => _playArea;
        public GameController GameController { get; set; }

        public void Initialize(GameController gameController)
        {
            GameController = gameController;

            if (_playArea == null)
                _playArea = GetComponentInChildren<PlayArea>(includeInactive: true);

            if (_cardFactory == null && gameController != null)
                _cardFactory = gameController.CardFactory;

            if (_playArea == null)
                Debug.LogError("PlayAreaController: _playArea is not assigned and could not be found in children.");

            if (_cardFactory == null)
                Debug.LogError("PlayAreaController: _cardFactory is not assigned and could not be obtained from GameController.");
        }

        public void InitializeZones()
        {
            if (_playArea == null || _cardFactory == null)
            {
                Debug.LogError("PlayAreaController: InitializeZones called before Initialize() or without required references.");
                return;
            }

            foreach (var zone in _playArea.Zones)
            {
                var zoneGO = Instantiate(_playArea.zonePrefab, _playArea.transform);
                var view = zoneGO.GetComponent<PlayAreaZoneView>();
                view.ZoneIndex = zone.ZoneIndex;
                zoneViews[zone] = view;
    
                zone.OnCardAssigned += card => SpawnCardInZone(zone, card);
                zone.OnCardRemoved += () => RemoveCardFromZone(zone);
            }
        }
    
        private void SpawnCardInZone(PlayAreaZone zone, Card card)
        {
            if (card == null)
                return;

            CardView cardView = null;
            if (GameController?.CardViewRegistry != null)
                GameController.CardViewRegistry.TryGet(card, out cardView);

            if (cardView == null)
                cardView = _cardFactory.CreateCard(card, card.GameState, GameController?.CardViewRegistry);

            if (cardView == null)
                return;

            // Ensure this view is discoverable for reuse by other controllers.
            if (GameController?.CardViewRegistry != null && cardView.CardData != null)
                GameController.CardViewRegistry.Register(cardView.CardData, cardView);

            if (zoneViews.TryGetValue(zone, out var zoneView) && zoneView != null)
            {
                cardView.transform.SetParent(zoneView.CardContainer ?? zoneView.transform, true);
                cardView.transform.localPosition = Vector3.zero;
                cardView.SetState(new CardInPlayState(zoneView));
                cardViews[zone] = cardView;
            }
        }
    
        private void RemoveCardFromZone(PlayAreaZone zone)
        {
            cardViews.Remove(zone);
        }
    }


}