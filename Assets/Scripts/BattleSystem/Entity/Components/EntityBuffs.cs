using System.Collections.Generic;

namespace TxRpg.Entity
{
    /// <summary>
    /// 유닛의 버프/디버프를 관리합니다.
    /// 턴 시작 시 TickBuffs()를 호출하여 지속시간을 감소시킵니다.
    /// </summary>
    public class EntityBuffs
    {
        private readonly List<ActiveBuff> _buffs = new();
        private readonly EntityStats _stats;

        public IReadOnlyList<ActiveBuff> Buffs => _buffs;

        public EntityBuffs(EntityStats stats)
        {
            _stats = stats;
        }

        public void AddBuff(BuffData buffData)
        {
            var activeBuff = new ActiveBuff(buffData);
            _buffs.Add(activeBuff);
            _stats.AddModifier(buffData.AtkModifier, buffData.DefModifier, buffData.SpdModifier);
        }

        public void TickBuffs()
        {
            for (int i = _buffs.Count - 1; i >= 0; i--)
            {
                _buffs[i].RemainingTurns--;

                if (_buffs[i].RemainingTurns <= 0)
                {
                    var data = _buffs[i].Data;
                    _stats.RemoveModifier(data.AtkModifier, data.DefModifier, data.SpdModifier);
                    _buffs.RemoveAt(i);
                }
            }
        }

        public void ClearAll()
        {
            foreach (var buff in _buffs)
            {
                var data = buff.Data;
                _stats.RemoveModifier(data.AtkModifier, data.DefModifier, data.SpdModifier);
            }
            _buffs.Clear();
        }
    }

    [System.Serializable]
    public class BuffData
    {
        public string BuffName;
        public int Duration;
        public int AtkModifier;
        public int DefModifier;
        public int SpdModifier;
        public int HpPerTurn; // 양수: 회복, 음수: 도트 데미지
    }

    public class ActiveBuff
    {
        public BuffData Data { get; }
        public int RemainingTurns { get; set; }

        public ActiveBuff(BuffData data)
        {
            Data = data;
            RemainingTurns = data.Duration;
        }
    }
}
