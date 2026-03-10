using UnityEngine;

namespace TxRpg.Core.Events
{
    [CreateAssetMenu(menuName = "TxRpg/Events/Camera Pan Channel")]
    public class CameraPanEventChannel : EventChannel<CameraPanPayload> { }
}
