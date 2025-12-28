
using System;
using System.Collections.Generic;
using Assets.Scripts.CardEngine.Effects;
using Assets.Scripts.CardEngine.Game;
using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Events;

using UnityEngine;


namespace Assets.Scripts
{
    public class GameController : MonoBehaviour
    {
        [SerializeField] private GameObject CardPrefab;
        [SerializeField] private GameObject PlayerBoardPrefab;
        [SerializeField] private GameObject Board;
        public GameObject BoardObject => Board;
        public GameObject PlayerBoardObject => PlayerBoardPrefab;
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

        private void OnCardPlayed(CardPlayedEvent e)
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
            
            // Instantiate hands for each player, passing the hand area
            player1.Hand = new Hand(cardPrefab: CardPrefab, handArea: playerBoard1.HandArea, owner: player1, gameState: GameState);
            player2.Hand = new Hand(cardPrefab: CardPrefab, handArea: playerBoard2.HandArea, owner: player2, gameState: GameState);

            // Create and add sample cards to each hand
            var card1 = new Card { Id = "1", Name = "Fireball", CardType = new CardType { Name = "Spell" } };
            var card2 = new Card { Id = "2", Name = "Goblin", CardType = new CardType { Name = "Creature" } };
            var card3 = new Card { Id = "3", Name = "Heal", CardType = new CardType { Name = "Spell" } };
 
            player1.Hand.AddCard(card1);
            player1.Hand.AddCard(card2);
            player2.Hand.AddCard(card3);
        }
    }
}
