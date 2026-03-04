using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TxRpg.Data;
using TxRpg.Entity;
using UnityEngine;

namespace TxRpg.Skill
{
    public class SkillExecutor
    {
        private readonly TimelineDirector _director = new();

        public async UniTask ExecuteSkill(BattleEntity caster, List<BattleEntity> targets,
            SkillDataSO skill, CancellationToken ct = default)
        {
            Debug.Log($"[Skill] {caster.UnitData.unitName} 사용: {skill.skillName}");

            var context = new SkillContext(caster, targets, skill);

            if (skill.timeline != null && skill.timeline.Length > 0)
            {
                await _director.PlayTimeline(skill.timeline, context, ct);
            }
            else
            {
                // 타임라인 미정의 시 기본 연출
                await DefaultSkillSequence(context, ct);
            }
        }

        private async UniTask DefaultSkillSequence(SkillContext context, CancellationToken ct)
        {
            // 공격 애니메이션
            await context.Caster.Animator.PlayAttackAsync(ct);

            // 데미지 적용
            foreach (var target in context.Targets)
            {
                var result = Battle.DamageCalculator.Calculate(
                    context.Caster.Stats, target.Stats, context.SkillData);

                if (result.FinalDamage > 0)
                    target.TakeDamage(result.FinalDamage, result.IsCritical);
                else
                    target.Heal(-result.FinalDamage);

                // 피격 연출
                await target.Animator.PlayHitAsync(ct);
            }
        }
    }
}
