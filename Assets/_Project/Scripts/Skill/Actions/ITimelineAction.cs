using Cysharp.Threading.Tasks;
using System.Threading;

namespace TxRpg.Skill
{
    public interface ITimelineAction
    {
        UniTask Execute(SkillContext context, CancellationToken ct = default);
    }
}
