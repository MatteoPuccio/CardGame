using UnityEngine;
using Assets.Scripts.CardEngine.Board;
using Assets.Scripts.CardEngine.Game;

namespace Assets.Scripts.CardEngine.Cards
{
    public class CardView : MonoBehaviour
    {
        public Card CardData;

        [Header("Movement")]
        public float dragHeightOffset = 0.5f;
        public float placeHeightOffset = 0.1f;
        public float zoneRaycastDistance = 2f;

        [HideInInspector] public Camera MainCamera;
        [HideInInspector] public PlayArea PlayArea;

        public PlayAreaZone OccupiedZone { get; set; }
        public PlayAreaZoneView OccupiedZoneView { get; set; }

        private ICardInteractionState _state;

        void Start()
        {
            MainCamera = Camera.main;

            string tag = CardData.Owner.IsLocalPlayer
                ? PlayerArea.Local.ToString()
                : PlayerArea.Opponent.ToString();

            PlayArea = GameObject.FindGameObjectWithTag(tag)
                                 ?.GetComponent<PlayArea>();
        }

        public void SetState(ICardInteractionState newState)
        {
            _state?.Exit(this);
            _state = newState;
            _state?.Enter(this);
        }

        void OnMouseDown() => _state?.OnMouseDown(this);
        void OnMouseDrag() => _state?.OnMouseDrag(this);
        void OnMouseUp()   => _state?.OnMouseUp(this);

    }
}
