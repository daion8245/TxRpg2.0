using Cysharp.Threading.Tasks;
using System.Linq;
using System.Threading;
using TxRpg.Core;
using TxRpg.Skill;
using UnityEngine;

namespace TxRpg.Battle.Phases
{
    public class ExecutePhase : IState
    {
        private readonly BattleManager _manager;

        public ExecutePhase(BattleManager manager)
        {
            _manager = manager;
        }

        public async UniTask Enter(CancellationToken ct = default)
        {
            Debug.Log("[Battle] === Execute Phase ===");

            while (_manager.ActionQueueInstance.Count > 0)
            {
                var action = _manager.ActionQueueInstance.DequeueNext();

                // 캐스터가 죽었으면 스킵
                if (!action.Caster.IsAlive) continue;

                // 도망 또는 아이템(미구현)은 스킵
                if (action.Type == Data.ActionType.Flee || action.Type == Data.ActionType.Item)
                    continue;

                // 타겟 필터링 (죽은 대상 제거)
                var aliveTargets = action.Targets.Where(t => t.IsAlive).ToList();
                if (aliveTargets.Count == 0) continue;

                Debug.Log($"[Battle] {action.Caster.UnitData.unitName} -> {action.Skill?.skillName ?? "Attack"}");

                // MP 소모
                if (action.Skill != null)
                {
                    action.Caster.Stats.ConsumeMp(action.Skill.mpCost);
                }

                // 스킬 실행
                if (_manager.SkillExecutor != null && action.Skill != null)
                {
                    await _manager.SkillExecutor.ExecuteSkill(action.Caster, aliveTargets, action.Skill, ct);
                }
                else
                {
                    // SkillExecutor 없을 시 간단 데미지 처리
                    foreach (var target in aliveTargets)
                    {
                        var result = DamageCalculator.Calculate(action.Caster.Stats, target.Stats, action.Skill);
                        if (result.FinalDamage > 0)
                            target.TakeDamage(result.FinalDamage, result.IsCritical);
                        else
                            target.Heal(-result.FinalDamage);
                    }
                }

                // 사망 처리
                foreach (var entity in _manager.AllEntities)
                {
                    if (!entity.IsAlive && entity.gameObject.activeSelf)
                    {
                        await entity.PlayDeathAsync(ct);
                    }
                }

                // 배틀 종료 체크
                if (_manager.CheckBattleEnd())
                    break;

                await UniTask.Delay(200, cancellationToken: ct);
            }
        }

        public void Execute() { }

        public UniTask Exit(CancellationToken ct = default) => UniTask.CompletedTask;
    }
}
