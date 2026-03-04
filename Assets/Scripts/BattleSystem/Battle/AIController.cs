using System.Collections.Generic;
using System.Linq;
using TxRpg.Data;
using TxRpg.Entity;
using UnityEngine;

namespace TxRpg.Battle
{
    public class AIController
    {
        public BattleAction DecideAction(BattleEntity enemy, List<BattleEntity> allies, List<BattleEntity> opponents)
        {
            var availableSkills = enemy.Skills.GetUsableSkills(enemy.Stats.CurrentMp);
            SkillDataSO chosenSkill;

            if (availableSkills.Count > 0)
            {
                chosenSkill = availableSkills[Random.Range(0, availableSkills.Count)];
            }
            else
            {
                // MP가 없으면 기본 공격 (첫 번째 스킬을 기본 공격으로 간주)
                chosenSkill = enemy.Skills.AllSkills.Length > 0
                    ? enemy.Skills.AllSkills[0]
                    : null;
            }

            var targets = SelectTargets(chosenSkill, enemy, allies, opponents);

            return new BattleAction
            {
                Caster = enemy,
                Targets = targets,
                Skill = chosenSkill,
                Type = chosenSkill != null ? ActionType.Skill : ActionType.Attack
            };
        }

        private List<BattleEntity> SelectTargets(SkillDataSO skill, BattleEntity caster,
            List<BattleEntity> allies, List<BattleEntity> opponents)
        {
            if (skill == null)
            {
                var aliveOpponents = opponents.Where(e => e.IsAlive).ToList();
                return aliveOpponents.Count > 0
                    ? new List<BattleEntity> { aliveOpponents[Random.Range(0, aliveOpponents.Count)] }
                    : new List<BattleEntity>();
            }

            switch (skill.targetType)
            {
                case TargetType.SingleEnemy:
                    var aliveEnemies = opponents.Where(e => e.IsAlive).ToList();
                    return aliveEnemies.Count > 0
                        ? new List<BattleEntity> { aliveEnemies[Random.Range(0, aliveEnemies.Count)] }
                        : new List<BattleEntity>();

                case TargetType.AllEnemies:
                    return opponents.Where(e => e.IsAlive).ToList();

                case TargetType.SingleAlly:
                    var aliveAllies = allies.Where(e => e.IsAlive).ToList();
                    return aliveAllies.Count > 0
                        ? new List<BattleEntity> { aliveAllies[Random.Range(0, aliveAllies.Count)] }
                        : new List<BattleEntity>();

                case TargetType.AllAllies:
                    return allies.Where(e => e.IsAlive).ToList();

                case TargetType.Self:
                    return new List<BattleEntity> { caster };

                default:
                    return new List<BattleEntity>();
            }
        }
    }
}
