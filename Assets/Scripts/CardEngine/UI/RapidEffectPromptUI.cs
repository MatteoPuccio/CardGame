using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Assets.Scripts.CardEngine.Game;
using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Assets.Scripts.CardEngine.UI
{
    public sealed class RapidEffectPromptUI : MonoBehaviour, IRapidEffectPrompter
    {
        [Header("Wiring")]
        [SerializeField] private GameObject _root;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private Button _passButton;

        [Header("Options")]
        [SerializeField] private Transform _optionsRoot;
        [Tooltip("Optional: a plain UI Button used as an option template. If omitted, the UI will try to use your existing UICard prefabs via UIController.")]
        [SerializeField] private Button _optionButtonPrefab;

        [Tooltip("Optional: if set, uses UIController.GetUIPrefabForCard(card) to create clickable card entries (supports multiple prefabs by CardType).")]
        [SerializeField] private UIController _uiController;

        private readonly List<GameObject> _spawnedOptionObjects = new();
        private TaskCompletionSource<RapidEffectOption?> _pending;
        private bool _loggedMissingWiring;

        private void Awake()
        {
            if (_root != null)
                _root.SetActive(false);

            if (_passButton != null)
                _passButton.onClick.AddListener(Pass);
        }

        private void OnDisable()
        {
            // If UI disappears while waiting, treat as pass.
            Complete(null);
        }

        public Task<RapidEffectOption?> ChooseActivationAsync(Player player, IReadOnlyList<RapidEffectOption> options)
        {
            if (_root == null || _optionsRoot == null)
            {
                if (!_loggedMissingWiring)
                {
                    _loggedMissingWiring = true;
                    Debug.LogError(
                        "RapidEffectPromptUI: Missing wiring. Assign _root and _optionsRoot in the inspector; prompt UI will not show.");
                }
                return Task.FromResult<RapidEffectOption?>(null);
            }

            if (_uiController == null)
                _uiController = FindFirstObjectByType<UIController>();

            // Cancel any previous prompt.
            Complete(null);

            _pending = new TaskCompletionSource<RapidEffectOption?>();

            if (_title != null)
                _title.text = $"Rapid effects: {player?.Name ?? "<player>"}";

            RebuildOptions(options);

            Debug.Log($"RapidEffectPromptUI: Showing prompt for '{player?.Name ?? "<player>"}', options={(options == null ? 0 : options.Count)}");
            _root.SetActive(true);
            return _pending.Task;
        }

        private void RebuildOptions(IReadOnlyList<RapidEffectOption> options)
        {
            ClearSpawnedOptions();

            if (options == null || options.Count == 0)
                return;

            if (_optionButtonPrefab != null)
            {
                BuildOptionsUsingButtons(options);
                return;
            }

            BuildOptionsUsingCardPrefabs(options);
        }

        private void ClearSpawnedOptions()
        {
            for (int i = 0; i < _spawnedOptionObjects.Count; i++)
            {
                if (_spawnedOptionObjects[i] != null)
                    Destroy(_spawnedOptionObjects[i]);
            }
            _spawnedOptionObjects.Clear();
        }

        private void BuildOptionsUsingButtons(IReadOnlyList<RapidEffectOption> options)
        {
            for (int i = 0; i < options.Count; i++)
            {
                var option = options[i];
                var button = Instantiate(_optionButtonPrefab, _optionsRoot);
                button.gameObject.SetActive(true);
                SetButtonLabel(button, option.ToString());
                button.onClick.AddListener(() => Activate(option));
                _spawnedOptionObjects.Add(button.gameObject);
            }
        }

        private void BuildOptionsUsingCardPrefabs(IReadOnlyList<RapidEffectOption> options)
        {
            for (int i = 0; i < options.Count; i++)
            {
                var option = options[i];
                var go = CreateCardPrefabOption(option);
                if (go == null)
                    continue;

                _spawnedOptionObjects.Add(go);
            }
        }

        private GameObject CreateCardPrefabOption(RapidEffectOption option)
        {
            var cardPrefab = _uiController != null ? _uiController.GetUIPrefabForCard(option.SourceCard) : null;
            if (cardPrefab == null)
                return null;

            var go = Instantiate(cardPrefab, _optionsRoot);
            go.name = $"RapidEffectOption_{option}";
            go.SetActive(true);

            BindCardView(go, option.SourceCard);
            BindClickHandlers(go, option);
            return go;
        }

        private void BindClickHandlers(GameObject optionObject, RapidEffectOption option)
        {
            var uiCardView = optionObject.GetComponentInChildren<UICardView>(includeInactive: true);
            if (uiCardView != null)
            {
                var clickOnView = uiCardView.GetComponent<RapidEffectOptionUICardClickHandler>();
                if (clickOnView == null)
                    clickOnView = uiCardView.gameObject.AddComponent<RapidEffectOptionUICardClickHandler>();
                clickOnView.Bind(this, option);
            }

            EnsureClickCatcher(optionObject, option);
        }

        private void EnsureClickCatcher(GameObject optionRoot, RapidEffectOption option)
        {
            if (optionRoot == null)
                return;

            var rootRect = optionRoot.GetComponent<RectTransform>();
            if (rootRect == null)
            {
                // Not a UI prefab; fallback to attaching handler on the root.
                var direct = optionRoot.GetComponent<RapidEffectOptionClickHandler>();
                if (direct == null)
                    direct = optionRoot.AddComponent<RapidEffectOptionClickHandler>();
                direct.Bind(this, option);
                return;
            }

            // Create or reuse a click-catcher child.
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

                // Transparent but still raycastable.
                var img = catcher.GetComponent<Image>();
                img.color = new Color(0, 0, 0, 0);
                img.raycastTarget = true;

                // Make sure it sits on top of other visuals.
                catcher.transform.SetAsLastSibling();
            }

            var click = catcher.GetComponent<RapidEffectOptionClickHandler>();
            if (click == null)
                click = catcher.AddComponent<RapidEffectOptionClickHandler>();
            click.Bind(this, option);
        }

        private static void BindCardView(GameObject item, Card card)
        {
            var uiView = item != null ? item.GetComponent<UICardView>() : null;
            if (uiView != null)
            {
                uiView.Bind(card);
                return;
            }

            var label = item != null ? item.GetComponentInChildren<Text>(includeInactive: true) : null;
            if (label != null)
                label.text = card != null ? card.Name : "<null>";
        }

        private static void SetButtonLabel(Button button, string text)
        {
            if (button == null)
                return;

            var tmp = button.GetComponentInChildren<TMP_Text>(includeInactive: true);
            if (tmp != null)
            {
                tmp.text = text;
                return;
            }

            var legacy = button.GetComponentInChildren<Text>(includeInactive: true);
            if (legacy != null)
                legacy.text = text;
        }

        private void Activate(RapidEffectOption option)
        {
            Complete(option);
        }

        public void SelectOption(RapidEffectOption option)
        {
            Activate(option);
        }

        private void Pass()
        {
            Complete(null);
        }

        private void Complete(RapidEffectOption? choice)
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
