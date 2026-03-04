using Cysharp.Threading.Tasks;
using System.Threading;
using TxRpg.Battle;
using TxRpg.Data;

namespace TxRpg.Skill.Actions
{
    public class ApplyDamageAction : ITimelineAction
    {
        private readonly int _multiplierPercent;

        public ApplyDamageAction(SkillActionData data)
        {
            _multiplierPercent = data.damageMultiplierPercent;
        }

        public async UniTask Execute(SkillContext context, CancellationToken ct = default)
        {
            foreach (var target in context.Targets)
            {
                if (!target.IsAlive) continue;

                var result = DamageCalculator.Calculate(
                    context.Caster.Stats, target.Stats, context.SkillData);

                // 추가 배율 적용
                int finalDamage = (int)(result.FinalDamage * (_multiplierPercent / 100f));

                if (finalDamage > 0)
                    target.TakeDamage(finalDamage, result.IsCritical);
                else
                    target.Heal(-finalDamage);
            }

            await UniTask.CompletedTask;
        }
    }
}
