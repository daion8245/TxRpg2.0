using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

namespace TxRpg.Animation
{
    /// <summary>
    /// Unity Animator 기반 IAnimationAdapter 구현.
    /// 스프라이트 프레임 애니메이션에 사용합니다.
    /// </summary>
    public class SpriteAnimAdapter : IAnimationAdapter
    {
        private readonly Animator _animator;

        public SpriteAnimAdapter(Animator animator)
        {
            _animator = animator;
        }

        public void Play(string stateName)
        {
            if (_animator == null) return;
            _animator.Play(stateName);
        }

        public async UniTask PlayAsync(string stateName, CancellationToken ct = default)
        {
            if (_animator == null) return;

            _animator.Play(stateName);

            // 한 프레임 대기하여 상태가 전환되도록 함
            await UniTask.Yield(ct);

            // 애니메이션이 끝날 때까지 대기
            var stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            while (stateInfo.normalizedTime < 1f && !_animator.IsInTransition(0))
            {
                await UniTask.Yield(ct);
                stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            }
        }

        public bool IsPlaying(string stateName)
        {
            if (_animator == null) return false;
            var stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            return stateInfo.IsName(stateName) && stateInfo.normalizedTime < 1f;
        }

        public void SetSpeed(float speed)
        {
            if (_animator == null) return;
            _animator.speed = speed;
        }
    }
}
