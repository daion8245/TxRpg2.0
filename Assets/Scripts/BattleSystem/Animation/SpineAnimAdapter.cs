using System;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace TxRpg.Animation
{
    /// <summary>
    /// Spine 런타임 기반 IAnimationAdapter 구현 (껍데기).
    /// Spine 패키지 설치 후 구현 예정.
    /// </summary>
    public class SpineAnimAdapter : IAnimationAdapter
    {
        public void Play(string stateName)
        {
            throw new NotImplementedException("Spine 런타임이 설치되지 않았습니다.");
        }

        public UniTask PlayAsync(string stateName, CancellationToken ct = default)
        {
            throw new NotImplementedException("Spine 런타임이 설치되지 않았습니다.");
        }

        public bool IsPlaying(string stateName)
        {
            throw new NotImplementedException("Spine 런타임이 설치되지 않았습니다.");
        }

        public void SetSpeed(float speed)
        {
            throw new NotImplementedException("Spine 런타임이 설치되지 않았습니다.");
        }
    }
}
