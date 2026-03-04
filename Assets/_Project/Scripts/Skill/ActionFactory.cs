using TxRpg.Data;
using TxRpg.Skill.Actions;

namespace TxRpg.Skill
{
    public static class ActionFactory
    {
        public static ITimelineAction Create(SkillActionData data)
        {
            return data.actionType switch
            {
                SkillActionType.PlayAnimation => new PlayAnimAction(data),
                SkillActionType.MoveToTarget => new MoveToAction(data),
                SkillActionType.ReturnToPosition => new MoveToAction(data, isReturn: true),
                SkillActionType.ApplyDamage => new ApplyDamageAction(data),
                SkillActionType.SpawnEffect => new SpawnEffectAction(data),
                SkillActionType.PlaySound => new PlaySoundAction(data),
                SkillActionType.CameraShake => new CameraShakeAction(data),
                SkillActionType.CameraZoom => new CameraZoomAction(data),
                SkillActionType.Flash => new FlashAction(data),
                SkillActionType.Wait => new WaitAction(data),
                SkillActionType.Parallel => new ParallelAction(data),
                _ => new WaitAction(data)
            };
        }
    }
}
