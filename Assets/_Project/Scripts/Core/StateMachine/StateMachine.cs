using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

namespace TxRpg.Core
{
    /// <summary>
    /// 범용 비동기 상태 머신.
    /// 상태 전환 시 현재 상태의 Exit → 새 상태의 Enter를 순차 실행합니다.
    /// </summary>
    public class StateMachine
    {
        public IState CurrentState { get; private set; }
        private bool _isTransitioning;

        public async UniTask TransitionTo(IState newState, CancellationToken ct = default)
        {
            if (_isTransitioning)
            {
                Debug.LogWarning("[StateMachine] 전환 중 중복 전환 시도 무시");
                return;
            }

            _isTransitioning = true;

            if (CurrentState != null)
            {
                await CurrentState.Exit(ct);
            }

            CurrentState = newState;

            if (CurrentState != null)
            {
                await CurrentState.Enter(ct);
            }

            _isTransitioning = false;
        }

        public void Update()
        {
            if (!_isTransitioning)
            {
                CurrentState?.Execute();
            }
        }
    }
}
