using UnityEngine;

namespace TxRpg.Core.Events
{
    [CreateAssetMenu(menuName = "TxRpg/Events/Turn Changed Channel")]
    public class TurnChangedEventChannel : EventChannel<TurnChangedPayload> { }
}
