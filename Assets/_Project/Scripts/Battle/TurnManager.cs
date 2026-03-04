using System.Collections.Generic;
using System.Linq;
using TxRpg.Entity;

namespace TxRpg.Battle
{
    public class TurnManager
    {
        public int CurrentTurn { get; private set; }

        public void Reset()
        {
            CurrentTurn = 0;
        }

        public void AdvanceTurn()
        {
            CurrentTurn++;
        }

        public List<BattleEntity> GetActionOrder(List<BattleEntity> allEntities)
        {
            return allEntities
                .Where(e => e.IsAlive)
                .OrderByDescending(e => e.Stats.EffectiveSpd)
                .ThenByDescending(e => e.Stats.Spd)
                .ToList();
        }
    }
}
