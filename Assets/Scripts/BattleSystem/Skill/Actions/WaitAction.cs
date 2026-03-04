using Cysharp.Threading.Tasks;
using System.Threading;
using TxRpg.Data;

namespace TxRpg.Skill.Actions
{
    public class WaitAction : ITimelineAction
    {
        private readonly float _duration;

        public WaitAction(SkillActionData data)
        {
            _duration = data.duration;
        }

        public async UniTask Execute(SkillContext context, CancellationToken ct = default)
        {
            await UniTask.Delay((int)(_duration * 1000), cancellationToken: ct);
        }
    }
}
