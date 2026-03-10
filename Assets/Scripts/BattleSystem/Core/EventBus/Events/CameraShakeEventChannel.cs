using UnityEngine;

namespace TxRpg.Core.Events
{
    [CreateAssetMenu(menuName = "TxRpg/Events/Camera Shake Channel")]
    public class CameraShakeEventChannel : EventChannel<CameraShakePayload> { }
}
