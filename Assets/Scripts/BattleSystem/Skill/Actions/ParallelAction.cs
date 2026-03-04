using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using TxRpg.Data;

namespace TxRpg.Skill.Actions
{
    public class ParallelAction : ITimelineAction
    {
        private readonly SkillActionData[] _parallelActions;

        public ParallelAction(SkillActionData data)
        {
            _parallelActions = data.parallelActions;
        }

        public async UniTask Execute(SkillContext context, CancellationToken ct = default)
        {
            if (_parallelActions == null || _parallelActions.Length == 0) return;

            var tasks = new List<UniTask>();
            foreach (var actionData in _parallelActions)
            {
                var action = ActionFactory.Create(actionData);
                tasks.Add(action.Execute(context, ct));
            }

            await UniTask.WhenAll(tasks);
        }
    }
}
