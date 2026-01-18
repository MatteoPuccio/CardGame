using TMPro;
using UnityEngine;
using Assets.Scripts.CardEngine.Game;

namespace Assets.Scripts.CardEngine.Board
{
    public sealed class LifePointsView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _lifeText;

        private Player _player;

        public void Bind(Player player)
        {
            if (_player == player)
                return;

            Unsubscribe();
            _player = player;
            Subscribe();
            SetLife(_player != null ? _player.Life : 0);
        }

        public void SetLife(uint life)
        {
            if (_lifeText == null)
                return;

            _lifeText.text = life.ToString();
        }

        private void OnEnable()
        {
            Subscribe();
            if (_player != null)
                SetLife(_player.Life);
        }

        private void OnDisable() => Unsubscribe();

        private void OnDestroy() => Unsubscribe();

        private void Subscribe()
        {
            if (_player == null)
                return;

            _player.LifeChanged += OnLifeChanged;
        }

        private void Unsubscribe()
        {
            if (_player == null)
                return;

            _player.LifeChanged -= OnLifeChanged;
        }

        private void OnLifeChanged(uint newValue) => SetLife(newValue);
    }
}
