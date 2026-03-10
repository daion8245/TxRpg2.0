using UnityEngine;

namespace TxRpg.Core.Events
{
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
}
