using System.Collections.Generic;
using TxRpg.Data;
using TxRpg.Entity;

namespace TxRpg.Battle
{
    public struct BattleAction
    {
        public BattleEntity Caster;
        public List<BattleEntity> Targets;
        public SkillDataSO Skill;
        public ActionType Type;
    }

    public class ActionQueue
    {
        private readonly Queue<BattleAction> _queue = new();

        public int Count => _queue.Count;

        public void Enqueue(BattleAction action)
        {
            _queue.Enqueue(action);
        }

        public BattleAction DequeueNext()
        {
            return _queue.Dequeue();
        }

        public BattleAction Peek()
        {
            return _queue.Peek();
        }

        public void Clear()
        {
            _queue.Clear();
        }
    }
}
