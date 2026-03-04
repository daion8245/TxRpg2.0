using System.Collections.Generic;
using TxRpg.Data;
using TxRpg.Entity;

namespace TxRpg.Skill
{
    public class SkillContext
    {
        public BattleEntity Caster { get; }
        public List<BattleEntity> Targets { get; }
        public SkillDataSO SkillData { get; }

        public SkillContext(BattleEntity caster, List<BattleEntity> targets, SkillDataSO skillData)
        {
            Caster = caster;
            Targets = targets;
            SkillData = skillData;
        }
    }
}
