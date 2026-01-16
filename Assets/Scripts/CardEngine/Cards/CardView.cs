using UnityEngine;
using Assets.Scripts.CardEngine.Board;
using Assets.Scripts.CardEngine.Game;
using TMPro;

namespace Assets.Scripts.CardEngine.Cards
{
    public class CardView : MonoBehaviour
    {
        public Card CardData;

        [Header("Movement")]
        public float dragHeightOffset = 0.5f;
        public float placeHeightOffset = 0.1f;
        public float zoneRaycastDistance = 2f;

        [Header("Text")]
        public TextMeshPro NameText;
        public TextMeshPro DescriptionText;

        [Header("Troop Properties")]
        [SerializeField] private Renderer _oneStar;
        [SerializeField] private Renderer _twoStar;
        [SerializeField] private Renderer _threeStar;

        [SerializeField] private TextMeshPro _powerText;
        [SerializeField] private TextMeshPro _healthText;


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

        void OnMouseDown() => HandleClick();

        public void HandleClick()
        {
            // If the game is in targeting mode, clicks are used to pick a target.
            if (CardData?.GameState?.Targeting != null && CardData.GameState.Targeting.IsActive)
            {
                CardData.GameState.Targeting.TrySelect(CardData);
                return;
            }

            CardPreviewController.Show(CardData);
            _state?.OnMouseDown(this);
        }
        void OnMouseDrag() => _state?.OnMouseDrag(this);
        void OnMouseUp()   => _state?.OnMouseUp(this);

        public void ApplyDeployCostStars(int deployCost, Material baseMaterial, Material filledMaterial)
        {
            if(CardData.Category == CardType.Troop) {
                ApplyStar(_oneStar, deployCost >= 1, baseMaterial, filledMaterial);
                ApplyStar(_twoStar, deployCost >= 2, baseMaterial, filledMaterial);
                ApplyStar(_threeStar, deployCost >= 3, baseMaterial, filledMaterial);
            }
        }

        private static void ApplyStar(Renderer renderer, bool filled, Material baseMaterial, Material filledMaterial)
        {
            renderer.material = filled ?
                filledMaterial :
                baseMaterial;
        }

        public void UpdateTroopStats(TroopBehavior troop)
        {
            if (_powerText == null || _healthText == null)
                return;
            _powerText.text = troop.Power.ToString();
            _healthText.text = troop.Health.ToString();
        }
    }
}
