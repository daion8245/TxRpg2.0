using System;
using System.Collections.Generic;
using System.Linq;
using TxRpg.Data;

namespace TxRpg.Magic
{
    /// <summary>
    /// SpellResolver.TryBuildSpell이 생성하는 불변 주문 결과.
    /// 단어 특징의 합집합(Traits)과 기본 위력(화염 4, 화염구 4, 투척 방패 2, 불붙은 투척 방패 8, 강하게 +2)을 제공합니다.
    /// </summary>
    public sealed class SpellData
    {
        public MagicWordType Body { get; }
        public bool IsLaunched { get; }
        public bool IsBurning { get; }
        public bool IsStrong { get; }
        public int Cost { get; }
        public string Name { get; }
        public SpellTrait Traits { get; }

        public bool IsSelfTarget => Body == MagicWordType.Shield && !IsLaunched;
        public int GuardPower => Body == MagicWordType.Shield ? 2 + (IsStrong ? 1 : 0) : 0;

        public bool HasTrait(SpellTrait trait) => trait != SpellTrait.None && (Traits & trait) == trait;

        internal SpellData(
            MagicWordType body,
            bool isLaunched,
            bool isBurning,
            bool isStrong,
            int cost,
            string name,
            SpellTrait traits)
        {
            Body = body;
            IsLaunched = isLaunched;
            IsBurning = isBurning;
            IsStrong = isStrong;
            Cost = cost;
            Name = name;
            Traits = traits;
        }

        /// <summary>
        /// 그로기/방어 배율 적용 전 기본 공격 위력 (전투 개선 기획서 7절 수치 기준).
        /// 화염 4, 화염구 4, 투척 방패 2, 불붙은 투척 방패 8, 강하게 +2 (+레벨 보너스).
        /// </summary>
        public int ComputeBaseDamage(bool targetNear, int levelBonusDamage = 0)
        {
            if (!IsLaunched && !targetNear)
                return 0;

            int damage = 0;
            if (Body == MagicWordType.Fire)
                damage = 4;
            else if (Body == MagicWordType.Shield && IsLaunched)
                damage = IsBurning ? 8 : 2;

            if (damage > 0)
            {
                if (IsStrong) damage += 2;
                damage += Math.Max(0, levelBonusDamage);
            }

            return damage;
        }

        public int ComputeDamage(bool targetNear) => ComputeBaseDamage(targetNear, 0);
    }

    public static class SpellResolver
    {
        public static readonly MagicWordType[] AllWords =
        {
            MagicWordType.Fire,
            MagicWordType.Shield,
            MagicWordType.Launch,
            MagicWordType.Strong
        };

        public static string WordName(MagicWordType word) => word switch
        {
            MagicWordType.Fire => "화염",
            MagicWordType.Shield => "방패",
            MagicWordType.Launch => "발사",
            MagicWordType.Strong => "강하게",
            _ => "알 수 없는 단어"
        };

        public static bool IsMainWord(MagicWordType word) =>
            word == MagicWordType.Fire || word == MagicWordType.Shield;

        public static bool TryBuildSpell(IReadOnlyList<MagicWordType> group, out SpellData spell, out string error)
        {
            spell = null;
            error = string.Empty;

            if (group == null || group.Count == 0)
            {
                error = "주문에 넣을 단어를 선택하세요.";
                return false;
            }

            var seen = new HashSet<MagicWordType>();
            MagicWordType body = MagicWordType.Fire;
            int mainCount = 0;
            int fireIndex = -1;
            int shieldIndex = -1;
            bool hasLaunch = false;
            bool hasStrong = false;

            for (int i = 0; i < group.Count; i++)
            {
                MagicWordType word = group[i];
                if (!Enum.IsDefined(typeof(MagicWordType), word))
                {
                    error = $"정의되지 않은 단어입니다. ({(int)word})";
                    return false;
                }

                if (!seen.Add(word))
                {
                    error = $"같은 단어는 한 주문에 중복할 수 없습니다: {WordName(word)}";
                    return false;
                }

                switch (word)
                {
                    case MagicWordType.Fire:
                        fireIndex = i;
                        body = word;
                        mainCount++;
                        break;
                    case MagicWordType.Shield:
                        shieldIndex = i;
                        body = word;
                        mainCount++;
                        break;
                    case MagicWordType.Launch:
                        hasLaunch = true;
                        break;
                    case MagicWordType.Strong:
                        hasStrong = true;
                        break;
                }
            }

            if (mainCount == 0)
            {
                error = "메인 단어(화염 또는 방패)가 필요합니다.";
                return false;
            }

            if (fireIndex >= 0 && shieldIndex >= 0 && shieldIndex < fireIndex)
            {
                error = "화염으로 방패를 강화하려면 화염을 방패보다 앞에 놓으세요.";
                return false;
            }

            bool isBurning = fireIndex >= 0 && shieldIndex >= 0;
            string name = body == MagicWordType.Fire
                ? (hasLaunch ? "화염구" : "기본 화염")
                : (hasLaunch ? "투척 방패" : "방패");

            if (isBurning) name = "불붙은 " + name;
            if (hasStrong) name = "강화 " + name;

            // 특징 합집합 계산 (기획서 4절)
            SpellTrait traits = SpellTrait.None;
            if (fireIndex >= 0) traits |= SpellTrait.Fire;
            if (hasLaunch) traits |= SpellTrait.Projectile;
            if (hasStrong) traits |= SpellTrait.Heavy;
            if (shieldIndex >= 0)
                traits |= hasLaunch ? SpellTrait.Intercept : SpellTrait.Block;

            spell = new SpellData(body, hasLaunch, isBurning, hasStrong, group.Count, name, traits);
            return true;
        }
    }

    public readonly struct CombatCard
    {
        public int Id { get; }
        public MagicWordType Word { get; }

        public CombatCard(int id, MagicWordType word)
        {
            Id = id;
            Word = word;
        }
    }

    public sealed class CombatDeck
    {
        public const int MinSize = 3;
        public const int MaxSize = 8;
        public const int MaxPerWord = 2;
        public const int HandSize = 5;

        private readonly CombatCard[] _allCards;
        private readonly System.Random _random;
        private readonly List<CombatCard> _drawPile = new();
        private readonly List<CombatCard> _discardPile = new();
        private readonly List<CombatCard> _inHand = new();

        public int Seed { get; }
        public IReadOnlyList<CombatCard> DrawPile => _drawPile;
        public IReadOnlyList<CombatCard> DiscardPile => _discardPile;
        public IReadOnlyList<CombatCard> InHand => _inHand;

        private CombatDeck(CombatCard[] cards, int seed)
        {
            _allCards = cards;
            Seed = seed;
            _random = new System.Random(seed);
            _drawPile.AddRange(cards);
            Shuffle(_drawPile);
        }

        public static bool TryValidate(IEnumerable<MagicWordType> learned, IReadOnlyList<MagicWordType> words, out string error)
        {
            error = string.Empty;
            if (learned == null || words == null)
            {
                error = "덱 구성을 확인하세요.";
                return false;
            }

            if (words.Count < MinSize)
            {
                error = $"덱은 {MinSize}장 이상이어야 합니다. (현재 {words.Count}장)";
                return false;
            }

            if (words.Count > MaxSize)
            {
                error = $"덱은 {MaxSize}장 이하여야 합니다. (현재 {words.Count}장)";
                return false;
            }

            var known = new HashSet<MagicWordType>(learned);
            foreach (var group in words.GroupBy(w => w))
            {
                if (!known.Contains(group.Key))
                {
                    error = $"아직 습득하지 않은 단어입니다: {SpellResolver.WordName(group.Key)}";
                    return false;
                }

                if (group.Count() > MaxPerWord)
                {
                    error = $"{SpellResolver.WordName(group.Key)} 카드는 최대 {MaxPerWord}장까지 편성할 수 있습니다.";
                    return false;
                }
            }

            return true;
        }

        public static bool TryCreate(IEnumerable<MagicWordType> learned, IReadOnlyList<MagicWordType> words, int seed,
            out CombatDeck deck, out string error)
        {
            deck = null;
            if (!TryValidate(learned, words, out error))
                return false;

            var built = new CombatCard[words.Count];
            for (int i = 0; i < built.Length; i++)
                built[i] = new CombatCard(i, words[i]);

            deck = new CombatDeck(built, seed);
            return true;
        }

        public IReadOnlyList<CombatCard> Draw()
        {
            _inHand.Clear();
            for (int i = 0; i < HandSize; i++)
            {
                if (_drawPile.Count == 0)
                {
                    if (_discardPile.Count == 0) break;
                    _drawPile.AddRange(_discardPile);
                    _discardPile.Clear();
                    Shuffle(_drawPile);
                }

                int top = _drawPile.Count - 1;
                _inHand.Add(_drawPile[top]);
                _drawPile.RemoveAt(top);
            }

            return _inHand.ToArray();
        }

        public void DiscardHand()
        {
            if (_inHand.Count == 0) return;
            _discardPile.AddRange(_inHand);
            _inHand.Clear();
        }

        private void Shuffle(List<CombatCard> pile)
        {
            for (int i = pile.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (pile[i], pile[j]) = (pile[j], pile[i]);
            }
        }
    }
}
