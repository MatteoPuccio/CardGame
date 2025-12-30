using System.Collections.Generic;
using UnityEngine;

using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Board;


namespace Assets.Scripts.CardEngine.Board
{
    public class PlayAreaController : MonoBehaviour
    {
        [SerializeField] private PlayArea playArea;
        [SerializeField] private CardFactory cardFactory;
    
        private Dictionary<PlayAreaZone, PlayAreaZoneView> zoneViews = new();
        private Dictionary<PlayAreaZone, CardView> cardViews = new();
    
        public void InitializeZones()
        {
            foreach (var zone in playArea.Zones)
            {
                var zoneGO = Instantiate(playArea.zonePrefab, playArea.transform);
                var view = zoneGO.GetComponent<PlayAreaZoneView>();
                view.ZoneIndex = zone.ZoneIndex;
                zoneViews[zone] = view;
    
                zone.OnCardAssigned += card => SpawnCardInZone(zone, card);
                zone.OnCardRemoved += () => RemoveCardFromZone(zone);
            }
        }
    
        private void SpawnCardInZone(PlayAreaZone zone, Card card)
        {
            var view = cardFactory.CreateCard(card);
            view.transform.SetParent(zoneViews[zone].CardContainer, false);
            view.transform.localPosition = Vector3.zero;
            cardViews[zone] = view;
        }
    
        private void RemoveCardFromZone(PlayAreaZone zone)
        {
            if (cardViews.TryGetValue(zone, out var view))
            {
                Destroy(view.gameObject);
                cardViews.Remove(zone);
            }
        }
    }


}