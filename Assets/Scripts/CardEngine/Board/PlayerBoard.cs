using System;
using UnityEngine;
using Assets.Scripts.CardEngine.Game;
using UnityEngine.UI;


namespace Assets.Scripts.CardEngine.Board
{
    public class PlayerArea
    {
        private PlayerArea(string value) { Value = value; }

        public string Value { get; private set; }

        public static PlayerArea Local   { get { return new PlayerArea("LocalArea"); } }
        public static PlayerArea Opponent   { get { return new PlayerArea("OpponentArea"); } }

        public override string ToString()
        {
            return Value;
        }
    }

    public class PlayerBoard
    {
        private readonly GameObject _boardInstance;
        private readonly HandController _handController;
        private readonly DeckController _deckController;
        private readonly CemeteryController _cemeteryController;
        private readonly PlayAreaController _playAreaController;
        private readonly Player _player;
        private readonly GameController _gameController;
        public string PlayerAreaTag => _player.IsLocalPlayer
            ? PlayerArea.Local.ToString()
            : PlayerArea.Opponent.ToString();
        public Player Player => _player;
        public DeckController DeckController => _deckController;
        public HandController HandController => _handController;
        public CemeteryController CemeteryController => _cemeteryController;
        public PlayAreaController PlayAreaController => _playAreaController;
        public GameObject BoardInstance => _boardInstance;


        private static Vector3 GetPlayerBoardPosition(GameObject mainBoard, GameObject playerBoard, bool isLocalPlayer)
        {
            var mainBoardRenderer = mainBoard.GetComponent<Renderer>();
            float mainBoardTopY = 0f;
            float mainBoardCenterZ = 0f;
            float mainBoardExtentZ = 0f;
            if (mainBoardRenderer != null)
            {
                mainBoardTopY = mainBoardRenderer.bounds.max.y;
                mainBoardCenterZ = mainBoardRenderer.bounds.center.z;
                mainBoardExtentZ = mainBoardRenderer.bounds.extents.z;
            }

            var playerBoardRenderer = playerBoard.GetComponent<Renderer>();
            float playerBoardTopOffset = 0f;
            if (playerBoardRenderer != null)
                playerBoardTopOffset = playerBoardRenderer.transform.position.y - playerBoardRenderer.bounds.min.y;

            float y = mainBoardTopY - playerBoardTopOffset;
            float z = 0f;
            if (isLocalPlayer)
            {
                // Center on positive third (front third) of the main board
                z = mainBoardCenterZ + mainBoardExtentZ / 3f;
            }
            else
            {
                // Center on negative third (back third) of the main board
                z = mainBoardCenterZ - mainBoardExtentZ / 3f;
            }
            return new Vector3(0, y, z);
        }

        public PlayerBoard(Player player, GameController gameController)
        {
            _player = player ?? throw new ArgumentNullException(nameof(player));
            _gameController = gameController ?? throw new ArgumentNullException(nameof(gameController));

            if (gameController.Board == null)
                throw new InvalidOperationException("PlayerBoard: GameController.Board is not assigned.");
            if (gameController.PlayerBoardPrefab == null)
                throw new InvalidOperationException("PlayerBoard: GameController.PlayerBoardPrefab is not assigned.");
            if (gameController.GameplayRoot == null)
                throw new InvalidOperationException("PlayerBoard: GameController.GameplayRoot is not assigned.");

            Vector3 boardPosition = GetPlayerBoardPosition(gameController.Board, gameController.PlayerBoardPrefab, player.IsLocalPlayer);

            if (player.IsLocalPlayer)
            {
                _boardInstance = GameObject.Instantiate(
                    original: gameController.PlayerBoardPrefab,
                    position: boardPosition,
                    rotation: UnityEngine.Quaternion.identity,
                    parent: gameController.GameplayRoot
                );
            }
            else
            {
                _boardInstance = GameObject.Instantiate(
                    original: gameController.PlayerBoardPrefab,
                    position: boardPosition,
                    rotation: UnityEngine.Quaternion.Euler(0, 180, 0),
                    parent: gameController.GameplayRoot
                );
            }

            var playAreaTransform = _boardInstance.transform.Find("PlayArea");
            if (playAreaTransform != null)
                playAreaTransform.gameObject.tag = PlayerAreaTag;
            else
                Debug.LogWarning("PlayerBoard: Could not find child named 'PlayArea' to tag.");

            _handController = _boardInstance.GetComponentInChildren<HandController>();
            _deckController = _boardInstance.GetComponentInChildren<DeckController>();
            _cemeteryController = _boardInstance.GetComponentInChildren<CemeteryController>();
            _playAreaController = _boardInstance.GetComponentInChildren<PlayAreaController>();

            if (_handController == null)
                throw new InvalidOperationException("PlayerBoard: HandController component not found in PlayerBoard prefab.");
            if (_deckController == null)
                throw new InvalidOperationException("PlayerBoard: DeckController component not found in PlayerBoard prefab.");
            if (_cemeteryController == null)
                throw new InvalidOperationException("PlayerBoard: CemeteryController component not found in PlayerBoard prefab.");
            if (_playAreaController == null)
                throw new InvalidOperationException("PlayerBoard: PlayAreaController component not found in PlayerBoard prefab.");

            // Dependency wiring first (avoid controllers running before they have references).
            _handController.GameController = gameController;
            _deckController.GameController = gameController;
            _cemeteryController.GameController = gameController;
            _playAreaController.Initialize(gameController);

            CreateHand();
            CreateDeck();
            CreateCemetery();

            if (_playAreaController.PlayArea == null)
                throw new InvalidOperationException("PlayerBoard: PlayAreaController.PlayArea is null after initialization.");

            player.PlayZones = _playAreaController.PlayArea.Zones;
        }

        private void CreateHand()
        {
            _player.Hand = new Hand(owner: _player, gameState: _gameController.GameState);
            
            _handController.Initialize(_player.Hand);
        }


        private void CreateDeck()
        {
            _player.Deck = new Deck(owner: _player, gameState: _gameController.GameState);

            _deckController.Initialize(_player.Deck);
        }

        private void CreateCemetery()
        {
            _player.Cemetery = new Cemetery(owner: _player, gameState: _gameController.GameState);
            
            _cemeteryController.Initialize(_player.Cemetery);
            ScrollRect owned = _player.IsLocalPlayer
                    ? _gameController.LocalCemeteryScrollRect
                    : _gameController.OpponentCemeteryScrollRect;

            _cemeteryController.BindScrollRects(owned, startDisabled: true);

        }

    }
}