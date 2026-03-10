using UnityEngine;

namespace TxRpg.Core.Events
{
    [CreateAssetMenu(menuName = "TxRpg/Events/Camera Zoom Channel")]
    public class CameraZoomEventChannel : EventChannel<CameraZoomPayload> { }
}
