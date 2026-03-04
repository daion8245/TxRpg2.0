using Cysharp.Threading.Tasks;
using System.Threading;
using TxRpg.Core;
using UnityEngine;

namespace TxRpg.Battle
{
    public class BattleStateMachine : StateMachine
    {
        private readonly BattleManager _manager;

        public BattleStateMachine(BattleManager manager)
        {
            _manager = manager;
        }

        public async UniTask RunBattle(CancellationToken ct)
        {
            var introPhase = new Phases.BattleIntroPhase(_manager);
            await TransitionTo(introPhase, ct);

            while (!ct.IsCancellationRequested)
            {
                // TurnStart
                var turnStart = new Phases.TurnStartPhase(_manager);
                await TransitionTo(turnStart, ct);

                // ActionSelect
                var actionSelect = new Phases.ActionSelectPhase(_manager);
                await TransitionTo(actionSelect, ct);

                // Execute
                var execute = new Phases.ExecutePhase(_manager);
                await TransitionTo(execute, ct);

                // TurnEnd
                var turnEnd = new Phases.TurnEndPhase(_manager);
                await TransitionTo(turnEnd, ct);

                // 승패 체크
                if (_manager.IsBattleOver)
                {
                    var result = new Phases.ResultPhase(_manager);
                    await TransitionTo(result, ct);
                    break;
                }
            }
        }
    }
}
