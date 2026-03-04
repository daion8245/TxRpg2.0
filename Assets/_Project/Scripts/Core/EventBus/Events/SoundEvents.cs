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

    [CreateAssetMenu(menuName = "TxRpg/Events/Play SFX Channel")]
    public class PlaySFXEventChannel : EventChannel<PlaySFXPayload> { }

    [CreateAssetMenu(menuName = "TxRpg/Events/Play BGM Channel")]
    public class PlayBGMEventChannel : EventChannel<PlayBGMPayload> { }

    [CreateAssetMenu(menuName = "TxRpg/Events/Stop BGM Channel")]
    public class StopBGMEventChannel : EventChannel<StopBGMPayload> { }
}
