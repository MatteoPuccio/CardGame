using Assets.Scripts.CardEngine.Game;
using UnityEngine;
using Assets.Scripts.CardEngine.Board;
using System.Linq;

namespace Assets.Scripts.CardEngine.Cards
{
    public class CardView : MonoBehaviour
    {
        private Transform originalParent;
        private Transform handParent;

        private bool isDragging;
        private bool isPlayed;

        private Camera mainCamera;

        public Card CardData;

        public float dragHeightOffset = 0.5f;
        public float placeHeightOffset = 0.1f;
        public float zoneRaycastDistance = 2f;

        private PlayArea playArea;
        private PlayAreaZone occupiedZone;
        private PlayAreaZoneView occupiedZoneView;

        void Start()
        {
            mainCamera = Camera.main;
            handParent = transform.parent;

            // Find the correct play area by owner
            string playAreaTag = CardData.Owner != null && CardData.Owner.IsLocalPlayer
                ? PlayerArea.Local.ToString()
                : PlayerArea.Opponent.ToString();

            var playAreaGO = GameObject.FindGameObjectWithTag(playAreaTag);
            if (playAreaGO != null)
                playArea = playAreaGO.GetComponent<PlayArea>();
        }

        void OnMouseDown()
        {
            if (isPlayed)
            {
                occupiedZone?.Vacate();
                occupiedZone = null;

                if (occupiedZoneView != null)
                {
                    transform.SetParent(handParent, true);
                    transform.localPosition = Vector3.zero;
                    occupiedZoneView = null;
                }

                isPlayed = false;
                // Return the card to the hand model on click (no drag)
                var handClick = CardData?.Owner?.Hand;
                if (handClick != null && !handClick.Cards.Contains(CardData))
                {
                    handClick.AddCard(CardData);
                }
                return;
            }

            isDragging = true;
            originalParent = transform.parent;
            transform.SetParent(null, true);
        }

        void OnMouseDrag()
        {
            if (!isDragging) return;

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            Plane dragPlane = new Plane(Vector3.up, Vector3.up * handParent.position.y);

            if (dragPlane.Raycast(ray, out float distance))
            {
                Vector3 point = ray.GetPoint(distance);
                point.y += dragHeightOffset;
                transform.position = point;
            }
        }

        void OnMouseUp()
        {
            // Capture whether this was a drag interaction
            bool wasDragging = isDragging;
            isDragging = false;

            if (TryGetZoneUnderCard(out PlayAreaZone zone, out PlayAreaZoneView zoneView))
            {
                PlayCardToZone(zone, zoneView);
            }
            else if (wasDragging)
            {
                // Only add back to hand if we had been dragging from the hand
                transform.SetParent(handParent, true);
                transform.localPosition = Vector3.zero;
                var handUp = CardData?.Owner?.Hand;
                if (handUp != null && !handUp.Cards.Contains(CardData))
                {
                    handUp.AddCard(CardData);
                }
            }
        }

        private bool TryGetZoneUnderCard(out PlayAreaZone zone, out PlayAreaZoneView zoneView)
        {
            zone = null;
            zoneView = null;

            Ray ray = new Ray(transform.position, Vector3.down);
            if (Physics.Raycast(ray, out RaycastHit hit, zoneRaycastDistance))
            {
                zoneView = hit.collider.GetComponent<PlayAreaZoneView>();
                if (zoneView != null)
                {
                    // Find the model for this view
                    foreach (var kvp in playArea.ZoneViews)
                    {
                        if (kvp.Value == zoneView && !kvp.Key.IsOccupied)
                        {
                            zone = kvp.Key;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private void PlayCardToZone(PlayAreaZone zone, PlayAreaZoneView zoneView)
        {
            if (zone == null || zoneView == null) return;

            // Attempt to occupy first; if it fails, return to hand without duplicating
            if (zone.TryOccupy(CardData))
            {
                CardData?.PlayFromHand(zone);
                transform.SetParent(zoneView.CardContainer ?? zoneView.transform, true);
                transform.localPosition = Vector3.up * placeHeightOffset;
                occupiedZone = zone;
                occupiedZoneView = zoneView;
                isPlayed = true;
            }
            else
            {
                transform.SetParent(handParent, true);
                transform.localPosition = Vector3.zero;
                var hand = CardData?.Owner?.Hand;
                if (hand != null && !hand.Cards.Contains(CardData))
                {
                    hand.AddCard(CardData);
                }
            }
        }
    }
}
