using Assets.Scripts.CardEngine.Utils;
using static Assets.Scripts.CardEngine.Utils.Constants;
namespace Assets.Scripts.CardEngine.Game
{
    public class Player
    {
        public string Name;
        public uint Life;
        public Deck Deck;
        public Hand Hand;
        public bool IsLocalPlayer;

        public Player(string name, bool isLocalPlayer)
        {
            Name = name;
            Life = STARTING_LIFE_VALUE;
            IsLocalPlayer = isLocalPlayer;
        }
    }

    
}