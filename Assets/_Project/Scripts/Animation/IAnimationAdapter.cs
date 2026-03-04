using Cysharp.Threading.Tasks;
using System.Threading;

namespace TxRpg.Animation
{
    /// <summary>
    /// 애니메이션 시스템 추상화 인터페이스.
    /// Spine, Unity Animator 등 다양한 백엔드를 통합합니다.
    /// </summary>
    public interface IAnimationAdapter
    {
        void Play(string stateName);
        UniTask PlayAsync(string stateName, CancellationToken ct = default);
        bool IsPlaying(string stateName);
        void SetSpeed(float speed);
    }
}
