using UnityEngine;
using Assets.Scripts.CardEngine.Game;


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
        private readonly PlayAreaController _playAreaController;
        public DeckController DeckController => _deckController;
        public HandController HandController => _handController;
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
            Vector3 boardPosition = GetPlayerBoardPosition(gameController.Board, gameController.PlayerBoardPrefab, player.IsLocalPlayer);

            if (player.IsLocalPlayer)
            {
                _boardInstance = GameObject.Instantiate(
                    original: gameController.PlayerBoardPrefab,
                    position: boardPosition,
                    rotation: UnityEngine.Quaternion.identity,
                    parent: gameController.Board.transform
                );
                _boardInstance.transform.Find("PlayArea").gameObject.tag = PlayerArea.Local.ToString();
            }
            else
            {
                _boardInstance = GameObject.Instantiate(
                    original: gameController.PlayerBoardPrefab,
                    position: boardPosition,
                    rotation: UnityEngine.Quaternion.Euler(0, 180, 0),
                    parent: gameController.Board.transform
                );
                _boardInstance.transform.Find("PlayArea").gameObject.tag = PlayerArea.Opponent.ToString();
            }

            _handController = _boardInstance.GetComponentInChildren<HandController>();
            _deckController = _boardInstance.GetComponentInChildren<DeckController>();
            _playAreaController = _boardInstance.GetComponentInChildren<PlayAreaController>();
            if (_handController != null)
                _handController.GameController = gameController;
            if (_deckController != null)
                _deckController.GameController = gameController;
            if (_playAreaController != null)
                _playAreaController.Initialize(gameController);
            
            if (_handController == null)
            {
                Debug.LogError("PlayerBoard: HandController component not found in PlayerBoard prefab.");
            }
            if (_deckController == null)
            {
                Debug.LogError("PlayerBoard: DeckController component not found in PlayerBoard prefab.");
            }
            if (_playAreaController == null)
            {
                Debug.LogError("PlayerBoard: PlayAreaZoneController component not found in PlayerBoard prefab.");
            }
        }

    }
}