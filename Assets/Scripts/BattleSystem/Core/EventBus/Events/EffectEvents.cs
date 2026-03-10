using UnityEngine;

namespace TxRpg.Core.Events
{
    [System.Serializable]
    public struct SpawnEffectPayload
    {
        public GameObject Prefab;
        public Vector3 Position;
        public Quaternion Rotation;
        public float Duration;
        public Transform Parent;
    }

}
