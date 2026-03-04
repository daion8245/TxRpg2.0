using Cysharp.Threading.Tasks;
using System.Threading;
using TxRpg.Core;
using TxRpg.Data;
using UnityEngine;

namespace TxRpg.Battle.Phases
{
    public class ResultPhase : IState
    {
        private readonly BattleManager _manager;

        public ResultPhase(BattleManager manager)
        {
            _manager = manager;
        }

        public async UniTask Enter(CancellationToken ct = default)
        {
            Debug.Log($"[Battle] === Result: {_manager.Result} ===");

            if (_manager.Result == BattleResult.Victory)
            {
                Debug.Log($"[Battle] 보상: Gold={_manager.StageData.goldReward}, EXP={_manager.StageData.expReward}");
            }

            _manager.RaiseBattleEnd();

            // 결과 UI 표시 대기
            await UniTask.Delay(2000, cancellationToken: ct);
        }

        public void Execute() { }

        public UniTask Exit(CancellationToken ct = default) => UniTask.CompletedTask;
    }
}
