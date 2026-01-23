using System.Collections.Generic;
using System.Threading.Tasks;
using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.CardEngine.UI
{
    public sealed class SelectCardFromZonePromptUI : MonoBehaviour, ISelectCardFromZonePrompter
    {
        [Header("Wiring")]
        [SerializeField] private GameObject _root;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _subtitle;
        [SerializeField] private Button _cancelButton;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private TMP_Text _selectionCount;

        [Header("Options")]
        [SerializeField] private Transform _optionsRoot;
        [Tooltip("Optional: a plain UI Button used as an option template.")]
        [SerializeField] private Button _optionButtonPrefab;

        [Tooltip("Optional: if set, uses UIController.GetUIPrefabForCard(card) to create clickable card entries.")]
        [SerializeField] private UIController _uiController;

        private readonly List<GameObject> _spawned = new();
        private readonly HashSet<Card> _selected = new();
        private TaskCompletionSource<IReadOnlyList<Card>> _pending;
        private bool _loggedMissingWiring;
        private bool _allowCancel;
        private int _minSelections;
        private int _maxSelections;

        private void Awake()
        {
            if (_root != null)
                _root.SetActive(false);

            if (_cancelButton != null)
                _cancelButton.onClick.AddListener(Cancel);

            if (_confirmButton != null)
                _confirmButton.onClick.AddListener(Confirm);
        }

        private void OnDisable()
        {
            // If UI disappears while waiting, treat as cancel.
            Complete(null);
        }

        public Task<IReadOnlyList<Card>> ChooseCardsAsync(SelectCardFromZoneRequest request)
        {
            if (!ValidateWiring())
                return Task.FromResult<IReadOnlyList<Card>>(null);

            ApplySelectionLimits(request);
            EnsureUIController();
            BeginPrompt(request);

            return _pending.Task;
        }

        private bool ValidateWiring()
        {
            if (_root != null && _optionsRoot != null)
                return true;

            if (!_loggedMissingWiring)
            {
                _loggedMissingWiring = true;
                Debug.LogError("SelectCardFromZonePromptUI: Missing wiring. Assign _root and _optionsRoot in the inspector; prompt UI will not show.");
            }

            return false;
        }

        private void ApplySelectionLimits(SelectCardFromZoneRequest request)
        {
            _allowCancel = request.AllowCancel;
            _minSelections = request.MinSelections < 0 ? 0 : request.MinSelections;
            _maxSelections = request.MaxSelections <= 0 ? 1 : request.MaxSelections;
            if (_minSelections > _maxSelections)
                _minSelections = _maxSelections;
        }

        private void EnsureUIController()
        {
            if (_uiController == null)
                _uiController = FindFirstObjectByType<UIController>();
        }

        private void BeginPrompt(SelectCardFromZoneRequest request)
        {
            // Cancel any previous prompt.
            Complete(null);
            _pending = new TaskCompletionSource<IReadOnlyList<Card>>();
            _selected.Clear();

            SetHeaderText(request);
            SetButtonsActive();

            RebuildOptions(request.Candidates);
            RefreshSelectionUI();

            _root.SetActive(true);
        }

        private void SetHeaderText(SelectCardFromZoneRequest request)
        {
            string zoneName = request.Zone != null ? request.Zone.ZoneName : "<zone>";
            string playerName = request.Player != null ? request.Player.Name : "<player>";

            if (_title != null)
                _title.text = string.IsNullOrWhiteSpace(request.Title) ? $"Select a card ({zoneName})" : request.Title;

            if (_subtitle != null)
                _subtitle.text = string.IsNullOrWhiteSpace(request.Subtitle) ? playerName : request.Subtitle;
        }

        private void SetButtonsActive()
        {
            if (_cancelButton != null)
                _cancelButton.gameObject.SetActive(_allowCancel);

            if (_confirmButton != null)
                _confirmButton.gameObject.SetActive(true);
        }

        private void RebuildOptions(IReadOnlyList<Card> cards)
        {
            ClearSpawned();
            _selected.Clear();

            if (cards == null || cards.Count == 0)
                return;

            if (_optionButtonPrefab != null)
            {
                BuildOptionsUsingButtons(cards);
                return;
            }

            BuildOptionsUsingCardPrefabs(cards);
        }

        private void ClearSpawned()
        {
            for (int i = 0; i < _spawned.Count; i++)
            {
                if (_spawned[i] != null)
                    Destroy(_spawned[i]);
            }
            _spawned.Clear();
        }

        private void BuildOptionsUsingButtons(IReadOnlyList<Card> cards)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                if (card == null)
                    continue;

                var button = Instantiate(_optionButtonPrefab, _optionsRoot);
                button.gameObject.SetActive(true);
                SetButtonLabel(button, card.Name);
                button.onClick.AddListener(() => Toggle(card));
                _spawned.Add(button.gameObject);
            }
        }

        private void BuildOptionsUsingCardPrefabs(IReadOnlyList<Card> cards)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                if (card == null)
                    continue;

                var go = CreateCardPrefabOption(card);
                if (go == null)
                    continue;

                _spawned.Add(go);
            }
        }

        private GameObject CreateCardPrefabOption(Card card)
        {
            var prefab = _uiController != null ? _uiController.GetUIPrefabForCard(card) : null;
            if (prefab == null)
                return null;

            var go = Instantiate(prefab, _optionsRoot);
            go.name = $"SelectCardFromZoneOption_{card.Id}";
            go.SetActive(true);

            BindCardView(go, card);
            EnsureClickHandlers(go, card);
            return go;
        }

        private void EnsureClickHandlers(GameObject optionObject, Card card)
        {
            var uiCardView = optionObject.GetComponentInChildren<UICardView>(includeInactive: true);
            if (uiCardView != null)
            {
                var clickOnView = uiCardView.GetComponent<SelectCardFromZoneOptionUICardClickHandler>();
                if (clickOnView == null)
                    clickOnView = uiCardView.gameObject.AddComponent<SelectCardFromZoneOptionUICardClickHandler>();
                clickOnView.Bind(this, card);
            }

            EnsureClickCatcher(optionObject, card);
        }

        private void EnsureClickCatcher(GameObject optionRoot, Card card)
        {
            if (optionRoot == null)
                return;

            var rootRect = optionRoot.GetComponent<RectTransform>();
            if (rootRect == null)
            {
                var direct = optionRoot.GetComponent<SelectCardFromZoneOptionClickHandler>();
                if (direct == null)
                    direct = optionRoot.AddComponent<SelectCardFromZoneOptionClickHandler>();
                direct.Bind(this, card);
                return;
            }

            var existing = optionRoot.transform.Find("ClickCatcher");
            GameObject catcher;
            if (existing != null)
            {
                catcher = existing.gameObject;
            }
            else
            {
                catcher = new GameObject("ClickCatcher", typeof(RectTransform), typeof(Image));
                catcher.transform.SetParent(optionRoot.transform, worldPositionStays: false);

                var rt = catcher.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                var img = catcher.GetComponent<Image>();
                img.color = new Color(0, 0, 0, 0);
                img.raycastTarget = true;

                catcher.transform.SetAsLastSibling();
            }

            var click = catcher.GetComponent<SelectCardFromZoneOptionClickHandler>();
            if (click == null)
                click = catcher.AddComponent<SelectCardFromZoneOptionClickHandler>();
            click.Bind(this, card);

            RefreshCatcherVisual(catcher.GetComponent<Image>(), card);
        }

        private void RefreshCatcherVisual(Image catcherImage, Card card)
        {
            if (catcherImage == null)
                return;

            bool isSelected = card != null && _selected.Contains(card);
            catcherImage.color = isSelected ? new Color(0.15f, 0.85f, 0.25f, 0.18f) : new Color(0, 0, 0, 0);
        }

        private void RefreshSelectionUI()
        {
            int count = _selected.Count;

            if (_selectionCount != null)
            {
                if (_maxSelections <= 1)
                    _selectionCount.text = string.Empty;
                else
                    _selectionCount.text = $"Selected: {count}/{_maxSelections}";
            }

            if (_confirmButton != null)
                _confirmButton.interactable = count >= _minSelections && count <= _maxSelections;

            // Update highlight for any click catchers.
            for (int i = 0; i < _spawned.Count; i++)
            {
                var go = _spawned[i];
                if (go == null)
                    continue;

                var catcher = go.transform.Find("ClickCatcher");
                if (catcher == null)
                    continue;

                var img = catcher.GetComponent<Image>();
                // Best-effort: infer card by bound handler.
                var click = catcher.GetComponent<SelectCardFromZoneOptionClickHandler>();
                if (click != null)
                    RefreshCatcherVisual(img, click.Card);
            }
        }

        private static void BindCardView(GameObject item, Card card)
        {
            var uiView = item != null ? item.GetComponent<UICardView>() : null;
            if (uiView != null)
            {
                uiView.Bind(card);
                return;
            }

            var label = item != null ? item.GetComponentInChildren<TMP_Text>(includeInactive: true) : null;
            if (label != null)
                label.text = card != null ? card.Name : "<null>";
        }

        private static void SetButtonLabel(Button button, string text)
        {
            if (button == null)
                return;

            var tmp = button.GetComponentInChildren<TMP_Text>(includeInactive: true);
            if (tmp != null)
                tmp.text = text;
        }

        public void Toggle(Card card)
        {
            if (card == null)
                return;

            if (_selected.Contains(card))
            {
                _selected.Remove(card);
                RefreshSelectionUI();
                return;
            }

            if (_selected.Count >= _maxSelections)
                return;

            _selected.Add(card);
            RefreshSelectionUI();
        }

        private void Confirm()
        {
            if (_selected.Count < _minSelections)
                return;

            // Preserve UI ordering as much as possible: follow spawned options order.
            var chosen = new List<Card>(_selected.Count);
            for (int i = 0; i < _spawned.Count; i++)
            {
                var go = _spawned[i];
                if (go == null)
                    continue;

                var catcher = go.transform.Find("ClickCatcher");
                var click = catcher != null ? catcher.GetComponent<SelectCardFromZoneOptionClickHandler>() : null;
                var card = click != null ? click.Card : null;
                if (card != null && _selected.Contains(card))
                    chosen.Add(card);
            }

            // Fallback if we couldn't infer ordering.
            if (chosen.Count == 0 && _selected.Count > 0)
                chosen.AddRange(_selected);

            Complete(chosen);
        }

        private void Cancel()
        {
            if (!_allowCancel)
                return;

            Complete(null);
        }

        private void Complete(IReadOnlyList<Card> choice)
        {
            if (_pending == null)
            {
                if (_root != null)
                    _root.SetActive(false);
                return;
            }

            var tcs = _pending;
            _pending = null;

            if (_root != null)
                _root.SetActive(false);

            tcs.TrySetResult(choice);
        }
    }
}
