using Cysharp.Threading.Tasks;
using System.Threading;
using TxRpg.Data;
using UnityEngine;

namespace TxRpg.Skill
{
    public class TimelineDirector
    {
        public async UniTask PlayTimeline(SkillActionData[] timeline, SkillContext context, CancellationToken ct = default)
        {
            if (timeline == null || timeline.Length == 0) return;

            foreach (var actionData in timeline)
            {
                if (ct.IsCancellationRequested) break;

                var action = ActionFactory.Create(actionData);
                await action.Execute(context, ct);
            }
        }
    }
}
