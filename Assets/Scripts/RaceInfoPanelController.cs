using TMPro;
using UnityEngine;

using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Rules;
using Assets.Scripts.CardEngine.Keywords;
using System.Text;

namespace Assets.Scripts
{
    public sealed class RaceInfoPanelController : MonoBehaviour
    {
        public static RaceInfoPanelController Instance { get; private set; }

        [Header("Wiring")]
        [SerializeField] private RectTransform _panelRoot;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _descriptionText;
        [SerializeField] private Canvas _canvas;

        private int _lastShowFrame = -1;

        private void OnEnable()
        {
            UIController.GlobalLeftClick += OnGlobalLeftClick;
        }

        private void OnDisable()
        {
            UIController.GlobalLeftClick -= OnGlobalLeftClick;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (_canvas == null)
                _canvas = GetComponentInParent<Canvas>();

            if (_panelRoot == null)
                _panelRoot = GetComponent<RectTransform>();

            Hide();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void OnGlobalLeftClick(Vector2 screenPos)
        {
            if (_panelRoot == null || !_panelRoot.gameObject.activeSelf)
                return;

            // Don’t immediately close on the same click that opened it.
            if (Time.frameCount == _lastShowFrame)
                return;

            if (RectTransformUtility.RectangleContainsScreenPoint(_panelRoot, screenPos, _canvas != null ? _canvas.worldCamera : null))
                return;

            Hide();
        }

        public static void Show(TroopRaces race, Vector2 screenPos)
        {
            Instance?.ShowInternal(race, screenPos);
        }

        public static void HidePanel()
        {
            Instance?.Hide();
        }

        private void Hide()
        {
            if (_panelRoot != null)
                _panelRoot.gameObject.SetActive(false);
        }

        private void ShowInternal(TroopRaces race, Vector2 screenPos)
        {
            if (_panelRoot == null)
                return;

            if (race == TroopRaces.None)
            {
                Hide();
                return;
            }

            _panelRoot.gameObject.SetActive(true);
            _lastShowFrame = Time.frameCount;

            if (_titleText != null)
                _titleText.text = race.ToString();

            if (_descriptionText != null)
                _descriptionText.text = BuildDescriptionText(race);
        }

        private static string BuildDescriptionText(TroopRaces race)
        {
            var sb = new StringBuilder(256);

            string raceDescription = RaceRegistry.GetDescription(race);
            if (!string.IsNullOrWhiteSpace(raceDescription))
            {
                sb.AppendLine(raceDescription.Trim());
                sb.AppendLine();
            }

            var traits = RaceRegistry.GetTraits(race);
            bool wroteAny = false;
            foreach (var trait in RaceTraitDescriptions.EnumerateFlags(traits))
            {
                wroteAny = true;
                sb.Append(RaceTraitDescriptions.GetDisplayName(trait));
                sb.Append(": ");
                sb.AppendLine(RaceTraitDescriptions.GetDescription(trait));
            }

            var keywords = RaceRegistry.GetGrantedKeywords(race);
            if (keywords != null)
            {
                for (int i = 0; i < keywords.Count; i++)
                {
                    wroteAny = true;
                    var keyword = keywords[i];
                    sb.Append(CardKeywordDescriptions.GetDisplayName(keyword));
                    sb.Append(": ");
                    sb.AppendLine(CardKeywordDescriptions.GetDescription(keyword));
                }
            }

            if (!wroteAny && sb.Length == 0)
                return string.Empty;

            return sb.ToString().TrimEnd();
        }

    }
}
