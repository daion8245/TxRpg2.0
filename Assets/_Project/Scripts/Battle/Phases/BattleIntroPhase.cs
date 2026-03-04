using Cysharp.Threading.Tasks;
using System.Threading;
using TxRpg.Core;
using UnityEngine;

namespace TxRpg.Battle.Phases
{
    public class BattleIntroPhase : IState
    {
        private readonly BattleManager _manager;

        public BattleIntroPhase(BattleManager manager)
        {
            _manager = manager;
        }

        public async UniTask Enter(CancellationToken ct = default)
        {
            Debug.Log("[Battle] === Intro Phase ===");

            // 유닛 생성 및 배치
            _manager.SpawnEntities();

            // 배틀 시작 이벤트
            _manager.RaiseBattleStart();

            // 등장 연출 대기
            await UniTask.Delay(1000, cancellationToken: ct);
        }

        public void Execute() { }

        public UniTask Exit(CancellationToken ct = default) => UniTask.CompletedTask;
    }
}
