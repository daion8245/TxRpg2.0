using UnityEngine;

namespace TxRpg.Core.Events
{
    #region Payloads

    [System.Serializable]
    public struct BattleStartPayload
    {
        public string StageName;
    }

    [System.Serializable]
    public struct BattleEndPayload
    {
        public bool IsVictory;
    }

    [System.Serializable]
    public struct TurnChangedPayload
    {
        public int TurnNumber;
        public GameObject CurrentEntity;
    }

    [System.Serializable]
    public struct UnitDiedPayload
    {
        public GameObject Unit;
    }

    [System.Serializable]
    public struct DamagePayload
    {
        public GameObject Target;
        public int Damage;
        public bool IsCritical;
        public Vector2 HitPosition;
    }

    #endregion

    #region Concrete Event Channels

    [CreateAssetMenu(menuName = "TxRpg/Events/Battle Start Channel")]
    public class BattleStartEventChannel : EventChannel<BattleStartPayload> { }

    [CreateAssetMenu(menuName = "TxRpg/Events/Battle End Channel")]
    public class BattleEndEventChannel : EventChannel<BattleEndPayload> { }

    [CreateAssetMenu(menuName = "TxRpg/Events/Turn Changed Channel")]
    public class TurnChangedEventChannel : EventChannel<TurnChangedPayload> { }

    [CreateAssetMenu(menuName = "TxRpg/Events/Unit Died Channel")]
    public class UnitDiedEventChannel : EventChannel<UnitDiedPayload> { }

    [CreateAssetMenu(menuName = "TxRpg/Events/Damage Channel")]
    public class DamageEventChannel : EventChannel<DamagePayload> { }

    #endregion
}
