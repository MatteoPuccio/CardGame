using Assets.Scripts.CardEngine.Game;
using Assets.Scripts.CardEngine.Utils;
using UnityEngine;

namespace Assets.Scripts.CardEngine.Cards
{
    public class DraggableCard : MonoBehaviour
    {
        private Vector3 originalPosition;
        private bool isDragging = false;
        private bool isPlayed = false;
        private Camera mainCamera;
        public string playAreaTag;
        public Card CardData;
        private PlayArea playArea;
        private PlayAreaZone occupiedZone;

        void Start()
        {
            playAreaTag = CardData.Owner != null && CardData.Owner.IsLocalPlayer ? PlayerArea.Local.ToString() : PlayerArea.Opponent.ToString();
            mainCamera = Camera.main;
            originalPosition = transform.position;
            // Find the PlayArea by tag
            var playAreaGO = GameObject.FindGameObjectWithTag(playAreaTag);
            if (playAreaGO != null)
                playArea = playAreaGO.GetComponent<PlayArea>();
        }

        void OnMouseDown()
        {
            if (isPlayed)
            {
                // Return to hand
                transform.position = originalPosition;
                isPlayed = false;
                if (occupiedZone != null)
                {
                    occupiedZone.Vacate();
                    occupiedZone = null;
                }
                return;
            }
            isDragging = true;
            Debug.Log($"Card clicked: {gameObject.name}");
        }

        void OnMouseDrag()
        {
            if (isDragging)
            {
                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                Plane plane = new Plane(Vector3.up, originalPosition);
                if (plane.Raycast(ray, out float distance))
                {
                    Vector3 point = ray.GetPoint(distance);
                    transform.position = point;
                }
            }
        }

        void OnMouseUp()
        {
            isDragging = false;
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, 0.01f);
            foreach (var hit in hitColliders)
            {
                if (hit.CompareTag(playAreaTag))
                {
                    Debug.Log($"Card played: {gameObject.name}");
                    if (CardData != null)
                    {
                        CardData.Play();
                    }
                    // Snap to first available zone
                    if (playArea != null)
                    {
                        foreach (var zone in playArea.Zones)
                        {
                            if (!zone.IsOccupied)
                            {
                                transform.parent = null;
                                transform.position = zone.transform.position + new Vector3(0, 0.01f, -0.1f);
                                zone.TryOccupy(this);
                                occupiedZone = zone;
                                isPlayed = true;
                                break;
                            }
                        }
                    }
                    return;
                }
            }
            // Return to original position
            transform.position = originalPosition;
        }
    }
}
