using UnityEngine;

namespace TxRpg.Core.Events
{
    [CreateAssetMenu(menuName = "TxRpg/Events/Unit Died Channel")]
    public class UnitDiedEventChannel : EventChannel<UnitDiedPayload> { }
}
