using UnityEngine;
using System.Collections;

namespace Assets.Scripts.CardEngine.Board
{
    /// <summary>
    /// MonoBehaviour view for a zone: handles transforms and card parenting.
    /// </summary>
    public class PlayAreaZoneView : MonoBehaviour
    {
        public int ZoneIndex { get; set; }
        public Transform CardContainer; // assign in prefab
    }
}