using System;
using System.Collections.Generic;
using System.Linq;

namespace Asmegica
{
    // 편성 덱의 시험값과 시작 전 검사. 메인(화염·방패) 단어를 요구하지 않아 보조만 있는 손패도 허용한다.
    public static class DeckRules
    {
        public const int MinSize = 3;
        public const int MaxSize = 8;
        public const int MaxPerWord = 2;
        public const int HandSize = 4;

        // 습득한 단어만, 3~8장, 단어당 최대 2장인지 확인한다. 실패하면 한국어 사유를 돌려준다.
        public static bool TryValidate(IEnumerable<MagicWord> learned, IReadOnlyList<MagicWord> words, out string error)
        {
            error = string.Empty;
            if (learned == null || words == null)
            {
                error = "덱 구성을 확인하세요.";
                return false;
            }
            if (words.Count < MinSize)
            {
                error = "덱은 " + MinSize + "장 이상이어야 합니다. (현재 " + words.Count + "장)";
                return false;
            }
            if (words.Count > MaxSize)
            {
                error = "덱은 " + MaxSize + "장 이하여야 합니다. (현재 " + words.Count + "장)";
                return false;
            }

            var known = new HashSet<MagicWord>(learned);
            foreach (IGrouping<MagicWord, MagicWord> group in words.GroupBy(word => word))
            {
                if (!Enum.IsDefined(typeof(MagicWord), group.Key))
                {
                    error = "정의되지 않은 단어가 덱에 있습니다. (값: " + (int)group.Key + ")";
                    return false;
                }
                if (!known.Contains(group.Key))
                {
                    error = "아직 습득하지 않은 단어입니다: " + SpellResolver.WordName(group.Key);
                    return false;
                }
                if (group.Count() > MaxPerWord)
                {
                    error = SpellResolver.WordName(group.Key) + " 카드는 덱에 최대 " + MaxPerWord + "장입니다.";
                    return false;
                }
            }
            return true;
        }
    }

    // 시드로 섞은 편성 덱. 뽑을 더미에서 손패를 최대 4장 뽑고, 턴 종료 시 손패 전부를 버린 더미로 보낸다.
    // 뽑을 더미가 비면 버린 더미만 섞어 옮긴다. 카드 ID는 생성 시 한 번 정해져 바뀌지 않는다.
    public sealed class CardDeck
    {
        private readonly Card[] cards;
        private readonly Random random;
        private readonly List<Card> drawPile = new List<Card>();
        private readonly List<Card> discardPile = new List<Card>();
        private readonly List<Card> inHand = new List<Card>();

        // 생성 순서대로 한 번 정해진 전체 카드(ID 0..N-1).
        public IReadOnlyList<Card> Cards => cards;
        public IReadOnlyList<Card> DrawPile => drawPile;
        public IReadOnlyList<Card> DiscardPile => discardPile;
        public IReadOnlyList<Card> InHand => inHand;
        public int Total => drawPile.Count + discardPile.Count + inHand.Count;

        private CardDeck(Card[] cards, int seed)
        {
            this.cards = cards;
            random = new Random(seed);
            drawPile.AddRange(cards);
            Shuffle(drawPile);
        }

        // 편성이 규칙을 지키면 시드로 섞은 덱을 만든다. 실패하면 deck은 null이다.
        public static bool TryCreate(IEnumerable<MagicWord> learned, IReadOnlyList<MagicWord> words, int seed,
            out CardDeck deck, out string error)
        {
            deck = null;
            if (!DeckRules.TryValidate(learned, words, out error)) return false;
            var built = new Card[words.Count];
            for (int i = 0; i < built.Length; i++) built[i] = new Card(i, words[i]);
            deck = new CardDeck(built, seed);
            return true;
        }

        // 손패를 최대 4장 뽑는다. 뽑을 더미가 빌 때만 버린 더미를 섞어 옮긴다.
        public IReadOnlyList<Card> Draw()
        {
            if (inHand.Count > 0) throw new InvalidOperationException("버리지 않은 손패가 남아 있습니다.");
            for (int i = 0; i < DeckRules.HandSize; i++)
            {
                if (drawPile.Count == 0)
                {
                    if (discardPile.Count == 0) break;
                    drawPile.AddRange(discardPile);
                    discardPile.Clear();
                    Shuffle(drawPile);
                }
                int top = drawPile.Count - 1;
                inHand.Add(drawPile[top]);
                drawPile.RemoveAt(top);
            }
            return inHand.ToArray();
        }

        // 턴 종료 시 손패 전부를 버린다. 넘어온 카드는 뽑은 손패와 ID·단어가 정확히 같아야 한다.
        public void Discard(IReadOnlyList<Card> played)
        {
            bool exact = played != null && played.Count == inHand.Count
                && played.Select(c => c.Id).Distinct().Count() == played.Count
                && played.All(c => inHand.Any(h => h.Id == c.Id && h.Word == c.Word));
            if (!exact) throw new InvalidOperationException("버릴 카드가 뽑은 손패와 다릅니다.");
            discardPile.AddRange(inHand);
            inHand.Clear();
        }

        private void Shuffle(List<Card> pile)
        {
            for (int i = pile.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                Card swap = pile[i];
                pile[i] = pile[j];
                pile[j] = swap;
            }
        }
    }
}
