using System;
using System.Collections.Generic;
using System.Linq;
using TxRpg.Data;

namespace TxRpg.Magic
{
    /// <summary>
    /// 칸 하나에 쌓인 단어: 메인 1장 + 서브 0~2장 (Docs/TxRpg/마법-칸쌓기-상세기획서.md).
    /// </summary>
    public readonly struct SlotWords
    {
        public MagicWordType Main { get; }
        public IReadOnlyList<MagicWordType> Subs { get; }

        public SlotWords(MagicWordType main, IReadOnlyList<MagicWordType> subs = null)
        {
            Main = main;
            Subs = subs ?? Array.Empty<MagicWordType>();
        }

        public bool Has(MagicWordType sub) => Subs.Contains(sub);
        public int CardCount => 1 + Subs.Count;
    }

    /// <summary>
    /// SpellResolver.BuildSpell이 생성하는 불변 주문 결과.
    /// 기본 위력: 화염 4(화염 강하게 +2), 투척 방패 2, 불붙은 투척 방패 8(화염 강하게 +2), 막기 2(방패 강하게 +1).
    /// </summary>
    public sealed class SpellData
    {
        public MagicWordType Body { get; }
        public bool IsLaunched { get; }
        public bool IsBurning { get; }
        public bool IsFireStrong { get; }
        public bool IsShieldStrong { get; }
        /// <summary>마나 비용 = 문장에 놓인 카드 수.</summary>
        public int Cost { get; }
        /// <summary>타임라인에서 차지하는 칸 수 = 문장의 메인 단어 수. 앞 칸들은 영창.</summary>
        public int Span { get; }
        public string Name { get; }
        public SpellTrait Traits { get; }

        public bool IsStrong => IsFireStrong || IsShieldStrong;
        public bool IsSelfTarget => Body == MagicWordType.Shield && !IsLaunched;
        public int GuardPower => Body == MagicWordType.Shield ? 2 + (IsShieldStrong ? 1 : 0) : 0;

        public bool HasTrait(SpellTrait trait) => trait != SpellTrait.None && (Traits & trait) == trait;

        internal SpellData(
            MagicWordType body,
            bool isLaunched,
            bool isBurning,
            bool isFireStrong,
            bool isShieldStrong,
            int cost,
            int span,
            string name,
            SpellTrait traits)
        {
            Body = body;
            IsLaunched = isLaunched;
            IsBurning = isBurning;
            IsFireStrong = isFireStrong;
            IsShieldStrong = isShieldStrong;
            Cost = cost;
            Span = span;
            Name = name;
            Traits = traits;
        }

        /// <summary>
        /// 그로기/방어 배율 적용 전 기본 공격 위력 (+레벨 보너스).
        /// 화염 4, 투척 방패 2, 불붙은 투척 방패 8. 화염에 붙은 강하게만 피해 +2.
        /// </summary>
        public int ComputeBaseDamage(bool targetNear, int levelBonusDamage = 0)
        {
            if (!IsLaunched && !targetNear)
                return 0;

            int fireBonus = IsFireStrong ? 2 : 0;
            int damage = 0;
            if (Body == MagicWordType.Fire)
                damage = 4 + fireBonus;
            else if (Body == MagicWordType.Shield && IsLaunched)
                damage = IsBurning ? 8 + fireBonus : 2;

            if (damage > 0)
                damage += Math.Max(0, levelBonusDamage);

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

        /// <summary>칸 하나(메인 1 + 서브 0~2)가 규칙에 맞는지 검사합니다.</summary>
        public static bool CanStack(MagicWordType main, IReadOnlyList<MagicWordType> subs, out string error)
        {
            error = string.Empty;
            if (!Enum.IsDefined(typeof(MagicWordType), main) || !IsMainWord(main))
            {
                error = "칸에는 메인 단어(화염 또는 방패)를 먼저 놓으세요.";
                return false;
            }

            var seen = new HashSet<MagicWordType>();
            foreach (MagicWordType sub in subs ?? Array.Empty<MagicWordType>())
            {
                if (!Enum.IsDefined(typeof(MagicWordType), sub))
                {
                    error = $"정의되지 않은 단어입니다. ({(int)sub})";
                    return false;
                }
                if (IsMainWord(sub))
                {
                    error = "한 칸에는 메인 단어를 하나만 놓을 수 있습니다. 다른 칸을 선택하세요.";
                    return false;
                }
                if (!seen.Add(sub))
                {
                    error = $"같은 서브 단어는 한 칸에 하나만 붙일 수 있습니다: {WordName(sub)}";
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 이웃한 두 칸이 한 문장으로 이어지는지. v1은 [화염(발사 없음)] → [방패] 하나뿐.
        /// 발사가 붙은 화염은 이미 날아갔으므로 이어지지 않습니다.
        /// </summary>
        public static bool Links(SlotWords front, SlotWords back) =>
            front.Main == MagicWordType.Fire && !front.Has(MagicWordType.Launch) && back.Main == MagicWordType.Shield;

        /// <summary>
        /// 이어진 칸들(각 칸은 CanStack 통과, 이웃 칸은 Links 통과)을 마지막 칸에서 발동하는 주문 하나로 만듭니다.
        /// </summary>
        public static SpellData BuildSpell(IReadOnlyList<SlotWords> chain)
        {
            if (chain == null || chain.Count == 0)
                throw new ArgumentException("주문 칸이 없습니다.", nameof(chain));

            SlotWords body = chain[chain.Count - 1];
            bool hasFire = chain.Any(w => w.Main == MagicWordType.Fire);
            bool isBurning = body.Main == MagicWordType.Shield && hasFire;
            bool hasLaunch = body.Has(MagicWordType.Launch);
            bool fireStrong = chain.Any(w => w.Main == MagicWordType.Fire && w.Has(MagicWordType.Strong));
            bool shieldStrong = body.Main == MagicWordType.Shield && body.Has(MagicWordType.Strong);

            string name;
            if (body.Main == MagicWordType.Fire)
            {
                name = (fireStrong ? "강한 " : "") + (hasLaunch ? "화염구" : "기본 화염");
            }
            else
            {
                name = (shieldStrong ? "단단한 " : "") + (hasLaunch ? "투척 방패" : "방패");
                if (isBurning) name = (fireStrong ? "거세게 불붙은 " : "불붙은 ") + name;
            }

            SpellTrait traits = SpellTrait.None;
            if (hasFire) traits |= SpellTrait.Fire;
            if (hasLaunch) traits |= SpellTrait.Projectile;
            if (fireStrong || shieldStrong) traits |= SpellTrait.Heavy;
            if (body.Main == MagicWordType.Shield)
                traits |= hasLaunch ? SpellTrait.Intercept : SpellTrait.Block;

            return new SpellData(body.Main, hasLaunch, isBurning, fireStrong, shieldStrong,
                chain.Sum(w => w.CardCount), chain.Count, name, traits);
        }

        /// <summary>
        /// 타임라인이 없는 탐험 퍼즐용: 입력 순서를 칸으로 읽습니다.
        /// 메인이 새 칸을 시작하고 서브는 바로 앞 메인에 붙으며, 하나로 이어지는 문장만 시전할 수 있습니다.
        /// </summary>
        public static bool TryBuildSpell(IReadOnlyList<MagicWordType> words, out SpellData spell, out string error)
        {
            spell = null;
            error = string.Empty;

            if (words == null || words.Count == 0)
            {
                error = "주문에 넣을 단어를 선택하세요.";
                return false;
            }

            var mains = new List<MagicWordType>();
            var subs = new List<List<MagicWordType>>();
            foreach (MagicWordType word in words)
            {
                if (IsMainWord(word))
                {
                    mains.Add(word);
                    subs.Add(new List<MagicWordType>());
                }
                else if (mains.Count == 0)
                {
                    error = "서브 단어(발사·강하게)는 메인 단어(화염·방패) 뒤에 붙이세요.";
                    return false;
                }
                else
                {
                    subs[subs.Count - 1].Add(word);
                }
            }

            var chain = new List<SlotWords>(mains.Count);
            for (int i = 0; i < mains.Count; i++)
            {
                if (!CanStack(mains[i], subs[i], out error)) return false;
                chain.Add(new SlotWords(mains[i], subs[i]));
                if (i > 0 && !Links(chain[i - 1], chain[i]))
                {
                    error = "한 번에 하나로 이어지는 주문만 시전할 수 있습니다. (발사 없는 화염 → 방패만 이어집니다)";
                    return false;
                }
            }

            spell = BuildSpell(chain);
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
