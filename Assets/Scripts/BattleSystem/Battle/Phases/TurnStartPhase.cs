using Cysharp.Threading.Tasks;
using System.Threading;
using TxRpg.Core;
using UnityEngine;

namespace TxRpg.Battle.Phases
{
    public class TurnStartPhase : IState
    {
        private readonly BattleManager _manager;

        public TurnStartPhase(BattleManager manager)
        {
            _manager = manager;
        }

        public async UniTask Enter(CancellationToken ct = default)
        {
            _manager.TurnManager.AdvanceTurn();
            Debug.Log($"[Battle] === Turn {_manager.TurnManager.CurrentTurn} Start ===");

            // 버프 틱 처리
            foreach (var entity in _manager.AllEntities)
            {
                if (entity.IsAlive)
                {
                    entity.Buffs.TickBuffs();
                    entity.Skills.TickCooldowns();
                }
            }

            // 행동 순서 결정
            _manager.ActionOrder = _manager.TurnManager.GetActionOrder(_manager.AllEntities);

            // 턴 변경 이벤트
            _manager.RaiseTurnChanged();

            await UniTask.Delay(300, cancellationToken: ct);
        }

        public void Execute() { }

        public UniTask Exit(CancellationToken ct = default) => UniTask.CompletedTask;
    }
}
