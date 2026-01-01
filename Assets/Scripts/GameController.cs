
using System;
using System.Collections.Generic;
using Assets.Scripts.CardEngine.Effects;
using Assets.Scripts.CardEngine.Game;
using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Events;
using Assets.Scripts.CardEngine.Board;
using UnityEngine;


namespace Assets.Scripts
{
    public class GameController : MonoBehaviour
    {
        [SerializeField] private CardFactory _cardFactory;
        [SerializeField] private GameObject _playerBoardPrefab;
        [SerializeField] private GameObject _board;
        public CardFactory CardFactory => _cardFactory;
        public GameObject Board => _board;
        public GameObject PlayerBoardPrefab => _playerBoardPrefab;
        private GameState GameState;
        private EventBus EventBus;
        private EffectStack EffectStack;
        private TargetResolver TargetResolver;


        void Start()
        {
            // Initialize core systems
            EventBus = new EventBus();
            GameState = new GameState(EventBus);
            EffectStack = new EffectStack();
            TargetResolver = new TargetResolver();

            RegisterEventHandlers();
            StartGame();
        }


        private void RegisterEventHandlers()
        {
            EventBus.Subscribe<CardPlayedEvent>(OnCardPlayed);
        }

        private static void OnCardPlayed(CardPlayedEvent e)
        {
            if (e == null) return;

            string playerName = e.Player != null ? e.Player.Name : "<null>";
            string cardName = e.Source != null ? e.Source.Name : "<null>";

            if (e.EventType == "CardMoved" || e.EventType == "CardMoveFailed")
            {
                Debug.Log($"[{e.EventType}] {playerName}: {cardName} {e.From} -> {e.To}");
                return;
            }

            Debug.Log($"[{e.EventType}] Player {playerName}: {cardName}");
        }

        private void StartGame()
        {
            Player player1 = new Player(name: "Alice", isLocalPlayer: true);
            Player player2 = new Player(name: "Bob", isLocalPlayer: false);
            PlayerBoard playerBoard1 = new PlayerBoard(player: player1, gameController: this);
            PlayerBoard playerBoard2 = new PlayerBoard(player: player2, gameController: this);

            CreateHands(playerBoard1.HandController, player1, GameState);
            CreateHands(playerBoard2.HandController, player2, GameState);

            CreateDecks(playerBoard1.DeckController, player1, GameState);
            CreateDecks(playerBoard2.DeckController, player2, GameState);

            player1.PlayZones = playerBoard1.PlayAreaController.PlayArea.Zones;
            player2.PlayZones = playerBoard2.PlayAreaController.PlayArea.Zones;

            GameState.AddPlayers(player1, player2);

            // Create and add sample cards to each hand
            Card card1 = new Card { Id = "1", Name = "Fireball", CardType = new CardType { Name = "Spell" } };
            Card card2 = new Card { Id = "2", Name = "Goblin", CardType = new CardType { Name = "Creature" } };
            Card card3 = new Card { Id = "3", Name = "Heal", CardType = new CardType { Name = "Spell" } };
            Card card4 = new Card { Id = "4", Name = "Frostbolt", CardType = new CardType { Name = "Spell" } };
            Card card5 = new Card { Id = "5", Name = "Dwarf", CardType = new CardType { Name = "Creature" } };
            Card card6 = new Card { Id = "6", Name = "TrapHole", CardType = new CardType { Name = "Trap" } };

            player1.Hand.AddCard(card1);
            player1.Hand.AddCard(card2);
            player2.Hand.AddCard(card3);

            player1.Deck.AddCard(card4);
            player1.Deck.AddCard(card5);
            player2.Deck.AddCard(card6);
        }

        private static void CreateHands(HandController handController, Player player, GameState gameState)
        {
            player.Hand = new Hand(owner: player, gameState: gameState);
            
            handController.Initialize(player.Hand);
        }


        private static void CreateDecks(DeckController deckController, Player player, GameState gameState)
        {
            player.Deck = new Deck(owner: player, gameState: gameState);

            deckController.Initialize(player.Deck);
        }
    }
}
