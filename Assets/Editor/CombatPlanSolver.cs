using System;
using System.Collections.Generic;
using System.Linq;
using TxRpg.Battle;
using TxRpg.Data;
using TxRpg.Magic;
using UnityEditor;
using UnityEngine;

namespace TxRpg.EditorTools
{
    /// <summary>
    /// 전투 개선 기획서 10~11절의 에디터 전용 검증 및 계획 열거 솔버.
    /// </summary>
    public static class CombatPlanSolver
    {
        [MenuItem("TxRpg/전투 개선 규칙 검증 (7절 A·B·C + 예지 일치)")]
        public static void RunRuleVerification()
        {
            var knight = AssetDatabase.LoadAssetAtPath<EnemyPatternSO>("Assets/Data/Monster/Pattern_ChargerKnight.asset");
            var shielder = AssetDatabase.LoadAssetAtPath<EnemyPatternSO>("Assets/Data/Monster/Pattern_Shielder.asset");
            var archer = AssetDatabase.LoadAssetAtPath<EnemyPatternSO>("Assets/Data/Monster/Pattern_Archer.asset");
            var boss = AssetDatabase.LoadAssetAtPath<EnemyPatternSO>("Assets/Data/Monster/Pattern_Boss.asset");

            Require(knight != null && shielder != null && archer != null && boss != null, "패턴 SO 로드");

            // --- 1. 기획서 7절 Plan A: 기존 습관 ---
            // [화염] 2칸(영창) → [방패+발사] 3칸 불붙은 투척 방패, [화염] 4칸, [화염] 5칸 -> 적 HP 12, 게이지 1, 내 피해 3
            var tA = new CombatTimeline(12, new[] { 20 }, new[] { knight }, manaPerTurn: 5);
            Require(tA.TryPlace(1, new[] { 0 }, 0, out _), "Plan A: 2칸 화염(영창)");
            Require(tA.TryPlace(2, new[] { 4, 3 }, 0, out _), "Plan A: 3칸 불붙은 투척 방패");
            Require(tA.GetOrder(2)?.Spell.IsBurning == true && tA.IsCastingSlot(1), "Plan A: 2~3칸 문장 이어짐");
            Require(tA.TryPlace(3, new[] { 1 }, 0, out _), "Plan A: 4칸 화염");
            Require(tA.TryPlace(4, new[] { 2 }, 0, out _), "Plan A: 5칸 화염");
            VerifyPreviewMatchesExecution(tA, "Plan A");
            Require(tA.GetEnemyHp(0) == 12 && tA.GetEnemyWeakGauge(0) == 1 && (12 - tA.PlayerHp) == 3,
                $"Plan A 불일치: HP={tA.GetEnemyHp(0)}, Gauge={tA.GetEnemyWeakGauge(0)}, Dmg={12 - tA.PlayerHp}");

            // --- 2. 기획서 7절 Plan B: 카운터 후 딜타임 ---
            // [화염] 2칸, [화염] 3칸(격파!), [화염+강하게] 5칸(서브는 칸을 차지하지 않음, 그로기 ×2 = 12) -> 적 HP 4, 게이지 2(회복), 내 피해 0
            var tB = new CombatTimeline(12, new[] { 20 }, new[] { knight }, manaPerTurn: 5);
            Require(tB.TryPlace(1, new[] { 0 }, 0, out _), "Plan B: 2칸 화염");
            Require(tB.TryPlace(2, new[] { 1 }, 0, out _), "Plan B: 3칸 화염");
            Require(tB.TryPlace(4, new[] { 5, 2 }, 0, out _), "Plan B: 5칸 강화 화염");
            VerifyPreviewMatchesExecution(tB, "Plan B");
            Require(tB.GetEnemyHp(0) == 4 && tB.GetEnemyWeakGauge(0) == 2 && tB.GetEnemyGroggyRemaining(0) == 0 && (12 - tB.PlayerHp) == 0,
                $"Plan B 불일치: HP={tB.GetEnemyHp(0)}, Gauge={tB.GetEnemyWeakGauge(0)}, Groggy={tB.GetEnemyGroggyRemaining(0)}, Dmg={12 - tB.PlayerHp}");

            // --- 3. 기획서 7절 Plan C: 원기옥 욕심 ---
            // [화염] 2칸, [화염+강하게] 4칸(영창) → [방패+발사] 5칸 거세게 불붙은 투척 방패 -> 4칸 피격으로 흩어짐 -> 적 HP 18, 게이지 1, 내 피해 3
            var tC = new CombatTimeline(12, new[] { 20 }, new[] { knight }, manaPerTurn: 5);
            Require(tC.TryPlace(1, new[] { 0 }, 0, out _), "Plan C: 2칸 화염");
            Require(tC.TryPlace(3, new[] { 1, 5 }, 0, out _), "Plan C: 4칸 강한 화염(영창)");
            Require(tC.TryPlace(4, new[] { 4, 3 }, 0, out _), "Plan C: 5칸 거세게 불붙은 투척 방패");
            VerifyPreviewMatchesExecution(tC, "Plan C");
            Require(tC.GetEnemyHp(0) == 18 && tC.GetEnemyWeakGauge(0) == 1 && (12 - tC.PlayerHp) == 3,
                $"Plan C 불일치: HP={tC.GetEnemyHp(0)}, Gauge={tC.GetEnemyWeakGauge(0)}, Dmg={12 - tC.PlayerHp}");

            // --- 4. 4칸 방패 막기 격파 -> 5칸 그로기(1/2) -> 다음 턴 1칸 그로기(2/2) 이월 검증 ---
            var tCarry = new CombatTimeline(12, new[] { 20 }, new[] { knight }, manaPerTurn: 5);
            // 화염 바로 다음 칸의 방패는 문장으로 이어지므로, 따로 쓰려고 3칸 화염에 발사를 붙인다
            Require(tCarry.TryPlace(2, new[] { 0, 3 }, 0, out _), "3칸 화염구 카운터 (게이지 2->1)");
            Require(tCarry.TryPlace(3, new[] { 4 }, 0, out _), "4칸 제자리 방패 막기 카운터 (게이지 1->0 격파! 5칸+다음턴 1칸 그로기)");
            VerifyPreviewMatchesExecution(tCarry, "Carryover Turn 1");
            Require(tCarry.GetEnemyWeakGauge(0) == 0 && tCarry.GetEnemyGroggyRemaining(0) == 1,
                $"턴 이월 그로기 실패: Gauge={tCarry.GetEnemyWeakGauge(0)}, Groggy={tCarry.GetEnemyGroggyRemaining(0)}");
            tCarry.NextTurn();
            Require(tCarry.IsEnemyGroggy(0) && tCarry.GetEnemyGroggyRemaining(0) == 1, "다음 턴 1칸 그로기 이월 유지");
            VerifyPreviewMatchesExecution(tCarry, "Carryover Turn 2");
            Require(!tCarry.IsEnemyGroggy(0) && tCarry.GetEnemyWeakGauge(0) == 2, "다음 턴 1칸 그로기 종료 후 게이지 최대치(2) 회복");

            // --- 5. 방패병: 방어 중 강타 미충족 시 추가 반감 vs 강타 카운터 ---
            var tShield = new CombatTimeline(12, new[] { 18 }, new[] { shielder }, manaPerTurn: 5);
            // 1칸 방어(원거리), 2칸 접근(근거리), 4칸 방어(근거리): 4칸에 [강하게+화염] 발동(3~4 점유) -> 카운터 성공
            Require(tShield.TryPlace(1, new[] { 0, 3 }, 0, out _), "방패병 2칸 접근 화염구 카운터 (게이지 2->1, 다음 칸 방패와 끊기 위해 발사)");
            Require(tShield.TryPlace(2, new[] { 4 }, 0, out _), "방패병 3칸 근접 제자리 방패 막기 카운터 (초과피해 1, 게이지 1->0 격파!)");
            VerifyPreviewMatchesExecution(tShield, "Shielder Block Counter");
            Require(tShield.GetEnemyGroggyRemaining(0) == 0 && tShield.GetEnemyWeakGauge(0) == 2 && (12 - tShield.PlayerHp) == 1,
                $"방패병 막기 카운터 검증 실패: HP={tShield.GetEnemyHp(0)}, Gauge={tShield.GetEnemyWeakGauge(0)}, PlayerDmg={12 - tShield.PlayerHp}");

            // --- 6. 궁수: 투척 방패 요격 카운터 + 커스텀 1칸 화염 카운터 ---
            var tArcher = new CombatTimeline(12, new[] { 16 }, new[] { archer }, manaPerTurn: 5);
            Require(tArcher.TryPlace(1, new[] { 3, 4 }, 0, out _), "궁수 2칸 투사체 [발사+방패] 요격 카운터");
            VerifyPreviewMatchesExecution(tArcher, "Archer Intercept");
            // 2칸: 투사체 3 중 2 차단(피해 1) + 요격 카운터(게이지 2->1), 4칸: 투사체 3 피격 -> 총 피해 4
            Require(tArcher.GetEnemyWeakGauge(0) == 1 && (12 - tArcher.PlayerHp) == 4,
                $"궁수 요격 실패: Gauge={tArcher.GetEnemyWeakGauge(0)}, PlayerDmg={12 - tArcher.PlayerHp}");

            // --- 7. 보스: 카운터 없는 턴 종료 시 게이지 1 회복 ---
            var tBoss = new CombatTimeline(30, new[] { 36 }, new[] { boss }, manaPerTurn: 5);
            Require(tBoss.TryPlace(1, new[] { 0 }, 0, out _), "보스 2칸 화염 카운터 (게이지 5->4)");
            VerifyPreviewMatchesExecution(tBoss, "Boss Turn 1");
            Require(tBoss.GetEnemyWeakGauge(0) == 4, "보스 1턴 후 게이지 4");
            tBoss.NextTurn();
            // 2턴에는 아무것도 배치하지 않음 -> 카운터 0회 -> 턴 종료 시 4->5 회복!
            VerifyPreviewMatchesExecution(tBoss, "Boss Turn 2 No Counter");
            Require(tBoss.GetEnemyWeakGauge(0) == 5, $"보스 게이지 회복 실패: {tBoss.GetEnemyWeakGauge(0)}");

            Debug.Log("[CombatPlanSolver] 모든 전투 개선 규칙(7절 A/B/C, 턴 이월 그로기, 막기/요격/강타 카운터, 보스 회복, 예지=실행 일치) 통과!");
        }

        [MenuItem("TxRpg/전투 인카운터 솔버 실행 (최적 계획 비교)")]
        public static void RunEncounterSolver()
        {
            var knight = AssetDatabase.LoadAssetAtPath<EnemyPatternSO>("Assets/Data/Monster/Pattern_ChargerKnight.asset");
            if (knight == null)
            {
                Debug.LogError("[CombatPlanSolver] Pattern_ChargerKnight.asset을 찾을 수 없습니다.");
                return;
            }

            // 돌격 기사(HP 20, 게이지 2, 그로기 2, 마나 5)에서 1턴 가능한 대표 계획군 열거
            var bestWithBreak = (score: int.MinValue, dmg: 0, taken: 99, desc: "없음");
            var bestWithoutBreak = (score: int.MinValue, dmg: 0, taken: 99, desc: "없음");
            int evaluated = 0;

            var baseTimeline = new CombatTimeline(12, new[] { 20 }, new[] { knight }, manaPerTurn: 5);
            var hand = baseTimeline.Hand;

            // 칸 하나에 놓을 수 있는 묶음(메인 1 + 서브 0~2) 후보. 이웃 칸 문장은 배치하면 타임라인이 알아서 잇는다.
            var stackCandidates = BuildStackCandidates(hand);

            // 재귀적으로 칸 묶음 배치 조합 탐색 (5칸, 마나 5 한도)
            void Search(int startSlot, int usedCardMask, int manaLeft, List<(int castSlot, int[] cards, string label)> current)
            {
                if (current.Count > 0)
                {
                    evaluated++;
                    var sim = new CombatTimeline(12, new[] { 20 }, new[] { knight }, manaPerTurn: 5);
                    bool ok = true;
                    foreach (var step in current)
                    {
                        if (!sim.TryPlace(step.castSlot, step.cards, 0, out _)) { ok = false; break; }
                    }
                    if (ok && sim.TryConfirm(out _))
                    {
                        bool broke = false;
                        while (sim.Phase == TimelinePhase.Execute)
                        {
                            if (sim.ResolveNextSlot(out var rep) && rep.EnemyResults.Any(r => r.TriggeredBreak))
                                broke = true;
                        }
                        int dealt = 20 - sim.GetEnemyHp(0);
                        int taken = 12 - sim.PlayerHp;
                        int score = dealt * 10 - taken * 12;
                        string desc = string.Join(" + ", current.Select(c => $"{c.castSlot + 1}칸[{c.label}]"));

                        if (broke && score > bestWithBreak.score)
                            bestWithBreak = (score, dealt, taken, desc);
                        else if (!broke && score > bestWithoutBreak.score)
                            bestWithoutBreak = (score, dealt, taken, desc);
                    }
                }

                if (manaLeft <= 0) return;

                foreach (var cand in stackCandidates)
                {
                    if (cand.cost > manaLeft) continue;
                    if ((usedCardMask & cand.mask) != 0) continue;

                    for (int slot = startSlot; slot < 5; slot++)
                    {
                        current.Add((slot, cand.cards, cand.name));
                        Search(slot + 1, usedCardMask | cand.mask, manaLeft - cand.cost, current);
                        current.RemoveAt(current.Count - 1);
                    }
                }
            }

            Search(0, 0, 5, new List<(int, int[], string)>());

            Debug.Log(
                $"[CombatPlanSolver] 돌격 기사(HP 20, 게이지 2) 탐색 완료 (유효 계획 {evaluated}개)\n" +
                $"  ▶ 그로기 격파 최적 계획: {bestWithBreak.desc} → 적 피해 {bestWithBreak.dmg}, 내 피해 {bestWithBreak.taken}\n" +
                $"  ▶ 비격파(원기옥 등) 최적 계획: {bestWithoutBreak.desc} → 적 피해 {bestWithoutBreak.dmg}, 내 피해 {bestWithoutBreak.taken}\n" +
                $"  ▶ 판정: {(bestWithBreak.dmg > bestWithoutBreak.dmg && bestWithoutBreak.dmg < 20 ? "통과 (비격파로는 처치 불가 · 그로기 격파가 압도적 우위)" : "조정 필요")}");
        }

        private static List<(int[] cards, int mask, int cost, string name)> BuildStackCandidates(IReadOnlyList<CombatCard> hand)
        {
            var results = new List<(int[], int, int, string)>();
            var subIdx = Enumerable.Range(0, hand.Count).Where(i => !SpellResolver.IsMainWord(hand[i].Word)).ToList();

            for (int m = 0; m < hand.Count; m++)
            {
                if (!SpellResolver.IsMainWord(hand[m].Word)) continue;

                for (int subset = 0; subset < (1 << subIdx.Count); subset++)
                {
                    var picked = new List<int> { m };
                    for (int b = 0; b < subIdx.Count; b++)
                        if ((subset & (1 << b)) != 0) picked.Add(subIdx[b]);

                    var words = picked.Select(i => hand[i].Word).ToArray();
                    if (!SpellResolver.CanStack(words[0], words.Skip(1).ToArray(), out _)) continue;

                    int mask = picked.Aggregate(0, (acc, i) => acc | (1 << i));
                    results.Add((picked.Select(i => hand[i].Id).ToArray(), mask, picked.Count,
                        string.Join("+", words.Select(SpellResolver.WordName))));
                }
            }

            return results;
        }

        [MenuItem("TxRpg/마법 칸 쌓기 규칙 검증")]
        public static void RunSlotStackVerification()
        {
            const MagicWordType F = MagicWordType.Fire, S = MagicWordType.Shield, L = MagicWordType.Launch, K = MagicWordType.Strong;
            SlotWords W(MagicWordType main, params MagicWordType[] subs) => new(main, subs);

            // --- 해석기: 칸 하나 ---
            Require(!SpellResolver.CanStack(F, new[] { S }, out _), "한 칸에 메인 두 장 거절");
            Require(!SpellResolver.CanStack(F, new[] { K, K }, out _), "같은 서브 두 장 거절");
            Require(!SpellResolver.CanStack(L, Array.Empty<MagicWordType>(), out _), "서브만 있는 칸 거절");

            SpellData fire = SpellResolver.BuildSpell(new[] { W(F) });
            Require(fire.Name == "기본 화염" && fire.ComputeBaseDamage(true) == 4 && fire.ComputeBaseDamage(false) == 0 && fire.Span == 1 && fire.Cost == 1, "기본 화염");
            SpellData fireball = SpellResolver.BuildSpell(new[] { W(F, L, K) });
            Require(fireball.Name == "강한 화염구" && fireball.ComputeBaseDamage(false) == 6 && fireball.Cost == 3 && fireball.Span == 1, "강한 화염구 (서브는 칸을 차지하지 않음)");
            SpellData hardThrown = SpellResolver.BuildSpell(new[] { W(S, L, K) });
            Require(hardThrown.ComputeBaseDamage(false) == 2 && hardThrown.GuardPower == 3 && hardThrown.HasTrait(SpellTrait.Heavy), "단단한 투척 방패: 피해 2, 막기 3");

            // --- 해석기: 문장 이어짐 ---
            Require(SpellResolver.Links(W(F, K), W(S)), "화염 → 방패 이어짐");
            Require(!SpellResolver.Links(W(F, L), W(S)), "발사된 화염은 방패와 이어지지 않음");
            Require(!SpellResolver.Links(W(S), W(F)), "방패 → 화염은 이어지지 않음");

            SpellData fierce = SpellResolver.BuildSpell(new[] { W(F, K), W(S, L) });
            Require(fierce.IsBurning && fierce.ComputeBaseDamage(false) == 10 && fierce.GuardPower == 2 && fierce.Span == 2 && fierce.Cost == 4,
                $"화염 강하게: 피해 10, 막기 2 (실제 {fierce.ComputeBaseDamage(false)}/{fierce.GuardPower})");
            SpellData hardBurning = SpellResolver.BuildSpell(new[] { W(F), W(S, L, K) });
            Require(hardBurning.ComputeBaseDamage(false) == 8 && hardBurning.GuardPower == 3,
                $"방패 강하게: 피해 8, 막기 3 (실제 {hardBurning.ComputeBaseDamage(false)}/{hardBurning.GuardPower})");

            // --- 퍼즐 입력 순서 해석 ---
            Require(SpellResolver.TryBuildSpell(new[] { F, S, L }, out var puzzle, out _) && puzzle.IsBurning && puzzle.IsLaunched, "퍼즐: 화염, 방패, 발사 = 불붙은 투척 방패");
            Require(!SpellResolver.TryBuildSpell(new[] { F, L, S }, out _, out _), "퍼즐: 화염, 발사, 방패 거절");
            Require(!SpellResolver.TryBuildSpell(new[] { L, F }, out _, out _), "퍼즐: 서브가 메인보다 먼저면 거절");
            Require(SpellResolver.TryBuildSpell(new[] { S, L }, out var thrown, out _) && thrown.Body == S && thrown.IsLaunched, "퍼즐: 방패, 발사 = 투척 방패");

            // --- 타임라인 클릭 조작 (손패: 0~2 화염, 3 발사, 4 방패, 5 강하게) ---
            var t = new CombatTimeline(12, new[] { 20 }, null, manaPerTurn: 5);
            Require(!t.TryAddCard(1, 3, 0, out _), "빈 칸에 서브 거절");
            Require(t.TryAddCard(1, 0, 0, out _) && t.TryAddCard(2, 4, 0, out _), "2칸 화염, 3칸 방패");
            Require(t.IsCastingSlot(1) && t.GetOrder(1) == null && t.GetOrder(2).Spell.Name == "불붙은 방패", "2칸 영창 → 3칸 불붙은 방패");
            Require(!t.TryAddCard(2, 1, 0, out _), "메인이 있는 칸에 메인 거절");
            Require(t.TryAddCard(2, 3, 0, out _) && t.GetOrder(2).Spell.IsLaunched && t.GetOrder(2).TargetEnemyIndex == 0, "3칸에 발사 → 불붙은 투척 방패");
            Require(t.TryAddCard(1, 5, 0, out _) && t.GetOrder(2).Spell.IsFireStrong && t.PlannedCost == 4, "2칸 화염에 강하게, 비용 4");
            Require(!t.TryAddCard(2, 5, 0, out _), "놓인 카드 재사용 거절");

            Require(t.TryRemoveCard(0, out _) && !t.IsCardReserved(5) && !t.GetOrder(2).Spell.IsBurning, "메인 회수 시 서브도 회수, 문장 끊김");
            Require(t.TryAddCard(1, 0, 0, out _) && t.TryAddCard(1, 5, 0, out _) && t.GetOrder(2).Spell.IsBurning, "다시 이어짐");
            Require(t.TryMove(2, 3, out _) && t.GetOrder(1) != null && t.GetOrder(3) != null && !t.GetOrder(3).Spell.IsBurning, "방패를 4칸으로 옮기면 끊김");
            Require(!t.TryMove(1, 3, out _), "놓인 칸으로 이동 거절");
            Require(t.TryRemove(3, out _) && t.TryRemove(1, out _) && t.PlannedCost == 0, "칸 비우기");

            Debug.Log("[CombatPlanSolver] 마법 칸 쌓기 규칙(칸 묶음, 서브 귀속, 화염→방패 문장, 퍼즐 입력, 클릭 조작) 통과!");
        }

        private static void VerifyPreviewMatchesExecution(CombatTimeline timeline, string tag)
        {
            PlanSimulationPreview preview = timeline.SimulateCurrentPlan();
            Require(timeline.TryConfirm(out string err), $"{tag} 확정 실패: {err}");
            while (timeline.Phase == TimelinePhase.Execute)
                timeline.ResolveNextSlot(out _);

            Require(preview.PredictedPlayerHp == timeline.PlayerHp,
                $"{tag} 예지/실행 PlayerHP 불일치: preview={preview.PredictedPlayerHp}, actual={timeline.PlayerHp}");
            for (int e = 0; e < timeline.EnemyCount; e++)
            {
                Require(preview.PredictedEnemyHp[e] == timeline.GetEnemyHp(e),
                    $"{tag} 예지/실행 Enemy{e + 1} HP 불일치");
                Require(preview.PredictedEnemyGauge[e] == timeline.GetEnemyWeakGauge(e),
                    $"{tag} 예지/실행 Enemy{e + 1} Gauge 불일치");
                Require(preview.PredictedEnemyGroggy[e] == timeline.GetEnemyGroggyRemaining(e),
                    $"{tag} 예지/실행 Enemy{e + 1} Groggy 불일치");
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception("[CombatPlanSolver] " + message);
        }
    }
}
