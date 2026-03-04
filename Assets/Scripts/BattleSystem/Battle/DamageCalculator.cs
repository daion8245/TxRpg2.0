using TxRpg.Data;
using TxRpg.Entity;
using UnityEngine;

namespace TxRpg.Battle
{
    public struct DamageResult
    {
        public int FinalDamage;
        public bool IsCritical;
        public ElementType Element;
    }

    public static class DamageCalculator
    {
        public static DamageResult Calculate(EntityStats attacker, EntityStats defender, SkillDataSO skill)
        {
            float baseDamage = skill.basePower / 100f;

            if (skill.category == SkillCategory.Physical)
            {
                baseDamage *= attacker.EffectiveAtk;
                baseDamage *= (1f - defender.EffectiveDef / (defender.EffectiveDef + 100f));
            }
            else if (skill.category == SkillCategory.Magical)
            {
                baseDamage *= attacker.EffectiveAtk * 0.8f;
                baseDamage *= (1f - defender.EffectiveDef * 0.5f / (defender.EffectiveDef * 0.5f + 100f));
            }
            else if (skill.category == SkillCategory.Healing)
            {
                baseDamage *= attacker.EffectiveAtk * 0.5f;
                return new DamageResult
                {
                    FinalDamage = -Mathf.Max(1, Mathf.RoundToInt(baseDamage)),
                    IsCritical = false,
                    Element = skill.element
                };
            }

            // 크리티컬 판정
            bool isCritical = Random.Range(0, 100) < attacker.Kd;
            if (isCritical)
            {
                float critMultiplier = 1.5f + attacker.Kr / 100f;
                baseDamage *= critMultiplier;
            }

            // 랜덤 편차 (90%~110%)
            baseDamage *= Random.Range(0.9f, 1.1f);

            int finalDamage = Mathf.Max(1, Mathf.RoundToInt(baseDamage));

            return new DamageResult
            {
                FinalDamage = finalDamage,
                IsCritical = isCritical,
                Element = skill.element
            };
        }
    }
}
