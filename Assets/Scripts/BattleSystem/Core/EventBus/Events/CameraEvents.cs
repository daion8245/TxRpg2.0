using UnityEngine;

namespace TxRpg.Core.Events
{
    [System.Serializable]
    public struct CameraShakePayload
    {
        public float Intensity;
        public float Duration;
    }

    [System.Serializable]
    public struct CameraZoomPayload
    {
        public Vector3 TargetPosition;
        public float OrthoSize;
        public float Duration;
    }

    [System.Serializable]
    public struct CameraPanPayload
    {
        public Vector3 TargetPosition;
        public float Duration;
    }
}
