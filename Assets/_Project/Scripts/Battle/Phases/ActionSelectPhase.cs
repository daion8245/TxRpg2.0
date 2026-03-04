using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TxRpg.Core;
using TxRpg.Data;
using TxRpg.Entity;
using UnityEngine;

namespace TxRpg.Battle.Phases
{
    public class ActionSelectPhase : IState
    {
        private readonly BattleManager _manager;

        public ActionSelectPhase(BattleManager manager)
        {
            _manager = manager;
        }

        public async UniTask Enter(CancellationToken ct = default)
        {
            Debug.Log("[Battle] === Action Select Phase ===");
            _manager.ActionQueueInstance.Clear();

            foreach (var entity in _manager.ActionOrder)
            {
                if (!entity.IsAlive) continue;

                BattleAction action;

                if (entity.Team == EntityTeam.Player)
                {
                    action = await WaitForPlayerInput(entity, ct);
                }
                else
                {
                    action = _manager.AI.DecideAction(
                        entity,
                        _manager.Enemies.Where(e => e.IsAlive).ToList(),
                        _manager.Players.Where(e => e.IsAlive).ToList()
                    );
                }

                _manager.ActionQueueInstance.Enqueue(action);
            }
        }

        private async UniTask<BattleAction> WaitForPlayerInput(BattleEntity entity, CancellationToken ct)
        {
            // UI를 통한 스킬 선택 대기
            if (_manager.SkillSelectUI != null)
            {
                var selectedSkill = await _manager.SkillSelectUI.WaitForSelection(entity, ct);
                var targets = await _manager.SkillSelectUI.WaitForTargetSelection(
                    selectedSkill, entity, _manager.Players, _manager.Enemies, ct);

                return new BattleAction
                {
                    Caster = entity,
                    Targets = targets,
                    Skill = selectedSkill,
                    Type = ActionType.Skill
                };
            }

            // UI가 없으면 기본 공격
            Debug.LogWarning("[Battle] SkillSelectUI 없음 - 기본 공격 자동 선택");
            var aliveEnemies = _manager.Enemies.Where(e => e.IsAlive).ToList();
            return new BattleAction
            {
                Caster = entity,
                Targets = aliveEnemies.Count > 0
                    ? new List<BattleEntity> { aliveEnemies[0] }
                    : new List<BattleEntity>(),
                Skill = entity.Skills.AllSkills.Length > 0 ? entity.Skills.AllSkills[0] : null,
                Type = ActionType.Attack
            };
        }

        public void Execute() { }

        public UniTask Exit(CancellationToken ct = default) => UniTask.CompletedTask;
    }
}
