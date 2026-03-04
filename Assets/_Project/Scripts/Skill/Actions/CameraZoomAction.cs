using Cysharp.Threading.Tasks;
using System.Threading;
using TxRpg.Core;
using TxRpg.Data;
using UnityEngine;

namespace TxRpg.Skill.Actions
{
    public class CameraZoomAction : ITimelineAction
    {
        private readonly float _zoomSize;
        private readonly float _duration;

        public CameraZoomAction(SkillActionData data)
        {
            _zoomSize = data.zoomSize;
            _duration = data.duration;
        }

        public async UniTask Execute(SkillContext context, CancellationToken ct = default)
        {
            if (ServiceLocator.TryGet<Camera.BattleCameraController>(out var cam))
            {
                Vector3 targetPos = context.Targets.Count > 0
                    ? (Vector3)context.Targets[0].Hitbox.Center
                    : context.Caster.transform.position;

                await cam.ZoomToAsync(targetPos, _zoomSize, _duration, ct);
            }
        }
    }
}
