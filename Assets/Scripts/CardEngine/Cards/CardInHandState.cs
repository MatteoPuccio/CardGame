using UnityEngine;
using Assets.Scripts.CardEngine.Board;
using Assets.Scripts.CardEngine.Game;

namespace Assets.Scripts.CardEngine.Cards
{
    public class CardInHandState : ICardInteractionState
    {
        private bool isDragging;
        private Vector3 dragPlaneOrigin;
        public HandView OwnerHandView { get; private set;}

        public CardInHandState(HandView ownerHandView)
        {
            OwnerHandView = ownerHandView;
        }

        public void Enter(CardView view)
        {
            isDragging = false;

            // Initialized here, but re-captured on mouse down to avoid stale Y after hand layout.
            dragPlaneOrigin = new Vector3(0f, view.transform.position.y, 0f);

            Debug.Log($"CardInHandState: Entered for {view.CardData.Name}");
        }

        public void Exit(CardView view)
        {
            isDragging = false;
        }

        public void OnMouseDown(CardView view)
        {
            isDragging = true;
            // Capture the current Y at drag start; the card may have been re-laid out after Enter().
            dragPlaneOrigin = new Vector3(0f, view.transform.position.y, 0f);
            view.transform.SetParent(null, true);
        }

        public void OnMouseDrag(CardView view)
        {
            if (!isDragging) return;

            Ray ray = view.MainCamera.ScreenPointToRay(Input.mousePosition);
            Plane plane = new Plane(Vector3.up, dragPlaneOrigin);

            if (plane.Raycast(ray, out float distance))
            {
                Vector3 point = ray.GetPoint(distance);
                point.y += view.dragHeightOffset;
                view.transform.position = point;
            }
        }

        public void OnMouseUp(CardView view)
        {
            isDragging = false;
            bool placedInZone = TryGetZone(view, out PlayAreaZone zone, out PlayAreaZoneView zoneView);
            if (
                placedInZone && 
                view.CardData.GameState != null &&
                view.CardData.GameState.TryMoveToZone(view.CardData, view.CardData.Owner.Hand, zone))
            {
                zoneView.AcceptCard(view);

                view.OccupiedZone = zone;
                view.OccupiedZoneView = zoneView;
                
                view.SetState(new CardInPlayState(zoneView));
                return;
            }
            OwnerHandView.ReturnCard(view);
        }

        private static bool TryGetZone(
            CardView view,
            out PlayAreaZone zone,
            out PlayAreaZoneView zoneView)
        {
            zone = null;
            zoneView = null;

            Ray ray = new Ray(view.transform.position, Vector3.down);
            if (!Physics.Raycast(ray, out RaycastHit hit, view.zoneRaycastDistance))
                return false;

            zoneView = hit.collider.GetComponent<PlayAreaZoneView>();
            if (zoneView == null)
                return false;

            foreach (var kvp in view.PlayArea.ZoneViews)
            {
                if (kvp.Value == zoneView && !kvp.Key.IsOccupied)
                {
                    zone = kvp.Key;
                    return true;
                }
            }

            return false;
        }
    }
}
