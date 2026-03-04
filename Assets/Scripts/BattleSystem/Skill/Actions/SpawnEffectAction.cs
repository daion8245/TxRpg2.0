using Cysharp.Threading.Tasks;
using System.Threading;
using TxRpg.Core;
using TxRpg.Core.Events;
using TxRpg.Data;
using UnityEngine;

namespace TxRpg.Skill.Actions
{
    public class SpawnEffectAction : ITimelineAction
    {
        private readonly EffectDataSO _effect;

        public SpawnEffectAction(SkillActionData data)
        {
            _effect = data.effect;
        }

        public async UniTask Execute(SkillContext context, CancellationToken ct = default)
        {
            if (_effect == null || _effect.prefab == null) return;

            foreach (var target in context.Targets)
            {
                var position = target.Hitbox.Center + (Vector2)_effect.offset;
                var parent = _effect.attachToTarget ? target.transform : null;

                // EventChannel 경유 대신 직접 스폰 (EffectManager가 없는 경우 대비)
                if (ServiceLocator.TryGet<Effect.EffectManager>(out var effectManager))
                {
                    effectManager.SpawnEffect(_effect, position, parent);
                }
                else
                {
                    var go = Object.Instantiate(_effect.prefab, position, Quaternion.identity, parent);
                    Object.Destroy(go, _effect.duration);
                }
            }

            await UniTask.Delay((int)(_effect.duration * 1000), cancellationToken: ct);
        }
    }
}
