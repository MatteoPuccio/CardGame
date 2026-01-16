using System;
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
        public string Name => "CardInHandState";

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
            if (view?.CardData?.GameState?.ActivePlayer != null && view.CardData.Owner != null && view.CardData.GameState.ActivePlayer != view.CardData.Owner)
                return;

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

            if (!IsCurrentPlayerAllowedToPlay(view))
            {
                OwnerHandView.ReturnCard(view);
                return;
            }

            if (IsNonBoardCard(view))
            {
                if (!HandleNonBoardCardRelease(view))
                {
                    OwnerHandView.ReturnCard(view);
                }
                return;
            }

            if (TryPlaceCardInZone(view, out PlayAreaZone zone, out PlayAreaZoneView zoneView))
            {
                HandlePlacedInZone(view, zone, zoneView);
                return;
            }

            OwnerHandView.ReturnCard(view);
        }

        private static bool IsCurrentPlayerAllowedToPlay(CardView view)
        {
            return !(view?.CardData?.GameState?.ActivePlayer != null &&
                     view.CardData.Owner != null &&
                     view.CardData.GameState.ActivePlayer != view.CardData.Owner);
        }

        private static bool IsNonBoardCard(CardView view)
        {
            return view?.CardData?.Behavior != null && !view.CardData.Behavior.RequiresPlayZone;
        }

        private bool HandleNonBoardCardRelease(CardView view)
        {
            if (view.CardData.GameState == null || view.CardData.Owner?.Hand == null || view.CardData.Owner?.Cemetery == null)
                return false;

            if (view.CardData.Category == CardType.Ritual)
            {
                if (!IsOverRitualZone(view))
                    return false;
            }
            else
            {
                if (!IsOverPlayArea(view))
                    return false;
            }

            return TryBeginTargetingOrPlay(view, view.CardData.Owner.Hand, onCancelled: () => OwnerHandView.ReturnCard(view));
        }

        private bool TryPlaceCardInZone(CardView view, out PlayAreaZone zone, out PlayAreaZoneView zoneView)
        {
            zone = null;
            zoneView = null;
            bool placedInZone = TryGetZone(view, out zone, out zoneView);
            return placedInZone &&
                   view.CardData.GameState != null &&
                   view.CardData.Owner?.Hand != null &&
                     view.CardData.GameState.MoveToZone(view.CardData, view.CardData.Owner.Hand, zone, this);
        }

        private void HandlePlacedInZone(CardView view, PlayAreaZone zone, PlayAreaZoneView zoneView)
        {
            if (view == null || zone == null || zoneView == null)
                return;

            zoneView.AcceptCard(view);

            view.OccupiedZone = zone;
            view.OccupiedZoneView = zoneView;

            if (TryBeginTargetingOrPlay(view, view.CardData.Owner.Hand, onCancelled: () => RollbackPlacedCardToHand(view, zone)))
                return;

            RollbackPlacedCardToHand(view, zone);
        }

        private void RollbackPlacedCardToHand(CardView view, PlayAreaZone zone)
        {
            // Play was cancelled (e.g., requires a target but none exist). Roll back.
            var gs = view != null ? view.CardData?.GameState : null;
            var hand = view != null ? view.CardData?.Owner?.Hand : null;

            if (gs != null && hand != null && zone != null)
                gs.MoveToZone(view.CardData, zone, hand, this);

            if (view != null)
            {
                view.OccupiedZone = null;
                view.OccupiedZoneView = null;
            }

            OwnerHandView.ReturnCard(view);
        }

        private static bool TryBeginTargetingOrPlay(CardView view, ICardZone sourceZone, Action onCancelled = null)
        {
            if (view?.CardData == null)
                return false;

            var card = view.CardData;
            var gameState = card.GameState;

            if (gameState == null)
                return false;

            if (!card.TryBeginPlay(sourceZone, out var session, out var candidates))
                return false;

            if (candidates != null && candidates.Count > 0)
            {
                gameState.Targeting.Begin(session, candidates, onCancelled);
            }

            return true;
        }

        private static bool IsOverPlayArea(CardView view)
        {
            if (view == null)
                return false;

            Ray ray = new Ray(view.transform.position, Vector3.down);
            if (!Physics.Raycast(ray, out RaycastHit hit, view.zoneRaycastDistance))
                return false;

            // Accept if the hit object is part of the play area (including zones).
            return hit.collider.GetComponentInParent<PlayArea>() != null;
        }

        private static bool IsOverRitualZone(CardView view)
        {
            if (view == null)
                return false;

            Ray ray = new Ray(view.transform.position, Vector3.down);
            if (!Physics.Raycast(ray, out RaycastHit hit, view.zoneRaycastDistance))
                return false;

            return hit.collider.GetComponentInParent<Game.RitualZoneView>() != null;
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
