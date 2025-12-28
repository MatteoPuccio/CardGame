using UnityEngine;
using Assets.Scripts.CardEngine.Game;


namespace Assets.Scripts
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
        private Player _player;
        private readonly GameObject _boardInstance;

        private readonly GameObject _handArea;
        public GameObject HandArea => _handArea;
        public GameObject BoardInstance => _boardInstance;

        private Vector3 GetPlayerBoardPosition(GameObject mainBoard, GameObject playerBoard, bool isLocalPlayer)
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
            _player = player;
            Vector3 boardPosition = GetPlayerBoardPosition(gameController.BoardObject, gameController.PlayerBoardObject, player.IsLocalPlayer);

            if (player.IsLocalPlayer)
            {
                _boardInstance = GameObject.Instantiate(
                    original: gameController.PlayerBoardObject,
                    position: boardPosition,
                    rotation: UnityEngine.Quaternion.identity,
                    parent: gameController.BoardObject.transform
                );
                _boardInstance.transform.Find("PlayArea").gameObject.tag = PlayerArea.Local.ToString();
            }
            else
            {
                _boardInstance = GameObject.Instantiate(
                    original: gameController.PlayerBoardObject,
                    position: boardPosition,
                    rotation: UnityEngine.Quaternion.Euler(0, 180, 0),
                    parent: gameController.BoardObject.transform
                );
                _boardInstance.transform.Find("PlayArea").gameObject.tag = PlayerArea.Opponent.ToString();
            }
            _handArea = _boardInstance.transform.Find("HandArea").gameObject;
        }

    }
}