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
        private HandController _handController;
        private DeckController _deckController;
        private CemeteryController _cemeteryController;
        private RitualZoneController _ritualZoneController;
        private PlayAreaController _playAreaController;
        private DeployPointsView _deployPointsView;
        private LifePointsView _lifePointsView;
        private readonly Player _player;
        private readonly GameController _gameController;
        public string PlayerAreaTag => _player.IsLocalPlayer
            ? PlayerArea.Local.ToString()
            : PlayerArea.Opponent.ToString();
        public Player Player => _player;
        public DeckController DeckController => _deckController;
        public HandController HandController => _handController;
        public CemeteryController CemeteryController => _cemeteryController;
        public RitualZoneController RitualZoneController => _ritualZoneController;
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
                z = mainBoardCenterZ - mainBoardExtentZ / 3f;
            }
            else
            {
                z = mainBoardCenterZ + mainBoardExtentZ / 3f;
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
            _boardInstance = GameObject.Instantiate(
                    original: gameController.PlayerBoardPrefab,
                    position: boardPosition,
                    rotation: UnityEngine.Quaternion.identity,
                    parent: gameController.GameplayRoot
                );


            var playAreaTransform = _boardInstance.transform.Find("PlayArea");
            if (playAreaTransform != null)
            {
                playAreaTransform.gameObject.tag = PlayerAreaTag;
                var clickView = playAreaTransform.gameObject.GetComponent<PlayerTargetClickView>();
                if (clickView == null)
                    clickView = playAreaTransform.gameObject.AddComponent<PlayerTargetClickView>();
                clickView.Bind(_gameController, _player);
            }
            else
                Debug.LogWarning("PlayerBoard: Could not find child named 'PlayArea' to tag.");
            AssignControllers();

            CreateHand();
            CreateDeck();
            CreateCemetery();
            CreateRitualZone();

            if (_playAreaController.PlayArea == null)
                throw new InvalidOperationException("PlayerBoard: PlayAreaController.PlayArea is null after initialization.");

            player.PlayZones = _playAreaController.PlayArea.Zones;
            RotateOpponentBoard();
        }

        private void AssignControllers()
        {
            _handController = _boardInstance.GetComponentInChildren<HandController>();
            _deckController = _boardInstance.GetComponentInChildren<DeckController>();
            _cemeteryController = _boardInstance.GetComponentInChildren<CemeteryController>();
            _ritualZoneController = _boardInstance.GetComponentInChildren<RitualZoneController>();
            _playAreaController = _boardInstance.GetComponentInChildren<PlayAreaController>();
            _deployPointsView = _boardInstance.GetComponentInChildren<DeployPointsView>();
            _lifePointsView = _boardInstance.GetComponentInChildren<LifePointsView>();

            if (_handController == null)
                throw new InvalidOperationException("PlayerBoard: HandController component not found in PlayerBoard prefab.");
            if (_deckController == null)
                throw new InvalidOperationException("PlayerBoard: DeckController component not found in PlayerBoard prefab.");
            if (_cemeteryController == null)
                throw new InvalidOperationException("PlayerBoard: CemeteryController component not found in PlayerBoard prefab.");
            if (_playAreaController == null)
                throw new InvalidOperationException("PlayerBoard: PlayAreaController component not found in PlayerBoard prefab.");

            _handController.GameController = _gameController;
            _deckController.GameController = _gameController;
            _cemeteryController.GameController = _gameController;
            _playAreaController.Initialize(_gameController);        

            if (_deployPointsView != null)
                _deployPointsView.Bind(_player);

            if (_lifePointsView != null)
                _lifePointsView.Bind(_player);
        }

        private void RotateOpponentBoard()
        {
            if (!_player.IsLocalPlayer) {
                _boardInstance.transform.Rotate(0f, 180f, 0f);
            }
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

        private void CreateRitualZone()
        {
            _player.Rituals = new RitualZone(owner: _player, gameState: _gameController.GameState);

            if (_ritualZoneController == null)
            {
                var playAreaTransform = _boardInstance.transform.Find("PlayArea");
                var parent = playAreaTransform != null ? playAreaTransform : _boardInstance.transform;

                var ritualRoot = new GameObject("RitualZone");
                ritualRoot.transform.SetParent(parent, worldPositionStays: false);
                ritualRoot.transform.localPosition = new Vector3(0f, 0.01f, -0.35f);
                ritualRoot.transform.localRotation = Quaternion.identity;
                ritualRoot.transform.localScale = Vector3.one;

                ritualRoot.AddComponent<RitualZoneView>();

                _ritualZoneController = ritualRoot.AddComponent<RitualZoneController>();
            }

            _ritualZoneController.GameController = _gameController;
            _ritualZoneController.Initialize(_player.Rituals);
        }

    }
}