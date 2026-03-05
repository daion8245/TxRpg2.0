using Cysharp.Threading.Tasks;
using System.Threading;
using TxRpg.Animation;
using TxRpg.Data;
using UnityEngine;

namespace TxRpg.Skill.Actions
{
    public class MoveToAction : ITimelineAction
    {
        private readonly float _speed;
        private readonly bool _isReturn;

        public MoveToAction(SkillActionData data, bool isReturn = false)
        {
            _speed = data.moveSpeed;
            _isReturn = isReturn;
        }

        public async UniTask Execute(SkillContext context, CancellationToken ct = default)
        {
            var casterTransform = context.Caster.transform;

            if (_isReturn)
            {
                // 원래 위치로 복귀
                var targetPos = (Vector3)context.Caster.FormationPosition;
                float distance = Vector3.Distance(casterTransform.position, targetPos);
                float duration = distance / Mathf.Max(_speed, 0.1f);
                await TweenHelper.MoveToAsync(casterTransform, targetPos, duration, ct);
            }
            else if (context.Targets.Count > 0)
            {
                // 대상 앞으로 이동
                var target = context.Targets[0];
                var targetPos = target.Hitbox.Center;
                float offsetX = context.Caster.Team == Data.EntityTeam.Player ? -1.5f : 1.5f;
                var attackPos = new Vector3(targetPos.x + offsetX, targetPos.y, 0f);

                float distance = Vector3.Distance(casterTransform.position, attackPos);
                float duration = distance / Mathf.Max(_speed, 0.1f);
                await TweenHelper.MoveToAsync(casterTransform, attackPos, duration, ct);
            }
        }
    }
}
