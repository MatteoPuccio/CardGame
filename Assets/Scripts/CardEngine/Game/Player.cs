using System.Collections.Generic;
using System;
using Assets.Scripts.CardEngine.Board;
using Assets.Scripts.CardEngine.Cards;
using static Assets.Scripts.CardEngine.Utils.Constants;

namespace Assets.Scripts.CardEngine.Game
{
    public class Player : ITargetable
    {
        public TargetableKind Kind => TargetableKind.Player;

        public string Name;
        public event Action<uint> LifeChanged;

        private uint _life;
        public uint Life
        {
            get => _life;
            set
            {
                if (_life == value)
                    return;
                _life = value;
                LifeChanged?.Invoke(_life);
            }
        }
        public Deck Deck;
        public ExtraDeck ExtraDeck;
        public Hand Hand;
        public Cemetery Cemetery;
        public RitualZone Rituals;
        public List<PlayAreaZone> PlayZones;
        public bool IsLocalPlayer;
        public int DeployPointsPerTurn = DEFAULT_DEPLOY_POINTS_PER_TURN;

        public event Action<int> DeployPointsChanged;

        private int _deployPoints;
        public int DeployPoints
        {
            get => _deployPoints;
            set
            {
                if (_deployPoints == value)
                    return;
                _deployPoints = value;
                DeployPointsChanged?.Invoke(_deployPoints);
            }
        }

        public Player(string name, bool isLocalPlayer)
        {
            Name = name;
            Life = STARTING_LIFE_VALUE;
            IsLocalPlayer = isLocalPlayer;
            DeployPoints = DeployPointsPerTurn;
        }
    }

    
}