
using System;
using Assets.Scripts.CardEngine.Game;
using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Events;
using Assets.Scripts.CardEngine.Board;
using Assets.Scripts.CardEngine.UI;
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
        [SerializeField] private RapidEffectPromptUI _rapidEffectPromptUI;

        [Header("Card Data (ScriptableObjects)")]
        [SerializeField] private ScriptableDeck _player1Deck;
        [SerializeField] private ScriptableDeck _player2Deck;

        [Header("Debug")]
        [SerializeField] private bool _autoActivateFirstRapidEffect;
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
        public RapidEffectChainSystem RapidEffectChain => _rapidEffectChain;
        private EventBus EventBus;

        private PlayerBoard _playerBoard1;
        private PlayerBoard _playerBoard2;

        private TurnFlow _turnFlow;
        private RapidEffectChainSystem _rapidEffectChain;

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
            EventBus.Subscribe<CardMovedEvent>(OnCardMoved);
            EventBus.Subscribe<CardMoveFailedEvent>(OnCardMoveFailed);
            EventBus.Subscribe<PhaseChangedEvent>(OnPhaseChanged);
			EventBus.Subscribe<TroopDamagedEvent>(OnTroopDamaged);
			EventBus.Subscribe<TroopDiedEvent>(OnTroopDied);
        }

        private static void OnCardPlayed(CardPlayedEvent e)
        {
            if (e == null) return;

            string playerName = e.Player != null ? e.Player.Name : "<null>";
            string cardName = e.Source != null ? e.Source.Name : "<null>";
            Debug.Log($"[CardPlayed] Player {playerName}: {cardName}");
        }

        private static void OnCardMoved(CardMovedEvent e)
        {
            if (e == null) return;

            string playerName = e.Player != null ? e.Player.Name : "<null>";
            string cardName = e.Source != null ? e.Source.Name : "<null>";
            Debug.Log($"[CardMoved] {playerName}: {cardName} {e.From} -> {e.To}");
        }

        private static void OnCardMoveFailed(CardMoveFailedEvent e)
        {
            if (e == null) return;

            string playerName = e.Player != null ? e.Player.Name : "<null>";
            string cardName = e.Source != null ? e.Source.Name : "<null>";
            Debug.Log($"[CardMoveFailed] {playerName}: {cardName} {e.From} -> {e.To}");
        }

        private static void OnPhaseChanged(PhaseChangedEvent e)
        {
            if (e == null) return;

            string playerName = e.ActivePlayer != null ? e.ActivePlayer.Name : "<null>";
            Debug.Log($"[PhaseChanged] Turn {e.TurnNumber} ({playerName}): {e.FromPhase} -> {e.ToPhase}");
        }

        private static void OnTroopDamaged(TroopDamagedEvent e)
        {
            if (e?.Source == null)
                return;

            string instigator = e.Instigator != null ? e.Instigator.Name : "<none>";
            int hp = (e.Source.Behavior is TroopBehavior troop) ? troop.Health : -1;
            Debug.Log($"[TroopDamaged] {e.Source.Name} took {e.Amount} (from {instigator}), HP now {hp}");
        }

        private static void OnTroopDied(TroopDiedEvent e)
        {
            if (e?.Source == null)
                return;

            string instigator = e.Instigator != null ? e.Instigator.Name : "<none>";
            Debug.Log($"[TroopDied] {e.Source.Name} (from {instigator}), movedToCemetery={e.MovedToCemetery}");
        }

        private void StartGame()
        {
            Player player1 = new(name: "Alice", isLocalPlayer: true);
            Player player2 = new(name: "Bob", isLocalPlayer: false);
            _playerBoard1 = new PlayerBoard(player: player1, gameController: this);
            _playerBoard2 = new PlayerBoard(player: player2, gameController: this);

            _gameState.AddPlayers(player1, player2);

            LoadDeckFromScriptable(player1, _player1Deck, _gameState);
            LoadDeckFromScriptable(player2, _player2Deck, _gameState);

            _rapidEffectChain = new RapidEffectChainSystem(_gameState)
            {
                Prompter = _rapidEffectPromptUI != null
                    ? _rapidEffectPromptUI
                    : new AutoPassRapidEffectPrompter()
            };
            _gameState.RapidEffectChain = _rapidEffectChain;
            _rapidEffectChain.Bind();

            if (_rapidEffectPromptUI == null)
                Debug.LogWarning("GameController: RapidEffectPromptUI is not assigned; rapid effects will auto-pass and no UI will show.");
        }

        private static void LoadDeckFromScriptable(Player owner, ScriptableDeck deck, GameState gameState)
        {
            if (owner?.Deck == null)
            {
                Debug.LogError("GameController: Cannot load deck; owner or owner.Deck is null.");
                return;
            }

            if (deck == null)
            {
                Debug.LogWarning($"GameController: No ScriptableDeck assigned for player '{owner?.Name ?? "<null>"}'.");
                return;
            }

            var entries = deck.Cards;
            if (entries == null || entries.Count == 0)
            {
                Debug.LogWarning($"GameController: ScriptableDeck '{deck.name}' is empty.");
                return;
            }

            foreach (var entry in entries)
            {
                if (entry == null)
                    continue;

                var cardAsset = entry.Card;
                if (cardAsset == null)
                {
                    Debug.LogWarning($"GameController: Deck '{deck.name}' contains a null card reference.");
                    continue;
                }

                int count = entry.Count <= 0 ? 1 : entry.Count;
                AddCopiesToDeck(owner, cardAsset, count, gameState);
            }
        }

        private static void AddCopiesToDeck(Player owner, ScriptableCard cardAsset, int count, GameState gameState)
        {
            string baseId = string.IsNullOrWhiteSpace(cardAsset.id) ? cardAsset.name : cardAsset.id;

            for (int i = 0; i < count; i++)
            {
                var card = cardAsset.CreateRuntimeCard(owner, gameState);
                if (card == null)
                    continue;

                if (count > 1)
                    card.Id = $"{baseId}_{i + 1}";

                owner.Deck.AddCard(card);
            }
        }

    }
}
