using Cysharp.Threading.Tasks;
using System.Threading;
using TxRpg.Animation;
using UnityEngine;

namespace TxRpg.Entity
{
    /// <summary>
    /// BattleEntity의 애니메이션을 관리하는 컴포넌트.
    /// IAnimationAdapter를 통해 백엔드에 독립적으로 동작합니다.
    /// </summary>
    public class EntityAnimator
    {
        private readonly IAnimationAdapter _adapter;
        private readonly AnimStateMachine _animStateMachine;
        private readonly SpriteRenderer _spriteRenderer;

        public EntityAnimator(IAnimationAdapter adapter, SpriteRenderer spriteRenderer)
        {
            _adapter = adapter;
            _spriteRenderer = spriteRenderer;
            _animStateMachine = new AnimStateMachine(adapter);
        }

        public void PlayIdle()
        {
            _animStateMachine.TransitionTo(AnimState.Idle);
        }

        public async UniTask PlayAttackAsync(CancellationToken ct = default)
        {
            _animStateMachine.TransitionTo(AnimState.Attack);
            await _adapter.PlayAsync(AnimStateMachine.GetAnimationName(AnimState.Attack), ct);
            PlayIdle();
        }

        public async UniTask PlayHitAsync(CancellationToken ct = default)
        {
            _animStateMachine.TransitionTo(AnimState.Hit);
            await _adapter.PlayAsync(AnimStateMachine.GetAnimationName(AnimState.Hit), ct);
            PlayIdle();
        }

        public async UniTask PlayDieAsync(CancellationToken ct = default)
        {
            _animStateMachine.TransitionTo(AnimState.Die);
            await _adapter.PlayAsync(AnimStateMachine.GetAnimationName(AnimState.Die), ct);
        }

        public async UniTask PlayAnimationAsync(string animName, CancellationToken ct = default)
        {
            await _adapter.PlayAsync(animName, ct);
        }

        public void SetFacingLeft(bool facingLeft)
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.flipX = facingLeft;
            }
        }

        public async UniTask FlashAsync(Color color, float duration, CancellationToken ct = default)
        {
            if (_spriteRenderer != null)
            {
                await TweenHelper.ColorFlashAsync(_spriteRenderer, color, duration, ct);
            }
        }
    }
}
