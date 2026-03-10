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

                // 도망 선택 시 나머지 엔티티 행동 생략
                if (action.Type == ActionType.Flee)
                    break;
            }
        }

        private async UniTask<BattleAction> WaitForPlayerInput(BattleEntity entity, CancellationToken ct)
        {
            // 커맨드 UI가 있으면 먼저 커맨드 선택
            if (_manager.BattleCommandUI != null)
            {
                var command = await _manager.BattleCommandUI.WaitForCommand(entity, ct);

                switch (command)
                {
                    case ActionType.Attack:
                        return await HandleAttack(entity, ct);

                    case ActionType.Skill:
                        return await HandleSkill(entity, ct);

                    case ActionType.Item:
                        return await HandleItem(entity, ct);

                    case ActionType.Flee:
                        return HandleFlee(entity);
                }
            }

            // 커맨드 UI 없으면 스킬 선택 UI로 폴백
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
            Debug.LogWarning("[Battle] UI 없음 - 기본 공격 자동 선택");
            return CreateDefaultAttack(entity);
        }

        /// <summary>
        /// 공격: 첫 번째 스킬(기본 공격)로 적 단일 대상
        /// </summary>
        private async UniTask<BattleAction> HandleAttack(BattleEntity entity, CancellationToken ct)
        {
            var aliveEnemies = _manager.Enemies.Where(e => e.IsAlive).ToList();
            var defaultSkill = entity.Skills.AllSkills.Length > 0 ? entity.Skills.AllSkills[0] : null;

            // 타겟 선택
            List<BattleEntity> targets;
            if (_manager.SkillSelectUI != null && defaultSkill != null)
            {
                targets = await _manager.SkillSelectUI.WaitForTargetSelection(
                    defaultSkill, entity, _manager.Players, _manager.Enemies, ct);
            }
            else
            {
                targets = aliveEnemies.Count > 0
                    ? new List<BattleEntity> { aliveEnemies[0] }
                    : new List<BattleEntity>();
            }

            return new BattleAction
            {
                Caster = entity,
                Targets = targets,
                Skill = defaultSkill,
                Type = ActionType.Attack
            };
        }

        /// <summary>
        /// 스킬: 스킬 선택 UI를 통해 스킬과 타겟 선택
        /// </summary>
        private async UniTask<BattleAction> HandleSkill(BattleEntity entity, CancellationToken ct)
        {
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

            Debug.LogWarning("[Battle] SkillSelectUI 없음 - 기본 공격으로 대체");
            return CreateDefaultAttack(entity);
        }

        /// <summary>
        /// 아이템: 기틀만 구현 (추후 아이템 UI 연결)
        /// </summary>
        private async UniTask<BattleAction> HandleItem(BattleEntity entity, CancellationToken ct)
        {
            // TODO: 아이템 선택 UI 연결
            Debug.Log("[Battle] 아이템 사용 - 아직 미구현, 턴 스킵");
            await UniTask.Yield(ct);

            return new BattleAction
            {
                Caster = entity,
                Targets = new List<BattleEntity>(),
                Skill = null,
                Type = ActionType.Item
            };
        }

        /// <summary>
        /// 도망: 배틀 종료 플래그 설정
        /// </summary>
        private BattleAction HandleFlee(BattleEntity entity)
        {
            Debug.Log("[Battle] 도망 선택!");
            _manager.SetFlee();

            return new BattleAction
            {
                Caster = entity,
                Targets = new List<BattleEntity>(),
                Skill = null,
                Type = ActionType.Flee
            };
        }

        private BattleAction CreateDefaultAttack(BattleEntity entity)
        {
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
