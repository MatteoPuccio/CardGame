using System.Collections.Generic;
using UnityEngine;

using Assets.Scripts.CardEngine.Cards;



namespace Assets.Scripts.CardEngine.Board
{
    public class PlayAreaController : MonoBehaviour
    {
        [SerializeField] private PlayArea _playArea;
        private CardFactory _cardFactory;

        private bool _initialized;
    
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
                throw new System.InvalidOperationException("PlayAreaController: _playArea is not assigned and could not be found in children.");

            if (_cardFactory == null)
                throw new System.InvalidOperationException("PlayAreaController: _cardFactory is not assigned and could not be obtained from GameController.");

            _initialized = true;
        }

        public void InitializeZones()
        {
            if (!_initialized)
                throw new System.InvalidOperationException("PlayAreaController: InitializeZones called before Initialize().");

            foreach (var zone in _playArea.Zones)
            {
                var zoneGO = Instantiate(_playArea.zonePrefab, _playArea.transform);
                var view = zoneGO.GetComponent<PlayAreaZoneView>();
                if (view == null)
                    view = zoneGO.AddComponent<PlayAreaZoneView>();
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