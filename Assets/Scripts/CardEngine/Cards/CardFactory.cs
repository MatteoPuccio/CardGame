using System;

using UnityEngine;
using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Game;

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
        [SerializeField] private CardCategoryPrefab[] _prefabsByCategory;
        private Transform _spawnParent;

        public void SetSpawnParent(Transform spawnParent)
        {
            _spawnParent = spawnParent;
        }

        public CardView CreateCard(Card card, GameState gameState = null, CardViewRegistry registry = null)
        {
            if (card == null)
            {
                Debug.LogError("CardFactory: CreateCard called with null Card.");
                return null;
            }

			GameObject prefabToUse = GetPrefabFor(card);
            Debug.Log($"CardFactory: Creating card view: {card.Name} (category: {card.Behavior?.Category.ToString() ?? "<null>"}, prefab: {(prefabToUse != null ? prefabToUse.name : "<null>")})");

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
                cardView.CardData = card;
                cardView.NameText.text = card.Name;
                cardView.DescriptionText.text = card.EffectText;
                registry?.Register(card, cardView);
            }
            return cardView;
        }

        private GameObject GetPrefabFor(Card card)
        {
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