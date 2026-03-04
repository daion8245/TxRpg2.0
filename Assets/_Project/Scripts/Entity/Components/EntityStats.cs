using TxRpg.Data;
using UnityEngine;

namespace TxRpg.Entity
{
    /// <summary>
    /// 배틀 중 유닛의 런타임 스탯을 관리합니다.
    /// UnitDataSO에서 초기화되며, 버프/데미지에 의해 변동됩니다.
    /// </summary>
    public class EntityStats
    {
        public int MaxHp { get; private set; }
        public int CurrentHp { get; set; }
        public int MaxMp { get; private set; }
        public int CurrentMp { get; set; }
        public int Atk { get; set; }
        public int Def { get; set; }
        public int Spd { get; set; }
        public int Kd { get; set; }
        public int Kr { get; set; }

        // 버프에 의한 보정치
        private int _atkModifier;
        private int _defModifier;
        private int _spdModifier;

        public int EffectiveAtk => Atk + _atkModifier;
        public int EffectiveDef => Def + _defModifier;
        public int EffectiveSpd => Spd + _spdModifier;

        public EntityStats(UnitDataSO data)
        {
            MaxHp = data.baseHp;
            CurrentHp = data.baseHp;
            MaxMp = data.baseMp;
            CurrentMp = data.baseMp;
            Atk = data.baseAtk;
            Def = data.baseDef;
            Spd = data.baseSpd;
            Kd = data.baseKd;
            Kr = data.baseKr;
        }

        public void ApplyDamage(int damage)
        {
            CurrentHp = Mathf.Max(0, CurrentHp - damage);
        }

        public void Heal(int amount)
        {
            CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);
        }

        public void ConsumeMp(int amount)
        {
            CurrentMp = Mathf.Max(0, CurrentMp - amount);
        }

        public void RestoreMp(int amount)
        {
            CurrentMp = Mathf.Min(MaxMp, CurrentMp + amount);
        }

        public void AddModifier(int atk, int def, int spd)
        {
            _atkModifier += atk;
            _defModifier += def;
            _spdModifier += spd;
        }

        public void RemoveModifier(int atk, int def, int spd)
        {
            _atkModifier -= atk;
            _defModifier -= def;
            _spdModifier -= spd;
        }
    }
}
