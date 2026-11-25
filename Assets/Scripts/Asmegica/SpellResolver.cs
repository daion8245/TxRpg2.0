using System;
using System.Collections.Generic;

namespace Asmegica
{
    public enum MagicWord
    {
        Fire,
        Shield,
        Launch,
        Strong
    }

    // Immutable spell. Created by SpellResolver.TryBuildSpell.
    public sealed class Spell
    {
        internal Spell(MagicWord body, bool isLaunched, bool isBurning, bool isStrong, int cost, string name)
        {
            Body = body;
            IsLaunched = isLaunched;
            IsBurning = isBurning;
            IsStrong = isStrong;
            Cost = cost;
            Name = name;
        }

        // Last main word in the group: Fire or Shield.
        public MagicWord Body { get; }

        public bool IsLaunched { get; }

        // True only when a Fire placed earlier enchants a Shield.
        public bool IsBurning { get; }

        public bool IsStrong { get; }

        // Every word costs 1.
        public int Cost { get; }

        // Korean display name.
        public string Name { get; }
    }

    public static class SpellResolver
    {
        public static string WordName(MagicWord word)
        {
            switch (word)
            {
                case MagicWord.Fire:   return "화염";
                case MagicWord.Shield: return "방패";
                case MagicWord.Launch: return "발사";
                case MagicWord.Strong: return "강하게";
                default:               return "알 수 없는 단어";
            }
        }

        public static bool TryBuildSpell(IReadOnlyList<MagicWord> group, out Spell spell, out string error)
        {
            spell = null;
            error = string.Empty;

            if (group == null)
            {
                error = "주문에 넣을 단어를 선택하세요.";
                return false;
            }

            int count = group.Count;
            if (count == 0)
            {
                error = "주문 단어가 하나도 없습니다.";
                return false;
            }

            var seen = new HashSet<MagicWord>();
            MagicWord body = MagicWord.Fire;
            int mainCount = 0;
            int fireIndex = -1;
            int shieldIndex = -1;
            bool hasLaunch = false;
            bool hasStrong = false;

            for (int i = 0; i < count; i++)
            {
                MagicWord word = group[i];

                if (!Enum.IsDefined(typeof(MagicWord), word))
                {
                    error = "정의되지 않은 단어가 포함되어 있습니다. (값: " + (int)word + ")";
                    return false;
                }

                if (!seen.Add(word))
                {
                    error = "같은 단어는 한 주문에 한 번만 쓸 수 있습니다: " + WordName(word);
                    return false;
                }

                switch (word)
                {
                    case MagicWord.Fire:
                        fireIndex = i;
                        body = word; // last main wins
                        mainCount++;
                        break;
                    case MagicWord.Shield:
                        shieldIndex = i;
                        body = word; // last main wins
                        mainCount++;
                        break;
                    case MagicWord.Launch:
                        hasLaunch = true;
                        break;
                    case MagicWord.Strong:
                        hasStrong = true;
                        break;
                }
            }

            if (mainCount == 0)
            {
                error = "메인 단어인 화염이나 방패가 필요합니다.";
                return false;
            }

            if (fireIndex >= 0 && shieldIndex >= 0 && shieldIndex < fireIndex)
            {
                error = "화염으로 방패를 강화하려면 화염을 방패보다 앞에 놓으세요.";
                return false;
            }

            bool isBurning = fireIndex >= 0 && shieldIndex >= 0;
            string name = BuildName(body, isBurning, hasLaunch, hasStrong);

            spell = new Spell(body, hasLaunch, isBurning, hasStrong, count, name);
            return true;
        }

        private static string BuildName(MagicWord body, bool isBurning, bool isLaunched, bool isStrong)
        {
            string name = body == MagicWord.Fire
                ? (isLaunched ? "화염구" : "기본 화염")
                : (isLaunched ? "투척 방패" : "방패");
            if (isBurning) name = "불붙은 " + name;
            if (isStrong) name = "강화 " + name;
            return name;
        }
    }
}
