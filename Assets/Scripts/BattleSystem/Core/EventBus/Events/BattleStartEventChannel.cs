using UnityEngine;

namespace TxRpg.Core.Events
{
    [CreateAssetMenu(menuName = "TxRpg/Events/Battle Start Channel")]
    public class BattleStartEventChannel : EventChannel<BattleStartPayload> { }
}
