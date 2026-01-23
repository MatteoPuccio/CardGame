using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Effects;
using Assets.Scripts.CardEngine.Game;
using Assets.Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.CardEngine.UI
{
    public sealed class OptionalEffectPromptUI : MonoBehaviour, IOptionalEffectPrompter
    {
        [Header("Wiring")]
        [SerializeField] private GameObject _root;
        [SerializeField] private TMP_Text _title;

        [Header("Options")]
        [SerializeField] private Transform _optionsRoot;
        [Tooltip("Optional: if set, uses UIController.GetUIPrefabForCard(card) to create clickable card entries (supports multiple prefabs by CardType).")]
        [SerializeField] private UIController _uiController;

        [SerializeField] private Button _passButton;

        private readonly List<GameObject> _spawnedOptionObjects = new();
        private TaskCompletionSource<OptionalEffectOption?> _pending;
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

        public Task<OptionalEffectOption?> ChooseActivationAsync(Player player, IReadOnlyList<OptionalEffectOption> options)
        {
            // Only prompt the local player; for AI/opponent, default to activate (preserves old behavior).
            if (player != null && !player.IsLocalPlayer)
            {
                if (options == null || options.Count == 0)
                    return Task.FromResult<OptionalEffectOption?>(null);
                return Task.FromResult<OptionalEffectOption?>(options[0]);
            }

            if (_root == null || _optionsRoot == null)
            {
                if (!_loggedMissingWiring)
                {
                    _loggedMissingWiring = true;
                    Debug.LogWarning("OptionalEffectPromptUI: Missing wiring. Assign _root and _optionsRoot; optional effects will auto-activate.");
                }

                if (options == null || options.Count == 0)
                    return Task.FromResult<OptionalEffectOption?>(null);
                return Task.FromResult<OptionalEffectOption?>(options[0]);
            }

            if (_uiController == null)
                _uiController = FindFirstObjectByType<UIController>();

            // Cancel any previous prompt.
            Complete(null);

            _pending = new TaskCompletionSource<OptionalEffectOption?>();

            if (_title != null)
                _title.text = $"Optional effects: {player?.Name ?? "<player>"}";

            RebuildOptions(options);

            _root.SetActive(true);
            return _pending.Task;
        }

        private void RebuildOptions(IReadOnlyList<OptionalEffectOption> options)
        {
            ClearSpawnedOptions();

            if (options == null || options.Count == 0)
                return;

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

        private void BuildOptionsUsingCardPrefabs(IReadOnlyList<OptionalEffectOption> options)
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

        private GameObject CreateCardPrefabOption(OptionalEffectOption option)
        {
            var cardPrefab = _uiController != null ? _uiController.GetUIPrefabForCard(option.SourceCard) : null;
            if (cardPrefab == null)
                return null;

            var go = Instantiate(cardPrefab, _optionsRoot);
            go.name = $"OptionalEffectOption_{option}";
            go.SetActive(true);

            BindCardView(go, option.SourceCard);
            BindClickHandlers(go, option);
            return go;
        }

        private void BindClickHandlers(GameObject optionObject, OptionalEffectOption option)
        {
            var uiCardView = optionObject.GetComponentInChildren<UICardView>(includeInactive: true);
            if (uiCardView != null)
            {
                var clickOnView = uiCardView.GetComponent<OptionalEffectOptionUICardClickHandler>();
                if (clickOnView == null)
                    clickOnView = uiCardView.gameObject.AddComponent<OptionalEffectOptionUICardClickHandler>();
                clickOnView.Bind(this, option);
            }

            EnsureClickCatcher(optionObject, option);
        }

        private void EnsureClickCatcher(GameObject optionRoot, OptionalEffectOption option)
        {
            if (optionRoot == null)
                return;

            var rootRect = optionRoot.GetComponent<RectTransform>();
            if (rootRect == null)
            {
                var direct = optionRoot.GetComponent<OptionalEffectOptionClickHandler>();
                if (direct == null)
                    direct = optionRoot.AddComponent<OptionalEffectOptionClickHandler>();
                direct.Bind(this, option);
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
                catcher = new GameObject("ClickCatcher", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                catcher.transform.SetParent(optionRoot.transform, worldPositionStays: false);

                var rt = catcher.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                var img = catcher.GetComponent<UnityEngine.UI.Image>();
                img.color = new Color(0, 0, 0, 0);
                img.raycastTarget = true;

                catcher.transform.SetAsLastSibling();
            }

            var click = catcher.GetComponent<OptionalEffectOptionClickHandler>();
            if (click == null)
                click = catcher.AddComponent<OptionalEffectOptionClickHandler>();
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
            {
                tmp.text = text;
            }
        }

        private void Activate(OptionalEffectOption option) => Complete(option);

        public void SelectOption(OptionalEffectOption option) => Activate(option);

        private void Pass() => Complete(null);

        private void Complete(OptionalEffectOption? result)
        {
            try
            {
                if (_root != null)
                    _root.SetActive(false);

                var pending = _pending;
                _pending = null;

                if (pending != null && !pending.Task.IsCompleted)
                    pending.TrySetResult(result);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
    }
}
