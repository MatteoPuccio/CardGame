using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Assets.Scripts.SceneFlow
{
    /// <summary>
    /// Simple UI hook for menus:
    /// - Load the main gameplay scene ("Play" button)
    /// - Restart the current scene ("Restart" button)
    ///
    /// You can either:
    /// - Assign buttons and it auto-wires listeners, OR
    /// - Leave buttons null and hook the public methods via the Inspector.
    /// </summary>
    public sealed class MainSceneUIActions : MonoBehaviour
    {
        [Header("Main/Game Scene")]
        [SerializeField] private string _mainSceneName;

#if UNITY_EDITOR
        [SerializeField] private SceneAsset _mainScene;
#endif

        [Header("Optional Buttons (auto-wire)")]
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _restartButton;

        private void OnEnable()
        {
            if (_playButton != null)
                _playButton.onClick.AddListener(LoadMainScene);
            if (_restartButton != null)
                _restartButton.onClick.AddListener(RestartCurrentScene);
        }

        private void OnDisable()
        {
            if (_playButton != null)
                _playButton.onClick.RemoveListener(LoadMainScene);
            if (_restartButton != null)
                _restartButton.onClick.RemoveListener(RestartCurrentScene);
        }

        public void LoadMainScene()
        {
            if (string.IsNullOrWhiteSpace(_mainSceneName))
            {
                Debug.LogError("MainSceneUIActions: Main scene name is not set.");
                return;
            }

            SceneManager.LoadScene(_mainSceneName, LoadSceneMode.Single);
        }

        public void RestartCurrentScene()
        {
            var scene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(scene.name, LoadSceneMode.Single);
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            if (_mainScene != null)
                _mainSceneName = _mainScene.name;
#endif
        }
    }
}
