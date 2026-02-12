using System;

using UnityEngine;
using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Game;
using Assets.Scripts.CardEngine.Cards.Views;

namespace Assets.Scripts.CardEngine.Cards 
{
    public class CardFactory: MonoBehaviour
    {
        [Serializable]
        private class CardCategoryPrefab
        {
            [SerializeField] private CardType _category;
            [SerializeField] private GameObject _prefab;

            public CardType Category => _category;
            public GameObject Prefab => _prefab;
        }

        [SerializeField] private GameObject _cardPrefab;
        [SerializeField] private GameObject _hiddenCardPrefab;
        [SerializeField] private CardCategoryPrefab[] _prefabsByCategory;

        [Header("Deploy Cost Stars")]
        [Tooltip("Material used for filled stars on the 3D card prefab. Stars are expected to be child objects named OneStar, TwoStar, ThreeStar.")]
        [SerializeField] private Material _filledStarMaterial;
        [SerializeField] private Material _baseStarMaterial;
        private Transform _spawnParent;

        public Material FilledStarMaterial => _filledStarMaterial;
        public Material StarBaseMaterial => _baseStarMaterial;

        public void SetSpawnParent(Transform spawnParent)
        {
            _spawnParent = spawnParent;
        }

        public CardView CreateCard(Card card, GameState gameState = null, CardViewRegistry registry = null, bool hidden = false)
        {
            if (card == null)
            {
                Debug.LogError("CardFactory: CreateCard called with null Card.");
                return null;
            }

			GameObject prefabToUse = GetPrefabFor(card, hidden);
            Debug.Log($"CardFactory: Creating card view: {card.Name} (category: {card.Behavior?.Category.ToString() ?? "<null>"}, hidden: {hidden}, prefab: {(prefabToUse != null ? prefabToUse.name : "<null>")})");

            if (prefabToUse == null)
            {
                Debug.LogError("CardFactory: No prefab resolved for card (default prefab or type prefab missing)." );
                return null;
            }

            Transform parent = _spawnParent;
            var cardGO = Instantiate(original: prefabToUse, parent: parent);
            cardGO.name = $"Card_{card.Name}";
            card.GameState = gameState;
            var cardView = cardGO.GetComponent<CardView>();
            if (cardView != null)
            {
                ConfigureCardView(cardView, card, hidden);
                registry?.Register(card, cardView);
            }
            return cardView;
        }

        public void ConfigureCardView(CardView cardView, Card card, bool hidden)
        {
            if (cardView == null || card == null)
                return;

            cardView.CardData = card;
            cardView.SetHidden(hidden);

            // Option C: delegate to optional sub-views when present.
            var face = cardView.GetComponent<CardFaceText3DView>();
            if (face != null)
            {
                face.Set(card, hidden);
            }
            else
            {
                // Legacy fallback (older prefabs).
                if (cardView.NameText != null)
                    cardView.NameText.text = hidden ? string.Empty : (card.Name ?? string.Empty);
                if (cardView.DescriptionText != null)
                    cardView.DescriptionText.text = hidden ? string.Empty : (card.EffectText ?? string.Empty);
            }

            // Avoid revealing stats/markers on hidden cards.
            if (hidden)
            {
                // Clear ALL TextMeshPro components to ensure no text leaks through
                // on the hidden prefab, even if fields are not wired.
                foreach (var tmp in cardView.GetComponentsInChildren<TMPro.TMP_Text>(true))
                    tmp.text = string.Empty;
                return;
            }

            if (card.Behavior is TroopBehavior troop)
            {
                cardView.ApplyDeployCostStars(troop.DeployCost, _baseStarMaterial, _filledStarMaterial);
                cardView.UpdateTroopStats(troop);
                troop.OnStatsChanged += cardView.UpdateTroopStats;

                // Auto-wire attack UI (sword icon) for troops.
                if (cardView.gameObject.GetComponent<AttackSwordPresenter>() == null)
                    cardView.gameObject.AddComponent<AttackSwordPresenter>();
            }
        }

        private GameObject GetPrefabFor(Card card, bool hidden)
        {
			if (hidden && _hiddenCardPrefab != null)
				return _hiddenCardPrefab;

			CardType? category = card != null ? card.Category : null;
            if (category != null && _prefabsByCategory != null)
            {
                for (int i = 0; i < _prefabsByCategory.Length; i++)
                {
                    var entry = _prefabsByCategory[i];

                    if (entry?.Category == category.Value && entry?.Prefab != null)
                        return entry.Prefab;
                }
            }

            return _cardPrefab;
        }

    }
}