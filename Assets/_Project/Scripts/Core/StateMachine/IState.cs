using Cysharp.Threading.Tasks;
using System.Threading;

namespace TxRpg.Core
{
    /// <summary>
    /// 상태 머신의 개별 상태 인터페이스.
    /// Enter/Exit는 비동기로 연출 가능.
    /// </summary>
    public interface IState
    {
        UniTask Enter(CancellationToken ct = default);
        void Execute();
        UniTask Exit(CancellationToken ct = default);
    }
}
