using UnityEngine;
using System.Collections;

using Assets.Scripts.CardEngine.Cards;

namespace Assets.Scripts.CardEngine.Board
{
    /// <summary>
    /// MonoBehaviour view for a zone: handles transforms and card parenting.
    /// </summary>
    public class PlayAreaZoneView : MonoBehaviour
    {
        public int ZoneIndex { get; set; }
        public Transform CardContainer;
    
        public void AcceptCard(CardView cardView)
        {
            if (cardView == null || CardContainer == null)
                return;
    
            cardView.transform.SetParent(CardContainer, true);
            cardView.transform.localPosition = Vector3.up * 0.01f;
        }
    
    }
}