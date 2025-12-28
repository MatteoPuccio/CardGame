using System.Collections.Generic;
using Assets.Scripts.CardEngine.Cards;

namespace Assets.Scripts.CardEngine.Game
{
    public class TargetResolver
    {
        public List<ITargetable> Resolve(string rule, GameState state, Player player)
        {
            // Example rules: "enemy_character", "all_enemies", "self"
            // Dummy example:
            if (rule == "enemy_character")
            {
                return state.GetEnemyCharacters(player);
            }
    
            return new List<ITargetable>();
        }
    }

}