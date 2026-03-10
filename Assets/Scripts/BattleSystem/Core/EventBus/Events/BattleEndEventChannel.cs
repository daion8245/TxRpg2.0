using UnityEngine;

namespace TxRpg.Core.Events
{
    [CreateAssetMenu(menuName = "TxRpg/Events/Battle End Channel")]
    public class BattleEndEventChannel : EventChannel<BattleEndPayload> { }
}
