using UnityEngine;

namespace TxRpg.Core.Events
{
    [CreateAssetMenu(menuName = "TxRpg/Events/Damage Channel")]
    public class DamageEventChannel : EventChannel<DamagePayload> { }
}
