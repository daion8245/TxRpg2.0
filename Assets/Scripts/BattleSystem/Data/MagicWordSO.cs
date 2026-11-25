using System;
using UnityEngine;

namespace TxRpg.Data
{
    public enum MagicWordType
    {
        Fire,
        Shield,
        Launch,
        Strong
    }

    /// <summary>
    /// 단어와 주문이 가지는 카운터 판정용 특징 태그 (전투 개선 기획서 4절).
    /// </summary>
    [Flags]
    public enum SpellTrait
    {
        None = 0,
        Fire = 1 << 0,       // 화염
        Projectile = 1 << 1, // 투사체 (발사)
        Block = 1 << 2,      // 막기 (제자리 방패)
        Intercept = 1 << 3,  // 요격 (투척 방패)
        Heavy = 1 << 4       // 강타 (강하게)
    }

    public static class SpellTraitHelper
    {
        public static string ToIconLabel(SpellTrait trait) => trait switch
        {
            SpellTrait.Fire => "[화염]",
            SpellTrait.Projectile => "[투사체]",
            SpellTrait.Block => "[막기]",
            SpellTrait.Intercept => "[요격]",
            SpellTrait.Heavy => "[강타]",
            _ => ""
        };

        public static string FormatAll(SpellTrait traits)
        {
            if (traits == SpellTrait.None) return "없음";
            var list = new System.Collections.Generic.List<string>(4);
            if ((traits & SpellTrait.Fire) != 0) list.Add("화염");
            if ((traits & SpellTrait.Projectile) != 0) list.Add("투사체");
            if ((traits & SpellTrait.Block) != 0) list.Add("막기");
            if ((traits & SpellTrait.Intercept) != 0) list.Add("요격");
            if ((traits & SpellTrait.Heavy) != 0) list.Add("강타");
            return string.Join("·", list);
        }
    }

    [CreateAssetMenu(menuName = "TxRpg/Magic/Magic Word")]
    public class MagicWordSO : ScriptableObject
    {
        public MagicWordType wordType;
        public string displayName;
        public bool isMain;
        public int manaCost = 1;
        public SpellTrait grantedTrait = SpellTrait.None;
        [TextArea] public string description;
        public Color accentColor = Color.white;
    }
}
