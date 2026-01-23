
using System;
using System.IO;
using Assets.Scripts.CardEngine.Game;
using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Events;
using Assets.Scripts.CardEngine.Board;
using Assets.Scripts.CardEngine.UI;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Assets.Scripts.CardEngine.Utils;


#if UNITY_EDITOR
using UnityEditor;
#endif


namespace Assets.Scripts
{
    public class GameController : MonoBehaviour
    {
        [SerializeField] private CardFactory _cardFactory;
        [SerializeField] private GameObject _playerBoardPrefab;
        [SerializeField] private GameObject _board;
        private Transform _gameplayRoot;

        [Header("UI")]
        [SerializeField] private ScrollRect _localCemeteryScrollRect;
        [SerializeField] private ScrollRect _opponentCemeteryScrollRect;
        [SerializeField] private RapidEffectPromptUI _rapidEffectPromptUI;
        [SerializeField] private OptionalEffectPromptUI _optionalEffectPromptUI;
        [SerializeField] private SelectCardFromZonePromptUI _selectCardFromZonePromptUI;

        [Header("Card Data (ScriptableObjects)")]
        [SerializeField] private ScriptableDeck _player1Deck;
        [SerializeField] private ScriptableDeck _player2Deck;

        [Header("Scene Flow")]
        [Tooltip("If set, this scene will be loaded when a player is defeated. If empty, the current scene is reloaded.")]
    #if UNITY_EDITOR
        [SerializeField] private SceneAsset _onDefeatLoadSceneAsset;
    #endif
        [SerializeField, HideInInspector] private string _onDefeatLoadScene;

        private bool _isEndingMatch;
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
			EventBus.Subscribe<PlayerDefeatedEvent>(OnPlayerDefeated);
        }

        private void OnPlayerDefeated(PlayerDefeatedEvent e)
        {
            if (_isEndingMatch)
                return;

            var defeated = e?.DefeatedPlayer;
            if (defeated == null)
                return;

            _isEndingMatch = true;

            // Best-effort cleanup to avoid UI continuing to act during transition.
            _gameState?.Targeting?.Cancel("Match ended.");
            _gameState?.Attack?.EndAttackPhase("Match ended.");

            string next = _onDefeatLoadScene;
            if (string.IsNullOrWhiteSpace(next))
            {
                RestartMatch();
                return;
            }

            SceneManager.LoadScene(next, LoadSceneMode.Single);
        }

        public void RestartMatch()
        {
            if (!isActiveAndEnabled)
                return;

            var scene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(scene.name, LoadSceneMode.Single);
        }

        public void LoadScene(string sceneName)
        {
            if (!isActiveAndEnabled)
                return;

            if (string.IsNullOrWhiteSpace(sceneName))
                return;

            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
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
            for(int i = 0; i < Constants.STARTING_HAND_SIZE; i++)
            {
                player1.Deck.DrawTop();
                player2.Deck.DrawTop();
            }

            _rapidEffectChain = new RapidEffectChainSystem(_gameState)
            {
                Prompter = _rapidEffectPromptUI != null
                    ? _rapidEffectPromptUI
                    : new AutoPassRapidEffectPrompter()
            };
            _gameState.RapidEffectChain = _rapidEffectChain;
            _rapidEffectChain.Bind();

            _gameState.OptionalEffectPrompter = _optionalEffectPromptUI != null
                ? _optionalEffectPromptUI
                : new AutoDeclineOptionalEffectPrompter();

            _gameState.SelectCardFromZonePrompter = _selectCardFromZonePromptUI != null
                ? _selectCardFromZonePromptUI
                : new AutoCancelSelectCardFromZonePrompter();

            if (_rapidEffectPromptUI == null)
                Debug.LogWarning("GameController: RapidEffectPromptUI is not assigned; rapid effects will auto-pass and no UI will show.");

            if (_optionalEffectPromptUI == null)
                Debug.LogWarning("GameController: OptionalEffectPromptUI is not assigned; optional effects will auto-decline.");

            if (_selectCardFromZonePromptUI == null)
                Debug.LogWarning("GameController: SelectCardFromZonePromptUI is not assigned; card selection prompts will auto-cancel.");
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
            owner.Deck.Shuffle();
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
