using System;
using UnityEngine;

namespace TxRpg.Data
{
    public enum EnemyIntentKind
    {
        Wait,
        Approach,
        Prepare,
        Melee,
        Missile,
        Retreat,
        Defend
    }

    [Serializable]
    public struct EnemyIntentSlot
    {
        public EnemyIntentKind kind;
        public int power;
        public bool overrideCounter;
        public SpellTrait customCounterTrait;

        public EnemyIntentSlot(EnemyIntentKind kind, int power = 0, bool overrideCounter = false, SpellTrait customCounterTrait = SpellTrait.None)
        {
            this.kind = kind;
            this.power = power;
            this.overrideCounter = overrideCounter;
            this.customCounterTrait = customCounterTrait;
        }

        public SpellTrait EffectiveCounterTrait => overrideCounter ? customCounterTrait : DefaultCounterTrait(kind);

        public static SpellTrait DefaultCounterTrait(EnemyIntentKind k) => k switch
        {
            EnemyIntentKind.Wait => SpellTrait.Projectile,
            EnemyIntentKind.Approach => SpellTrait.Fire,
            EnemyIntentKind.Prepare => SpellTrait.Fire,
            EnemyIntentKind.Melee => SpellTrait.Block,
            EnemyIntentKind.Missile => SpellTrait.Intercept,
            EnemyIntentKind.Defend => SpellTrait.Heavy,
            _ => SpellTrait.None
        };

        public string Label => kind switch
        {
            EnemyIntentKind.Approach => "접근",
            EnemyIntentKind.Prepare => "공격 준비",
            EnemyIntentKind.Melee => $"근접 {power}",
            EnemyIntentKind.Missile => $"투사체 {power}",
            EnemyIntentKind.Defend => "방어",
            EnemyIntentKind.Retreat => "후퇴",
            _ => "대기"
        };

        public string CounterBadge => SpellTraitHelper.ToIconLabel(EffectiveCounterTrait);
    }

    [CreateAssetMenu(menuName = "TxRpg/Data/Enemy Pattern")]
    public class EnemyPatternSO : ScriptableObject
    {
        [Header("기본 정보")]
        public string patternName = "돌격 기사";

        [Header("약점 게이지 & 그로기 다이얼 (기획서 8절)")]
        public int maxWeakGauge = 2;
        public int groggyDuration = 2;
        public bool recoverGaugeOnNoCounterTurn;

        [Header("5칸 행동 패턴")]
        public EnemyIntentSlot[] slots =
        {
            new(EnemyIntentKind.Wait),
            new(EnemyIntentKind.Approach),
            new(EnemyIntentKind.Prepare),
            new(EnemyIntentKind.Melee, 3),
            new(EnemyIntentKind.Wait)
        };

        public EnemyIntentSlot GetSlot(int index)
        {
            if (slots == null || slots.Length == 0)
                return new EnemyIntentSlot(EnemyIntentKind.Wait);
            return slots[Mathf.Clamp(index, 0, slots.Length - 1)];
        }
    }
}
