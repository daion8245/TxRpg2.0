using UnityEngine;

namespace TxRpg.Core.Events
{
    [System.Serializable]
    public struct PlaySFXPayload
    {
        public AudioClip Clip;
        public float Volume;
    }

    [System.Serializable]
    public struct PlayBGMPayload
    {
        public AudioClip Clip;
        public float Volume;
        public bool Loop;
    }

    [System.Serializable]
    public struct StopBGMPayload
    {
        public float FadeDuration;
    }

}
