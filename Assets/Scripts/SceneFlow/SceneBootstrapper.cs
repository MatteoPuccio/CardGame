using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Assets.Scripts.SceneFlow
{
    /// <summary>
    /// Put this in a "boot" scene to immediately load your gameplay scene.
    /// This makes it easy to press Play from a lightweight scene.
    /// </summary>
    public sealed class SceneBootstrapper : MonoBehaviour
    {
        [SerializeField] private string _gameplaySceneName;

        #if UNITY_EDITOR
        [SerializeField] private SceneAsset _gameplayScene;
        #endif
        [SerializeField] private bool _loadOnStart = true;

        private void Start()
        {
            if (!_loadOnStart)
                return;

            LoadGameplayScene();
        }

        public void LoadGameplayScene()
        {
            if (string.IsNullOrWhiteSpace(_gameplaySceneName))
            {
                Debug.LogError("SceneBootstrapper: Gameplay scene name is not set.");
                return;
            }

            SceneManager.LoadScene(_gameplaySceneName, LoadSceneMode.Single);
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            if (_gameplayScene != null)
                _gameplaySceneName = _gameplayScene.name;
#endif
        }
    }
}
