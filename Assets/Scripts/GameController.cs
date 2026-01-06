
using System;
using System.Collections.Generic;
using Assets.Scripts.CardEngine.Effects;
using Assets.Scripts.CardEngine.Game;
using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Events;
using Assets.Scripts.CardEngine.Board;
using UnityEngine;
using UnityEngine.UI;


namespace Assets.Scripts
{
    public class GameController : MonoBehaviour
    {
        [SerializeField] private CardFactory _cardFactory;
        [SerializeField] private GameObject _playerBoardPrefab;
        [SerializeField] private GameObject _board;
        [SerializeField] private Transform _gameplayRoot;

        [Header("UI")]
        [SerializeField] private ScrollRect _localCemeteryScrollRect;
        [SerializeField] private ScrollRect _opponentCemeteryScrollRect;
        private GameState _gameState;
        public CardFactory CardFactory => _cardFactory;
        public CardViewRegistry CardViewRegistry { get; private set; }
        public GameObject Board => _board;
        public Transform GameplayRoot => _gameplayRoot;
        public GameObject PlayerBoardPrefab => _playerBoardPrefab;
        public GameState GameState => _gameState;
		public ScrollRect LocalCemeteryScrollRect => _localCemeteryScrollRect;
		public ScrollRect OpponentCemeteryScrollRect => _opponentCemeteryScrollRect;
        public TurnFlow TurnFlow => _turnFlow;
        private EventBus EventBus;

        private PlayerBoard _playerBoard1;
        private PlayerBoard _playerBoard2;

        private TurnFlow _turnFlow;

        public PlayerBoard PlayerBoard1 => _playerBoard1;
        public PlayerBoard PlayerBoard2 => _playerBoard2;



        void Start()
        {
            EnsureGameplayRoot();

            // Initialize core systems
            EventBus = new EventBus();
            _gameState = new GameState(EventBus);
            CardViewRegistry = new CardViewRegistry();

            RegisterEventHandlers();
            StartGame();

            _turnFlow = new TurnFlow(_gameState);
            _turnFlow.BeginGame();
        }

        private void EnsureGameplayRoot()
        {
            if (_gameplayRoot != null)
                return;

            var root = new GameObject("GameplayRoot");
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            root.transform.localScale = Vector3.one;
            _gameplayRoot = root.transform;

            _cardFactory.SetSpawnParent(_gameplayRoot);
        }


        private void RegisterEventHandlers()
        {
            EventBus.Subscribe<CardPlayedEvent>(OnCardPlayed);
            EventBus.Subscribe<PhaseChangedEvent>(OnPhaseChanged);
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

        private static void OnPhaseChanged(PhaseChangedEvent e)
        {
            if (e == null) return;

            string playerName = e.ActivePlayer != null ? e.ActivePlayer.Name : "<null>";
            Debug.Log($"[PhaseChanged] Turn {e.TurnNumber} ({playerName}): {e.FromPhase} -> {e.ToPhase}");
        }

        private void StartGame()
        {
            Player player1 = new(name: "Alice", isLocalPlayer: false);
            Player player2 = new(name: "Bob", isLocalPlayer: true);
            _playerBoard1 = new PlayerBoard(player: player1, gameController: this);
            _playerBoard2 = new PlayerBoard(player: player2, gameController: this);

            _gameState.AddPlayers(player1, player2);

            // Create and add sample cards to each deck
            Card card1 = new(id: "1", name: "Fireball", cardCategory: CardType.Spell, owner: player1, gameState: _gameState);
            Card card2 = new(id: "2", name: "Goblin", cardCategory: CardType.Troop, owner: player1, gameState: _gameState);
            Card card3 = new(id: "3", name: "Heal", cardCategory: CardType.Spell, owner: player1, gameState: _gameState);
            Card card4 = new(id: "4", name: "Frostbolt", cardCategory: CardType.Spell, owner: player2, gameState: _gameState);
            Card card5 = new(id: "5", name: "Dwarf", cardCategory: CardType.Troop, owner: player2, gameState: _gameState);
            Card card6 = new(
                id: "6",
                name: "Raigeki",
                effectText: "Destroy all troops your opponent controls.",
                cardCategory: CardType.Spell,
                owner: player2,
                gameState: _gameState
            )
            {
                OnPlayEffect = new TargetedEffect(
                    selector: new AllEnemyCharactersSelector(),
                    effect: new DestroyEffect()
                )
            };

            player1.Deck.AddCard(card1);
            player1.Deck.AddCard(card2);
            player1.Deck.AddCard(card3);

            player2.Deck.AddCard(card4);
            player2.Deck.AddCard(card5);
            player2.Deck.AddCard(card6);
        }

    }
}
