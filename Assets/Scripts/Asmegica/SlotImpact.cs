using System;

namespace Asmegica
{
    public static class SlotImpact
    {
        public static int Absorb(int incoming, ref int remainingGuard)
        {
            int damage = Math.Max(0, incoming);
            int blocked = Math.Min(damage, Math.Max(0, remainingGuard));
            remainingGuard -= blocked;
            return damage - blocked;
        }

        // Only Fire and launched Shield deal damage; unlaunched spells miss when the target is far.
        // Strong adds 1 only on a hit (non-zero damage).
        public static int SpellDamage(Spell spell, bool targetNear)
        {
            if (spell == null || (!spell.IsLaunched && !targetNear))
                return 0;

            int damage = 0;
            if (spell.Body == MagicWord.Fire)
                damage = 2;
            else if (spell.Body == MagicWord.Shield && spell.IsLaunched)
                damage = spell.IsBurning ? 4 : 1;

            if (damage > 0 && spell.IsStrong)
                damage++;
            return damage;
        }
    }
}
