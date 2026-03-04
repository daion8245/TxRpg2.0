using Cysharp.Threading.Tasks;
using System.Threading;
using TxRpg.Data;
using UnityEngine;

namespace TxRpg.Skill.Actions
{
    public class FlashAction : ITimelineAction
    {
        private readonly Color _color;
        private readonly float _duration;

        public FlashAction(SkillActionData data)
        {
            _color = data.flashColor;
            _duration = data.duration;
        }

        public async UniTask Execute(SkillContext context, CancellationToken ct = default)
        {
            foreach (var target in context.Targets)
            {
                await target.Animator.FlashAsync(_color, _duration, ct);
            }
        }
    }
}
