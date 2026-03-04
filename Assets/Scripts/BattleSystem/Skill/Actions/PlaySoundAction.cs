using Cysharp.Threading.Tasks;
using System.Threading;
using TxRpg.Core;
using TxRpg.Data;
using UnityEngine;

namespace TxRpg.Skill.Actions
{
    public class PlaySoundAction : ITimelineAction
    {
        private readonly AudioClip _clip;
        private readonly float _volume;

        public PlaySoundAction(SkillActionData data)
        {
            _clip = data.soundClip;
            _volume = data.soundVolume;
        }

        public async UniTask Execute(SkillContext context, CancellationToken ct = default)
        {
            if (_clip == null) return;

            if (ServiceLocator.TryGet<Sound.SoundManager>(out var soundManager))
            {
                soundManager.PlaySFX(_clip, _volume);
            }
            else
            {
                AudioSource.PlayClipAtPoint(_clip, context.Caster.transform.position, _volume);
            }

            await UniTask.CompletedTask;
        }
    }
}
