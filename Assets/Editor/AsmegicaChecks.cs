using System;
using System.Collections.Generic;
using System.Linq;
using Asmegica;
using UnityEditor;
using UnityEngine;

public static class AsmegicaChecks
{
    [MenuItem("Asmegica/그레이박스 규칙 검사")]
    public static void Run()
    {
        CheckGroups(Array.Empty<MagicWord>());
        foreach (var invalid in new[] { null, new[] { (MagicWord)99, MagicWord.Fire } })
            Require(!SpellResolver.TryBuildSpell(invalid, out var rejected, out var reason)
                && rejected == null && !string.IsNullOrWhiteSpace(reason), "잘못된 단어 거절");
        Require(SpellResolver.TryBuildSpell(new[] { MagicWord.Fire, MagicWord.Launch },
            out var fireball, out _) && fireball.Name == "화염구", "화염구 이름");
        Require(SpellResolver.TryBuildSpell(new[] { MagicWord.Fire, MagicWord.Launch, MagicWord.Shield },
            out var shield, out _) && shield.Name == "불붙은 투척 방패", "투척 방패 이름");
        CheckFixedPlan(6, false, 5, 4);
        CheckFixedPlan(6, true, 3, 2);
        CheckFixedPlan(4, false, 5, 2);
        CheckFixedPlan(4, true, 6, 0);
        CheckPlanning();
        CheckShieldAndMana();
        Debug.Log("아스메지카 그레이박스 규칙 검사 통과");
    }

    private static readonly MagicWord[] AllWords = { MagicWord.Fire, MagicWord.Shield, MagicWord.Launch, MagicWord.Strong };

    [MenuItem("Asmegica/편성 덱 회귀 검사")]
    public static void RunDeck()
    {
        CheckDeckRules();
        CheckDeckConservation();
        CheckDeckNamesAndIds();
        CheckDeckSeed();
        CheckDeckBattleEconomy();
        Debug.Log("아스메지카 편성 덱 회귀 검사 통과");
    }

    private static void CheckDeckRules()
    {
        var pool = new[] { MagicWord.Fire, MagicWord.Fire, MagicWord.Shield, MagicWord.Shield,
            MagicWord.Launch, MagicWord.Launch, MagicWord.Strong, MagicWord.Strong };
        for (int size = DeckRules.MinSize; size <= DeckRules.MaxSize; size++)
            Require(DeckRules.TryValidate(AllWords, pool.Take(size).ToArray(), out _), "3~8장 편성 허용");
        Require(DeckRules.TryValidate(new[] { MagicWord.Launch, MagicWord.Strong },
            new[] { MagicWord.Launch, MagicWord.Launch, MagicWord.Strong }, out _), "보조만 있는 손패 허용");
        Require(!DeckRules.TryValidate(AllWords, new[] { MagicWord.Fire, MagicWord.Shield }, out var tooFew)
            && !string.IsNullOrEmpty(tooFew), "2장 덱 거절");
        Require(!DeckRules.TryValidate(AllWords, pool.Concat(new[] { MagicWord.Fire }).ToArray(), out var tooMany)
            && !string.IsNullOrEmpty(tooMany), "9장 덱 거절");
        Require(!DeckRules.TryValidate(AllWords, new[] { MagicWord.Fire, MagicWord.Fire, MagicWord.Fire, MagicWord.Shield },
            out var perWord) && !string.IsNullOrEmpty(perWord), "단어당 2장 초과 거절");
        Require(!DeckRules.TryValidate(new[] { MagicWord.Fire }, new[] { MagicWord.Fire, MagicWord.Shield, MagicWord.Shield },
            out var unlearned) && unlearned.Contains("습득"), "미습득 단어 거절");
        Require(!DeckRules.TryValidate(new[] { MagicWord.Shield }, new[] { MagicWord.Shield, MagicWord.Shield }, out _),
            "습득 단어 1개로는 3장을 채울 수 없음");
        Require(!DeckRules.TryValidate(AllWords, new[] { (MagicWord)99, MagicWord.Fire, MagicWord.Fire }, out _),
            "정의되지 않은 단어 거절");
        Require(!DeckRules.TryValidate(AllWords, null, out _) && !DeckRules.TryValidate(null, pool.Take(3).ToArray(), out _),
            "빈 입력 거절");
        Require(!CardDeck.TryCreate(new[] { MagicWord.Fire }, new[] { MagicWord.Fire, MagicWord.Shield, MagicWord.Shield },
            1, out var locked, out _) && locked == null, "미습득 단어로 덱 생성 거절");
    }

    private static void CheckDeckConservation()
    {
        var pool = new[] { MagicWord.Fire, MagicWord.Fire, MagicWord.Shield, MagicWord.Shield,
            MagicWord.Launch, MagicWord.Launch, MagicWord.Strong, MagicWord.Strong };
        for (int size = DeckRules.MinSize; size <= DeckRules.MaxSize; size++)
        {
            for (int seed = 1; seed <= 3; seed++)
            {
                Require(CardDeck.TryCreate(AllWords, pool.Take(size).ToArray(), seed, out var deck, out _), "덱 생성");
                int[] allIds = IdsOf(deck);
                var seen = new HashSet<int>();
                for (int turn = 0; turn < 3 * size; turn++)
                {
                    int drawBefore = deck.DrawPile.Count;
                    int discardBefore = deck.DiscardPile.Count;
                    IReadOnlyList<Card> hand = deck.Draw();
                    Require(hand.Count == Math.Min(DeckRules.HandSize, size), "손패는 최대 4장이고 덱보다 많지 않음");
                    Require(hand.Select(c => c.Id).Distinct().Count() == hand.Count, "손패 ID 중복 없음");
                    if (drawBefore >= hand.Count)
                        Require(deck.DiscardPile.Count == discardBefore, "뽑을 더미로 충분하면 버린 더미를 섞지 않음");
                    else
                        Require(deck.DiscardPile.Count == 0, "뽑을 더미가 빌 때만 버린 더미를 섞음");
                    RequireConserved(deck, allIds, "뽑은 뒤 카드 보존");
                    foreach (Card card in hand) seen.Add(card.Id);
                    deck.Discard(hand);
                    RequireConserved(deck, allIds, "버린 뒤 카드 보존");
                    Require(deck.InHand.Count == 0, "턴 종료 뒤 손패 비움");
                }
                Require(seen.Count == size, "모든 카드가 순환 중 한 번 이상 등장");
            }
        }
    }

    private static void CheckDeckNamesAndIds()
    {
        var words = new[] { MagicWord.Fire, MagicWord.Fire, MagicWord.Launch, MagicWord.Strong };
        Require(CardDeck.TryCreate(AllWords, words, 7, out var deck, out _), "이름·ID 검사용 덱 생성");
        Require(deck.Cards.Select(c => c.Id).SequenceEqual(new[] { 0, 1, 2, 3 }), "카드 ID는 0부터 고유하게 부여");
        Require(deck.Cards.Select(c => SpellResolver.WordName(c.Word))
            .SequenceEqual(new[] { "화염", "화염", "발사", "강하게" }), "카드 이름은 한국어 단어명");

        var wordById = new Dictionary<int, MagicWord>();
        for (int turn = 0; turn < 6; turn++)
        {
            foreach (Card card in deck.Draw())
            {
                if (!wordById.ContainsKey(card.Id)) wordById.Add(card.Id, card.Word);
                Require(wordById[card.Id] == card.Word, "같은 ID는 항상 같은 단어");
                Require(card.Word != MagicWord.Shield, "편성에 없는 단어는 나오지 않음");
            }
            deck.Discard(deck.InHand.ToArray());
        }

        Require(CardDeck.TryCreate(AllWords, words, 7, out var twinDeck, out _), "같은 이름 카드 덱 생성");
        var twins = new GrayboxBattle(1, 4, twinDeck.Draw, twinDeck.Discard, 6);
        int[] fires = twins.Hand.Where(c => c.Word == MagicWord.Fire).Select(c => c.Id).ToArray();
        int launch = twins.Hand.Single(c => c.Word == MagicWord.Launch).Id;
        Require(fires.Length == 2 && fires[0] != fires[1], "같은 이름의 화염 카드는 서로 다른 ID");
        Require(twins.TryPlace(0, new[] { fires[0], launch }, 0, out _) && twins.TryPlace(1, new[] { fires[1] }, 0, out _),
            "같은 이름의 카드를 서로 다른 주문에 예약");
        Require(!twins.TryPlace(2, new[] { fires[0] }, 0, out _), "예약된 카드 재사용 거절");
        Require(twins.TryRemove(0, out _) && !twins.IsReserved(fires[0]) && twins.TryPlace(2, new[] { fires[0] }, 0, out _),
            "해제한 카드는 다시 예약 가능");
    }

    private static void CheckDeckSeed()
    {
        var words = new[] { MagicWord.Fire, MagicWord.Shield, MagicWord.Launch, MagicWord.Strong,
            MagicWord.Fire, MagicWord.Shield, MagicWord.Launch, MagicWord.Strong };
        int[] first = HandSequence(words, 42);
        Require(HandSequence(words, 42).SequenceEqual(first), "같은 시드는 같은 순서로 재현");
        bool varies = false;
        for (int seed = 1; seed <= 10 && !varies; seed++) varies = !HandSequence(words, seed).SequenceEqual(first);
        Require(varies, "다른 시드는 다른 순서를 낼 수 있음");
    }

    private static void CheckDeckBattleEconomy()
    {
        // 일반 인카운터: 적 1~3마리, 적 체력 4, 내 체력 6·10·14
        foreach (int enemies in new[] { 1, 2, 3 })
        {
            foreach (int playerHp in new[] { 6, 10, 14 })
            {
                Require(CardDeck.TryCreate(AllWords, AllWords, enemies * 100 + playerHp, out var deck, out _), "일반 덱 생성");
                var battle = new GrayboxBattle(enemies, 4, deck.Draw, deck.Discard, playerHp);
                Require(battle.EnemyCount == enemies && battle.PlayerHp == playerHp && battle.Mana == 3 && battle.Hand.Count == 4,
                    "일반 전투 시작값");
                for (int e = 0; e < enemies; e++) Require(battle.GetEnemyHp(e) == 4, "일반 전투 적 체력 4");
                RunEmptyPlans(battle, deck, IdsOf(deck), 4);
            }
        }

        // 보조 단어만 있는 손패: 메인이 없어 배치는 거절되지만 빈 계획 확정은 허용된다.
        Require(CardDeck.TryCreate(new[] { MagicWord.Launch, MagicWord.Strong },
            new[] { MagicWord.Launch, MagicWord.Launch, MagicWord.Strong }, 9, out var modifiers, out _), "보조만 있는 덱 생성");
        var modifierBattle = new GrayboxBattle(2, 4, modifiers.Draw, modifiers.Discard, 10);
        Require(!modifierBattle.TryPlace(0, new[] { modifierBattle.Hand[0].Id }, 0, out _), "메인 없는 주문 배치 거절");
        RunEmptyPlans(modifierBattle, modifiers, IdsOf(modifiers), 3);

        // 마나 부족: 네 단어 주문(비용 4)은 마나 3에서 확정되지 않고, 편집은 그대로 남는다.
        Require(CardDeck.TryCreate(AllWords, AllWords, 5, out var full, out _), "마나 검사 덱 생성");
        var costly = new GrayboxBattle(1, 4, full.Draw, full.Discard, 10);
        int fire = costly.Hand.Single(c => c.Word == MagicWord.Fire).Id;
        int shield = costly.Hand.Single(c => c.Word == MagicWord.Shield).Id;
        int launch = costly.Hand.Single(c => c.Word == MagicWord.Launch).Id;
        int strong = costly.Hand.Single(c => c.Word == MagicWord.Strong).Id;
        Require(costly.TryPlace(0, new[] { fire, launch, strong, shield }, 0, out _), "비용 4 주문 편집");
        Require(!costly.TryConfirm(out string shortage) && shortage.Contains("부족") && costly.Mana == 3
            && costly.Phase == RoundPhase.Plan, "마나 부족 시 확정 거절과 마나 보존");
        Require(costly.TryRemove(0, out _) && costly.TryPlace(0, new[] { fire, launch, strong }, 0, out _)
            && costly.TryConfirm(out _) && costly.Mana == 0, "비용 3 주문 확정 뒤 마나 소모");
        for (int n = 0; n < 5; n++) costly.Step();
        RequireConserved(full, IdsOf(full), "마나 검사 뒤 카드 보존");
    }

    // 빈 계획으로 턴을 돌리며 라운드마다 카드 보존과 손패 장수를 확인한다. 전멸하거나 쓰러지면 멈춘다.
    private static void RunEmptyPlans(GrayboxBattle battle, CardDeck deck, int[] allIds, int turns)
    {
        for (int turn = 0; turn < turns; turn++)
        {
            Require(battle.TryConfirm(out _) && battle.Mana == 3, "빈 계획 확정은 마나를 쓰지 않음");
            for (int step = 0; step < 5 && battle.Phase == RoundPhase.Execute; step++) battle.Step();
            RequireConserved(deck, allIds, "라운드 종료 뒤 카드 보존");
            if (battle.Phase != RoundPhase.TurnEnd) return;
            battle.NextTurn();
            RequireConserved(deck, allIds, "다음 턴 뽑기 뒤 카드 보존");
            Require(battle.Hand.Count == Math.Min(DeckRules.HandSize, allIds.Length), "다음 턴 손패 장수");
        }
    }

    // 뽑을 더미·손패·버린 더미를 합친 ID가 생성 시 전체 ID와 정확히 일치해야 한다(중복·분실 없음).
    private static void RequireConserved(CardDeck deck, int[] allIds, string check)
    {
        int[] piled = deck.DrawPile.Concat(deck.InHand).Concat(deck.DiscardPile).Select(c => c.Id).OrderBy(id => id).ToArray();
        Require(piled.SequenceEqual(allIds) && deck.Total == allIds.Length, check);
    }

    private static int[] IdsOf(CardDeck deck) => deck.Cards.Select(c => c.Id).OrderBy(id => id).ToArray();

    // 같은 시드의 손패 순서를 여러 턴 기록한다. 턴 경계는 -1로 표시한다.
    private static int[] HandSequence(MagicWord[] words, int seed)
    {
        Require(CardDeck.TryCreate(AllWords, words, seed, out var deck, out _), "시드 덱 생성");
        var sequence = new List<int>();
        for (int turn = 0; turn < 8; turn++)
        {
            IReadOnlyList<Card> hand = deck.Draw();
            sequence.AddRange(hand.Select(c => c.Id));
            sequence.Add(-1);
            deck.Discard(hand);
        }
        return sequence.ToArray();
    }

    private static void CheckGroups(MagicWord[] group)
    {
        var mains = group.Where(word => word == MagicWord.Fire || word == MagicWord.Shield).ToArray();
        bool valid = group.Distinct().Count() == group.Length && (mains.Length == 1 ||
            mains.SequenceEqual(new[] { MagicWord.Fire, MagicWord.Shield }));
        var original = group.ToArray();
        bool built = SpellResolver.TryBuildSpell(group, out var spell, out var reason);
        Require(built == valid && group.SequenceEqual(original), "주문 유효성과 입력 보존");
        if (built)
            Require(spell.Cost == group.Length && spell.Body == mains.Last()
                && spell.IsLaunched == group.Contains(MagicWord.Launch) && spell.IsBurning == (mains.Length == 2)
                && spell.IsStrong == group.Contains(MagicWord.Strong), "주문 해석값");
        else
            Require(spell == null && !string.IsNullOrWhiteSpace(reason), "무효 주문의 사유");
        if (group.Length < 4)
            foreach (MagicWord word in Enum.GetValues(typeof(MagicWord)))
                CheckGroups(group.Concat(new[] { word }).ToArray());
    }

    private static void Require(bool success, string check)
    {
        if (!success) throw new InvalidOperationException("그레이박스 검사 실패 · " + check);
    }

    private static void CheckFixedPlan(int startingHp, bool combine, int playerHp, int enemyHp)
    {
        var battle = new GrayboxBattle(1, startingHp);
        var fire = battle.Hand.Single(c => c.Word == MagicWord.Fire).Id;
        var launch = battle.Hand.Single(c => c.Word == MagicWord.Launch).Id;
        var shield = battle.Hand.Single(c => c.Word == MagicWord.Shield).Id;
        var group = combine ? new[] { fire, launch, shield } : new[] { fire, launch };
        Require(battle.TryPlace(1, group, 0, out _), "2칸 공격 배치");
        if (!combine) Require(battle.TryPlace(3, new[] { shield }, 0, out _), "4칸 방패 배치");
        Require(battle.TryConfirm(out _), "첫 확정");
        Require(!battle.TryConfirm(out _) && battle.Mana == 0, "중복 결제 방지");
        for (int i = 0; i < 5; i++) battle.Step();
        Require(battle.PlayerHp == playerHp && battle.GetEnemyHp(0) == enemyHp && battle.Mana == 0,
            "고정 손패 A/B · HP " + startingHp);
    }

    private static void CheckPlanning()
    {
        foreach (int count in new[] { 1, 2, 3 })
        {
            var battle = new GrayboxBattle(count, 4);
            int fire = battle.Hand.Single(c => c.Word == MagicWord.Fire).Id;
            var ids = battle.Hand.Select(c => c.Id).ToArray();
            Require(!battle.TryPlace(0, new[] { fire, fire }, 0, out _), "카드 중복 거절");
            Require(battle.TryPlace(0, ids, count - 1, out _), "1~3마리 중 대상 선택");
            Require(!battle.TryPlace(2, new[] { fire }, 0, out _), "칸 간 이중 예약 방지");
            Require(battle.TryMove(0, 1, out _) && battle.TryRetarget(1, 0, out _), "이동·대상 변경");
            Require(battle.TryRemove(1, out _) && battle.Mana == 3 && !battle.IsReserved(fire), "해제 환원");
            Require(battle.TryPlace(1, ids, 0, out _) && battle.TryConfirm(out _), "재배치·확정");
            for (int i = 0; i < 5; i++) battle.Step();
            Require(battle.GetEnemyHp(0) == 0, "대상 처치");
            if (count > 1)
                Require(battle.GetEnemyHp(1) == 4 && battle.PlayerHp == (count == 2 ? 4 : 1), "다른 적의 행동 유지");
        }
    }

    private static void CheckShieldAndMana()
    {
        var hand = new[] { new Card(0, MagicWord.Fire), new Card(1, MagicWord.Shield),
            new Card(2, MagicWord.Launch), new Card(3, MagicWord.Strong) };
        var battle = new GrayboxBattle(3, 6, () => hand, playerHp: 12);
        Require(battle.TryPlace(3, new[] { 1, 2, 3 }, 0, out _), "강화 투척 방패 예약");
        Require(battle.TryPlace(4, new[] { 0 }, 0, out _), "초과 비용 편집");
        Require(!battle.TryConfirm(out _) && battle.Mana == 3 && battle.Phase == RoundPhase.Plan,
            "마나 부족 시 편집과 마나 보존");
        Require(battle.TryRemove(4, out _) && battle.TryConfirm(out _), "비용 수정 후 확정");
        for (int n = 0; n < 5; n++) battle.Step();
        Require(battle.PlayerHp == 7 && battle.GetEnemyHp(0) == 4, "지정 투사체만 차단 · 다른 적 피해 유지");
        var shared = new GrayboxBattle(3, 6, playerHp: 12);
        Require(shared.TryPlace(3, new[] { 2 }, 0, out _) && shared.TryConfirm(out _), "기본 방패 예약");
        for (int n = 0; n < 5; n++) shared.Step();
        Require(shared.PlayerHp == 6, "동시 공격에 방어력을 한 번만 사용");
        var simultaneous = new GrayboxBattle(1, 2, playerHp: 3);
        Require(simultaneous.TryPlace(3, new[] { 0, 1 }, 0, out _) && simultaneous.TryConfirm(out _), "동시 공격 예약");
        for (int n = 0; n < 5; n++) simultaneous.Step();
        Require(simultaneous.Phase == RoundPhase.Defeat && simultaneous.GetEnemyHp(0) == 0 && simultaneous.PlayerHp == 0,
            "동시 전멸 판정");
    }
}
