using Cysharp.Threading.Tasks;
using System.Threading;
using TxRpg.Data;

namespace TxRpg.Skill.Actions
{
    public class PlayAnimAction : ITimelineAction
    {
        private readonly string _animName;

        public PlayAnimAction(SkillActionData data)
        {
            _animName = data.animationName;
        }

        public async UniTask Execute(SkillContext context, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(_animName))
            {
                await context.Caster.Animator.PlayAttackAsync(ct);
            }
            else
            {
                await context.Caster.Animator.PlayAnimationAsync(_animName, ct);
            }
        }
    }
}
