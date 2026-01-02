
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
        public CardViewRegistry CardViewRegistry { get; private set; }
        public GameObject Board => _board;
        public GameObject PlayerBoardPrefab => _playerBoardPrefab;
        private GameState GameState;
        private EventBus EventBus;
        private EffectStack EffectStack;
        private TargetResolver TargetResolver;

        private PlayerBoard _playerBoard1;
        private PlayerBoard _playerBoard2;

        public PlayerBoard PlayerBoard1 => _playerBoard1;
        public PlayerBoard PlayerBoard2 => _playerBoard2;


        void Start()
        {
            // Initialize core systems
            EventBus = new EventBus();
            GameState = new GameState(EventBus);
            EffectStack = new EffectStack();
            TargetResolver = new TargetResolver();
            CardViewRegistry = new CardViewRegistry();

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
            Player player1 = new Player(name: "Alice", isLocalPlayer: false);
            Player player2 = new Player(name: "Bob", isLocalPlayer: true);
            _playerBoard1 = new PlayerBoard(player: player1, gameController: this);
            _playerBoard2 = new PlayerBoard(player: player2, gameController: this);

            CreateHands(_playerBoard1.HandController, player1, GameState);
            CreateHands(_playerBoard2.HandController, player2, GameState);

            CreateDecks(_playerBoard1.DeckController, player1, GameState);
            CreateDecks(_playerBoard2.DeckController, player2, GameState);

            player1.PlayZones = _playerBoard1.PlayAreaController.PlayArea.Zones;
            player2.PlayZones = _playerBoard2.PlayAreaController.PlayArea.Zones;

            GameState.AddPlayers(player1, player2);

            // Create and add sample cards to each hand
            Card card1 = new(id: "1", name: "Fireball", cardType: new CardType { Name = "Spell" }, owner: player2, gameState: GameState);
            Card card2 = new(id: "2", name: "Goblin", cardType: new CardType { Name = "Creature" }, owner: player2, gameState: GameState);
            Card card3 = new(id: "3", name: "Heal", cardType: new CardType { Name = "Spell" }, owner: player2, gameState: GameState);
            Card card4 = new(id: "4", name: "Frostbolt", cardType: new CardType { Name = "Spell" }, owner: player1, gameState: GameState);
            Card card5 = new(id: "5", name: "Dwarf", cardType: new CardType { Name = "Creature" }, owner: player1, gameState: GameState);
            Card card6 = new Card(
                id: "6",
                name: "Raigeki",
                cardType: new CardType { Name = "Spell" },
                owner: player1,
                gameState: GameState
            );

            card6.OnPlayEffect = new TargetedEffect(
                selector: new AllEnemyCharactersSelector(),
                effect: new DestroyEffect()
            );

            player2.Hand.AddCard(card1);
            player2.Hand.AddCard(card2);
            player2.Hand.AddCard(card3);

            player1.Deck.AddCard(card4);
            player1.Deck.AddCard(card5);
            player1.Deck.AddCard(card6);
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
