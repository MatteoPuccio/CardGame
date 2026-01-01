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

            // Reuse the existing view when moving a card into the zone.
            // Creating/destroying views here conflicts with Deck/Hand controllers that also manage the same CardView.
            var view = card.CardView != null ? card.CardView : _cardFactory.CreateCard(card, card.Owner, card.GameState);
            if (view == null)
                return;

            if (zoneViews.TryGetValue(zone, out var zoneView) && zoneView != null)
            {
                view.transform.SetParent(zoneView.CardContainer ?? zoneView.transform, true);
                view.transform.localPosition = Vector3.zero;
                view.SetState(new CardInPlayState(zoneView));
                cardViews[zone] = view;
            }
        }
    
        private void RemoveCardFromZone(PlayAreaZone zone)
        {
            cardViews.Remove(zone);
        }
    }


}