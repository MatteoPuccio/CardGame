using System;
using UnityEngine;
using TMPro;
using Assets.Scripts.CardEngine.Game;


namespace Assets.Scripts.CardEngine.Board
{

    public class DeployPointsView : MonoBehaviour
    {
        [SerializeField] private TextMeshPro _deployPointsText;

        private Player _player;

        public void Bind(Player player)
        {
            if (_player == player)
                return;

            Unsubscribe();
            _player = player;
            Subscribe();
            SetDeployPoints(_player != null ? _player.DeployPoints : 0);
        }

        public void SetDeployPoints(int deployPoints)
        {
            if (_deployPointsText == null)
                return;
            _deployPointsText.text = deployPoints.ToString();
        }   

        private void OnEnable()
        {
            Subscribe();
            if (_player != null)
                SetDeployPoints(_player.DeployPoints);
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (_player == null)
                return;
            _player.DeployPointsChanged += OnDeployPointsChanged;
        }

        private void Unsubscribe()
        {
            if (_player == null)
                return;
            _player.DeployPointsChanged -= OnDeployPointsChanged;
        }

        private void OnDeployPointsChanged(int newValue)
        {
            SetDeployPoints(newValue);
        }
    }
}