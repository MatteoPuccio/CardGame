using UnityEngine;
using Assets.Scripts.CardEngine.Board;
using Assets.Scripts.CardEngine.Game;
using Assets.Scripts.CardEngine.Cards.Views;
using TMPro;

namespace Assets.Scripts.CardEngine.Cards
{
    public class CardView : MonoBehaviour
    {
        public Card CardData;

        public bool IsHidden { get; private set; }

        public void SetHidden(bool hidden)
        {
            IsHidden = hidden;
        }

        [Header("Movement")]
        public float dragHeightOffset = 0.5f;
        public float placeHeightOffset = 0.1f;
        public float zoneRaycastDistance = 2f;

        [Header("Text")]
        public TextMeshPro NameText;
        public TextMeshPro DescriptionText;
        
        [Header("Spell School")]
        [SerializeField] private TextMeshPro _spellSchoolText;
        [Header("Race Tag")]
        [SerializeField] private TMP_Text _raceText;
        [SerializeField] private BoxCollider _raceClickCollider;

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

            if (TryHandleRaceTagClick())
                return;

            _state?.OnMouseDown(this);
        }
        void OnMouseDrag() => _state?.OnMouseDrag(this);
        void OnMouseUp()   => _state?.OnMouseUp(this);

        void OnMouseEnter()
        {
            if (IsHidden)
                return;
            if (CardData != null)
                CardPreviewController.Show(CardData);
        }

        void OnMouseExit()
        {
            CardPreviewController.ClearPreview();
        }

        public void ApplyDeployCostStars(int deployCost, Material baseMaterial, Material filledMaterial)
        {
            if (CardData == null)
                return;

            // Option C: visuals are composed via optional components.
            // If this view has a dedicated stars sub-view, use it.
            var starsView = GetComponent<DeployCostStars3DView>();
            if (starsView != null)
            {
                if (CardData.Category == CardType.Troop)
                    starsView.Apply(deployCost, baseMaterial, filledMaterial);
                return;
            }

            // Legacy fallback: only attempt if any renderer is actually configured.
            if (_oneStar == null && _twoStar == null && _threeStar == null)
                return;

            if(CardData.Category == CardType.Troop) {
                ApplyStar(_oneStar, deployCost >= 1, baseMaterial, filledMaterial);
                ApplyStar(_twoStar, deployCost >= 2, baseMaterial, filledMaterial);
                ApplyStar(_threeStar, deployCost >= 3, baseMaterial, filledMaterial);
            }
        }

        private static void ApplyStar(Renderer renderer, bool filled, Material baseMaterial, Material filledMaterial)
        {
            if (renderer == null)
                return;

            renderer.material = filled ?
                filledMaterial :
                baseMaterial;
        }

        public void UpdateTroopStats(TroopBehavior troop)
        {
            if (troop == null)
                return;

            if (_powerText != null)
                _powerText.text = troop.Power.ToString();
            if (_healthText != null)
                _healthText.text = troop.Health.ToString();

            UpdateRaceTag(troop);
        }

        private void UpdateRaceTag(TroopBehavior troop)
        {
            if (_raceText == null)
                return;

            if (troop == null || !troop.HasRace)
            {
                _raceText.text = string.Empty;
                EnsureRaceClickCollider(enabled: false);
                return;
            }

            _raceText.text = troop.Race.ToString();
            EnsureRaceClickCollider(enabled: true);
            FitRaceClickColliderToText();
        }

        private void UpdateSpellSchoolText(SpellBehavior spell)
        {
            if (_spellSchoolText == null)
                return;

            if (spell == null || spell.School == SpellSchool.None)
            {
                _spellSchoolText.text = string.Empty;
                return;
            }

            _spellSchoolText.text = spell.School.ToString();
        }

        private void EnsureRaceClickCollider(bool enabled)
        {
            if (_raceText == null)
                return;

            _raceClickCollider ??= _raceText.GetComponent<BoxCollider>();
            if (_raceClickCollider == null)
                _raceClickCollider = _raceText.gameObject.AddComponent<BoxCollider>();

            _raceClickCollider.enabled = enabled;
            _raceClickCollider.isTrigger = true;
        }

        private void FitRaceClickColliderToText()
        {
            if (_raceText == null || _raceClickCollider == null)
                return;

            if (_raceText is not TextMeshPro tmp)
                return;

            // TMP bounds are in local space.
            var b = tmp.bounds;
            _raceClickCollider.center = b.center;
            _raceClickCollider.size = b.size;
        }

        private bool TryHandleRaceTagClick()
        {
            if (_raceText == null)
                return false;
            if (CardData?.Behavior is not TroopBehavior troop)
                return false;
            if (!troop.HasRace)
                return false;

            var cam = MainCamera != null ? MainCamera : Camera.main;
            if (cam == null)
                return false;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            var hits = Physics.RaycastAll(ray, 200f);
            if (hits == null || hits.Length == 0)
                return false;

            // Only handle clicks on our race tag hitbox.
            if (_raceClickCollider == null)
                return false;

            bool hitRace = false;
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider == _raceClickCollider)
                {
                    hitRace = true;
                    break;
                }
            }

            if (!hitRace)
                return false;

            RaceInfoPanelController.Show(troop.Race, Input.mousePosition);
            return true;
        }
    }
}
