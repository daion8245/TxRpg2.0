using Cysharp.Threading.Tasks;
using System.Threading;
using TxRpg.Core;
using TxRpg.Core.Events;
using TxRpg.Data;

namespace TxRpg.Skill.Actions
{
    public class CameraShakeAction : ITimelineAction
    {
        private readonly float _intensity;
        private readonly float _duration;

        public CameraShakeAction(SkillActionData data)
        {
            _intensity = data.shakeIntensity;
            _duration = data.duration;
        }

        public async UniTask Execute(SkillContext context, CancellationToken ct = default)
        {
            if (ServiceLocator.TryGet<Camera.BattleCameraController>(out var cam))
            {
                cam.Shake(_intensity, _duration);
            }

            await UniTask.Delay((int)(_duration * 1000), cancellationToken: ct);
        }
    }
}
