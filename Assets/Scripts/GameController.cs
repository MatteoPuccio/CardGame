
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
        [SerializeField] private GameObject _handPrefab;
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
            Debug.Log($"Player {e.Player.Name} played card: {e.Source.Name}");
        }

        private void StartGame()
        {
            // Setup players
            var player1 = new Player(name: "Alice", isLocalPlayer: true);
            var player2 = new Player(name: "Bob", isLocalPlayer: false);
            var playerBoard1 = new PlayerBoard(player: player1, gameController: this);
            var playerBoard2 = new PlayerBoard(player: player2, gameController: this);

            CreateHands(playerBoard1.HandController, player1, GameState);
            CreateHands(playerBoard2.HandController, player2, GameState);

            // Create and add sample cards to each hand
            var card1 = new Card { Id = "1", Name = "Fireball", CardType = new CardType { Name = "Spell" } };
            var card2 = new Card { Id = "2", Name = "Goblin", CardType = new CardType { Name = "Creature" } };
            var card3 = new Card { Id = "3", Name = "Heal", CardType = new CardType { Name = "Spell" } };

            player1.Hand.AddCard(card1);
            player1.Hand.AddCard(card2);
            player2.Hand.AddCard(card3);
        }

        private static void CreateHands(HandController handController, Player player, GameState gameState)
        {
            player.Hand = new Hand(owner: player, gameState: gameState);

            handController.Initialize(player.Hand);

        }
    }
}
