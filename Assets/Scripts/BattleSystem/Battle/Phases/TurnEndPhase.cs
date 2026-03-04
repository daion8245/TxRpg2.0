using Cysharp.Threading.Tasks;
using System.Threading;
using TxRpg.Core;
using UnityEngine;

namespace TxRpg.Battle.Phases
{
    public class TurnEndPhase : IState
    {
        private readonly BattleManager _manager;

        public TurnEndPhase(BattleManager manager)
        {
            _manager = manager;
        }

        public async UniTask Enter(CancellationToken ct = default)
        {
            Debug.Log($"[Battle] === Turn {_manager.TurnManager.CurrentTurn} End ===");

            // 도트 데미지/회복 처리
            foreach (var entity in _manager.AllEntities)
            {
                if (!entity.IsAlive) continue;

                foreach (var buff in entity.Buffs.Buffs)
                {
                    if (buff.Data.HpPerTurn != 0)
                    {
                        if (buff.Data.HpPerTurn > 0)
                            entity.Heal(buff.Data.HpPerTurn);
                        else
                            entity.TakeDamage(-buff.Data.HpPerTurn, false);
                    }
                }
            }

            // 사망 체크
            foreach (var entity in _manager.AllEntities)
            {
                if (!entity.IsAlive && entity.gameObject.activeSelf)
                {
                    await entity.PlayDeathAsync(ct);
                }
            }

            _manager.CheckBattleEnd();

            await UniTask.Delay(300, cancellationToken: ct);
        }

        public void Execute() { }

        public UniTask Exit(CancellationToken ct = default) => UniTask.CompletedTask;
    }
}
