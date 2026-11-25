using System;
using System.Collections.Generic;
using System.Linq;
using TxRpg.Data;
using TxRpg.Magic;

namespace TxRpg.Battle
{
    public enum TimelinePhase
    {
        Plan,
        Execute,
        TurnEnd,
        Victory,
        Defeat
    }

    public enum IntentOutcome
    {
        Pending,
        Executed,
        Blocked,
        Cancelled,
        Missed,
        Groggy,
        Countered,
        Break
    }

    public sealed class SlotOrder
    {
        public int[] CardIds;
        public SpellData Spell;
        public int TargetEnemyIndex;

        public SlotOrder Clone() => new()
        {
            CardIds = (int[])CardIds.Clone(),
            Spell = Spell,
            TargetEnemyIndex = TargetEnemyIndex
        };
    }

    public sealed class EnemySlotResult
    {
        public int EnemyIndex;
        public EnemyIntentSlot Intent;
        public IntentOutcome Outcome;
        public int BlockedAmount;
        public int DamageToPlayer;
        public bool MovedNear;
        public bool MovedFar;
        public bool WasGroggy;
        public bool Countered;
        public bool TriggeredBreak;
        public bool GroggyRecovered;
        public int GaugeAfter;
        public int GroggyAfter;
    }

    public sealed class SlotStepReport
    {
        public int SlotIndex;
        public SlotOrder PlayerOrder;
        public int GuardActivated;
        public int PlayerDamageDealt;
        public bool WasGroggyHit;
        public bool PlayerSpellMissed;
        public bool TargetKilled;
        public bool ChantInterrupted;
        public string InterruptedSpellName;
        public int TotalDamageToPlayer;
        public List<EnemySlotResult> EnemyResults = new();
        public string SummaryLine;
    }

    public sealed class PlanSimulationPreview
    {
        public SlotStepReport[] SlotReports;
        public int PredictedPlayerHp;
        public int[] PredictedEnemyHp;
        public int[] PredictedEnemyGauge;
        public int[] PredictedEnemyGroggy;
        public TimelinePhase PredictedPhase;
        public List<string> Highlights = new();
    }

    /// <summary>
    /// 전투 개선 기획서(약점 게이지 · 카운터 · 그로기 · 영창 시간/방해 · 1vs1~3)를 구현하는 결정론적 전투 모델.
    /// 편집 중 복제본 시뮬레이션(SimulateCurrentPlan)으로 예지와 실제 실행 결과의 100% 일치를 보장합니다.
    /// </summary>
    public sealed class CombatTimeline
    {
        public const int DefaultSlotCount = 5;
        public const int DefaultManaPerTurn = 5;

        private readonly int _slotCount;
        private readonly int _manaPerTurn;
        private readonly int _playerLevelBonus;
        private readonly SlotOrder[] _orders;
        private readonly int[] _enemyHp;
        private readonly int[] _enemyMaxHp;
        private readonly bool[] _enemyNear;
        private readonly int[] _enemyWeakGauge;
        private readonly int[] _enemyMaxWeakGauge;
        private readonly int[] _enemyGroggy;
        private readonly int[] _enemyGroggyDuration;
        private readonly bool[] _enemyRecoverOnNoCounter;
        private readonly bool[] _enemyCounteredThisTurn;
        private readonly string[] _enemyNames;
        private readonly EnemyIntentSlot[,] _intents;
        private readonly IntentOutcome[,] _outcomes;
        private readonly string[,] _outcomeNotes;
        private readonly List<string> _log = new();
        private readonly Func<IReadOnlyList<CombatCard>> _drawFunc;
        private readonly Action _discardFunc;

        public int SlotCount => _slotCount;
        public int EnemyCount => _enemyHp.Length;
        public int PlayerHp { get; private set; }
        public int PlayerMaxHp { get; private set; }
        public int PlayerLevelBonus => _playerLevelBonus;
        public int Mana { get; private set; }
        public int MaxMana => _manaPerTurn;
        public int Turn { get; private set; }
        public int CurrentSlot { get; private set; }
        public TimelinePhase Phase { get; private set; }
        public IReadOnlyList<CombatCard> Hand { get; private set; } = Array.Empty<CombatCard>();
        public IReadOnlyList<string> Log => _log;
        public int PlannedCost => _orders.Where(o => o != null).Sum(o => o.Spell.Cost);

        public CombatTimeline(
            int playerMaxHp,
            int[] enemyMaxHps,
            EnemyPatternSO[] enemyPatterns,
            int slotCount = DefaultSlotCount,
            int manaPerTurn = DefaultManaPerTurn,
            Func<IReadOnlyList<CombatCard>> drawFunc = null,
            Action discardFunc = null,
            int playerLevelBonus = 0,
            int[] weakGaugeOverrides = null,
            int[] groggyDurationOverrides = null)
        {
            if (enemyMaxHps == null || enemyMaxHps.Length < 1 || enemyMaxHps.Length > 3)
                throw new ArgumentOutOfRangeException(nameof(enemyMaxHps), "적은 1~3명이어야 합니다.");

            _slotCount = Math.Max(1, slotCount);
            _manaPerTurn = Math.Max(1, manaPerTurn);
            _playerLevelBonus = Math.Max(0, playerLevelBonus);
            _orders = new SlotOrder[_slotCount];

            PlayerMaxHp = Math.Max(1, playerMaxHp);
            PlayerHp = PlayerMaxHp;

            int count = enemyMaxHps.Length;
            _enemyHp = new int[count];
            _enemyMaxHp = new int[count];
            _enemyNear = new bool[count];
            _enemyWeakGauge = new int[count];
            _enemyMaxWeakGauge = new int[count];
            _enemyGroggy = new int[count];
            _enemyGroggyDuration = new int[count];
            _enemyRecoverOnNoCounter = new bool[count];
            _enemyCounteredThisTurn = new bool[count];
            _enemyNames = new string[count];
            _intents = new EnemyIntentSlot[count, _slotCount];
            _outcomes = new IntentOutcome[count, _slotCount];
            _outcomeNotes = new string[count, _slotCount];

            for (int e = 0; e < count; e++)
            {
                EnemyPatternSO pat = enemyPatterns != null && e < enemyPatterns.Length ? enemyPatterns[e] : null;
                _enemyMaxHp[e] = Math.Max(1, enemyMaxHps[e]);
                _enemyHp[e] = _enemyMaxHp[e];

                int maxGauge = weakGaugeOverrides != null && e < weakGaugeOverrides.Length && weakGaugeOverrides[e] > 0
                    ? weakGaugeOverrides[e]
                    : (pat != null && pat.maxWeakGauge > 0 ? pat.maxWeakGauge : 2);
                int groggyDur = groggyDurationOverrides != null && e < groggyDurationOverrides.Length && groggyDurationOverrides[e] > 0
                    ? groggyDurationOverrides[e]
                    : (pat != null && pat.groggyDuration > 0 ? pat.groggyDuration : 2);

                _enemyMaxWeakGauge[e] = maxGauge;
                _enemyWeakGauge[e] = maxGauge;
                _enemyGroggy[e] = 0;
                _enemyGroggyDuration[e] = groggyDur;
                _enemyRecoverOnNoCounter[e] = pat != null && pat.recoverGaugeOnNoCounterTurn;
                _enemyNames[e] = pat != null && !string.IsNullOrWhiteSpace(pat.patternName) ? pat.patternName : $"적 {e + 1}";

                for (int s = 0; s < _slotCount; s++)
                {
                    _intents[e, s] = pat != null ? pat.GetSlot(s) : GetDefaultIntent(e, s);
                }
            }

            _drawFunc = drawFunc ?? CreateSection7FixedHand;
            _discardFunc = discardFunc;

            BeginTurn();
        }

        // 시뮬레이션 전용 프라이빗 복제 생성자
        private CombatTimeline(CombatTimeline src)
        {
            _slotCount = src._slotCount;
            _manaPerTurn = src._manaPerTurn;
            _playerLevelBonus = src._playerLevelBonus;
            _orders = src._orders.Select(o => o?.Clone()).ToArray();
            _enemyHp = (int[])src._enemyHp.Clone();
            _enemyMaxHp = (int[])src._enemyMaxHp.Clone();
            _enemyNear = (bool[])src._enemyNear.Clone();
            _enemyWeakGauge = (int[])src._enemyWeakGauge.Clone();
            _enemyMaxWeakGauge = (int[])src._enemyMaxWeakGauge.Clone();
            _enemyGroggy = (int[])src._enemyGroggy.Clone();
            _enemyGroggyDuration = (int[])src._enemyGroggyDuration.Clone();
            _enemyRecoverOnNoCounter = (bool[])src._enemyRecoverOnNoCounter.Clone();
            _enemyCounteredThisTurn = (bool[])src._enemyCounteredThisTurn.Clone();
            _enemyNames = (string[])src._enemyNames.Clone();
            _intents = (EnemyIntentSlot[,])src._intents.Clone();
            _outcomes = (IntentOutcome[,])src._outcomes.Clone();
            _outcomeNotes = (string[,])src._outcomeNotes.Clone();

            PlayerHp = src.PlayerHp;
            PlayerMaxHp = src.PlayerMaxHp;
            Mana = src.Mana;
            Turn = src.Turn;
            CurrentSlot = src.CurrentSlot;
            Phase = src.Phase;
            Hand = src.Hand;
        }

        /// <summary>
        /// 전투 개선 기획서 7절 예시 손패 (화염 3, 발사 1, 방패 1, 강하게 1).
        /// </summary>
        public static IReadOnlyList<CombatCard> CreateSection7FixedHand() => new[]
        {
            new CombatCard(0, MagicWordType.Fire),
            new CombatCard(1, MagicWordType.Fire),
            new CombatCard(2, MagicWordType.Fire),
            new CombatCard(3, MagicWordType.Launch),
            new CombatCard(4, MagicWordType.Shield),
            new CombatCard(5, MagicWordType.Strong)
        };

        private static EnemyIntentSlot GetDefaultIntent(int enemyIndex, int slotIndex)
        {
            EnemyIntentSlot[,] fallback =
            {
                {
                    new(EnemyIntentKind.Wait),
                    new(EnemyIntentKind.Approach),
                    new(EnemyIntentKind.Prepare),
                    new(EnemyIntentKind.Melee, 3),
                    new(EnemyIntentKind.Wait)
                },
                {
                    new(EnemyIntentKind.Defend),
                    new(EnemyIntentKind.Approach),
                    new(EnemyIntentKind.Melee, 3),
                    new(EnemyIntentKind.Defend),
                    new(EnemyIntentKind.Wait)
                },
                {
                    new(EnemyIntentKind.Wait),
                    new(EnemyIntentKind.Missile, 2),
                    new(EnemyIntentKind.Retreat),
                    new(EnemyIntentKind.Missile, 3),
                    new(EnemyIntentKind.Wait)
                }
            };
            return fallback[enemyIndex % 3, slotIndex % 5];
        }

        private void BeginTurn()
        {
            Hand = Array.AsReadOnly(_drawFunc().ToArray());
            Array.Clear(_orders, 0, _orders.Length);
            Array.Clear(_enemyCounteredThisTurn, 0, _enemyCounteredThisTurn.Length);
            CurrentSlot = 0;
            Turn++;
            Mana = _manaPerTurn;
            Phase = TimelinePhase.Plan;

            for (int e = 0; e < EnemyCount; e++)
            {
                for (int s = 0; s < _slotCount; s++)
                {
                    _outcomes[e, s] = IsEnemyAlive(e) ? IntentOutcome.Pending : IntentOutcome.Cancelled;
                    _outcomeNotes[e, s] = IsEnemyAlive(e) ? string.Empty : "처치됨";
                }
            }
        }

        public void NextTurn()
        {
            if (Phase == TimelinePhase.TurnEnd)
                BeginTurn();
        }

        public int GetEnemyHp(int e) => _enemyHp[e];
        public int GetEnemyMaxHp(int e) => _enemyMaxHp[e];
        public string GetEnemyName(int e) => _enemyNames[e];
        public int GetEnemyWeakGauge(int e) => _enemyWeakGauge[e];
        public int GetEnemyMaxWeakGauge(int e) => _enemyMaxWeakGauge[e];
        public int GetEnemyGroggyRemaining(int e) => _enemyGroggy[e];
        public bool IsEnemyGroggy(int e) => e >= 0 && e < EnemyCount && _enemyGroggy[e] > 0;
        public bool IsEnemyAlive(int e) => e >= 0 && e < EnemyCount && _enemyHp[e] > 0;
        public bool IsEnemyNear(int e) => _enemyNear[e];
        public EnemyIntentSlot GetIntent(int e, int s) => _intents[e, s];
        public IntentOutcome GetOutcome(int e, int s) => _outcomes[e, s];
        public string GetOutcomeNote(int e, int s) => _outcomeNotes[e, s];
        public SlotOrder GetOrder(int s) => _orders[s];

        public int GetOrderCastSlot(int s)
        {
            if (s < 0 || s >= _slotCount) return -1;
            if (_orders[s] != null) return s;

            for (int cast = s + 1; cast < _slotCount; cast++)
            {
                SlotOrder order = _orders[cast];
                if (order == null) continue;
                int start = cast - (order.Spell.Cost - 1);
                if (start <= s) return cast;
            }
            return -1;
        }

        public bool IsSlotOccupied(int s) => GetOrderCastSlot(s) >= 0;
        public bool IsCastingSlot(int s) => GetOrderCastSlot(s) > s;
        public bool IsCardReserved(int cardId) => _orders.Any(o => o != null && Array.IndexOf(o.CardIds, cardId) >= 0);

        public bool CanPlaceSpan(int castSlot, int wordCount, int ignoreCastSlot, out string error)
        {
            error = string.Empty;
            if (wordCount < 1)
            {
                error = "주문 단어가 없습니다.";
                return false;
            }

            int startSlot = castSlot - (wordCount - 1);
            if (startSlot < 0)
            {
                error = $"{wordCount}글자 주문은 {wordCount}칸부터 배치할 수 있습니다.";
                return false;
            }

            for (int k = startSlot; k <= castSlot; k++)
            {
                int owner = GetOrderCastSlot(k);
                if (owner >= 0 && owner != ignoreCastSlot)
                {
                    error = wordCount > 1
                        ? $"{startSlot + 1}~{castSlot + 1}칸에 이미 다른 주문(또는 영창)이 있어 배치할 수 없습니다."
                        : "이미 주문(또는 영창)이 배치된 칸입니다.";
                    return false;
                }
            }

            return true;
        }

        public int FirstAliveEnemyIndex()
        {
            for (int e = 0; e < EnemyCount; e++)
                if (IsEnemyAlive(e)) return e;
            return 0;
        }

        public string GetPlanLabel(int s)
        {
            SlotOrder order = _orders[s];
            if (order != null)
            {
                return order.Spell.IsSelfTarget
                    ? $"★ {order.Spell.Name} · 자신"
                    : $"★ {order.Spell.Name} → 적 {order.TargetEnemyIndex + 1}";
            }

            int castSlot = GetOrderCastSlot(s);
            if (castSlot > s)
            {
                return $"영창 중 ({_orders[castSlot].Spell.Name} → {castSlot + 1}칸)";
            }

            return "빈 칸";
        }

        private bool CheckEditable(int slotIndex, out string error)
        {
            error = string.Empty;
            if (Phase != TimelinePhase.Plan)
            {
                error = "계획 편집 단계가 아닙니다.";
                return false;
            }
            if (slotIndex < 0 || slotIndex >= _slotCount)
            {
                error = $"1~{_slotCount}번 칸을 선택하세요.";
                return false;
            }
            return true;
        }

        public bool TryPlace(int slotIndex, IReadOnlyList<int> cardIds, int targetEnemyIndex, out string error)
        {
            if (!CheckEditable(slotIndex, out error)) return false;
            if (cardIds == null || cardIds.Count == 0) { error = "카드를 선택하세요."; return false; }

            var words = new List<MagicWordType>(cardIds.Count);
            var unique = new HashSet<int>();
            foreach (int id in cardIds)
            {
                if (!unique.Add(id) || IsCardReserved(id))
                {
                    error = "이미 다른 칸에 예약된 카드입니다.";
                    return false;
                }

                int handIdx = -1;
                for (int i = 0; i < Hand.Count; i++)
                {
                    if (Hand[i].Id == id) { handIdx = i; break; }
                }

                if (handIdx < 0)
                {
                    error = "현재 손패에 없는 카드입니다.";
                    return false;
                }

                words.Add(Hand[handIdx].Word);
            }

            if (!SpellResolver.TryBuildSpell(words, out SpellData spell, out error))
                return false;

            if (!CanPlaceSpan(slotIndex, spell.Cost, ignoreCastSlot: -1, out error))
                return false;

            if (!spell.IsSelfTarget && !IsEnemyAlive(targetEnemyIndex))
            {
                error = "살아 있는 적을 대상으로 선택하세요.";
                return false;
            }

            _orders[slotIndex] = new SlotOrder
            {
                CardIds = cardIds.ToArray(),
                Spell = spell,
                TargetEnemyIndex = spell.IsSelfTarget ? -1 : targetEnemyIndex
            };
            return true;
        }

        public bool TryRemove(int slotIndex, out string error)
        {
            if (!CheckEditable(slotIndex, out error)) return false;
            int castSlot = GetOrderCastSlot(slotIndex);
            if (castSlot < 0) { error = "제거할 주문이 없습니다."; return false; }
            _orders[castSlot] = null;
            return true;
        }

        public bool TryMove(int fromSlot, int toSlot, out string error)
        {
            if (!CheckEditable(fromSlot, out error) || !CheckEditable(toSlot, out error)) return false;
            int castFrom = GetOrderCastSlot(fromSlot);
            if (castFrom < 0)
            {
                error = "이동할 주문을 선택하세요.";
                return false;
            }
            if (castFrom == toSlot)
            {
                error = "같은 칸으로는 이동할 수 없습니다.";
                return false;
            }

            SlotOrder order = _orders[castFrom];
            if (!CanPlaceSpan(toSlot, order.Spell.Cost, ignoreCastSlot: castFrom, out error))
                return false;

            _orders[toSlot] = order;
            _orders[castFrom] = null;
            return true;
        }

        public bool TryRetarget(int slotIndex, int targetEnemyIndex, out string error)
        {
            if (!CheckEditable(slotIndex, out error)) return false;
            int castSlot = GetOrderCastSlot(slotIndex);
            if (castSlot < 0 || _orders[castSlot].Spell.IsSelfTarget || !IsEnemyAlive(targetEnemyIndex))
            {
                error = "공격 주문과 살아 있는 적을 선택하세요.";
                return false;
            }
            _orders[castSlot].TargetEnemyIndex = targetEnemyIndex;
            return true;
        }

        public bool TryConfirm(out string error)
        {
            if (!CheckEditable(0, out error)) return false;

            var reserved = new HashSet<int>();
            foreach (SlotOrder order in _orders.Where(o => o != null))
            {
                foreach (int id in order.CardIds)
                {
                    if (!reserved.Add(id) || !Hand.Any(c => c.Id == id))
                    {
                        error = "카드 예약을 다시 확인하세요.";
                        return false;
                    }
                }
            }

            int cost = PlannedCost;
            if (cost > Mana)
            {
                error = $"마나가 {cost - Mana} 부족합니다.";
                return false;
            }

            Mana -= cost;
            Phase = TimelinePhase.Execute;
            return true;
        }

        /// <summary>
        /// 현재 계획을 복제본에서 끝까지 시뮬레이션해 예지(격파 칸, 그로기 칸, 예상 피해, 영창 방해)를 계산합니다.
        /// 원본 타임라인의 상태와 마나는 전혀 바꾸지 않습니다.
        /// </summary>
        public PlanSimulationPreview SimulateCurrentPlan()
        {
            var sim = new CombatTimeline(this);
            if (sim.Phase == TimelinePhase.Plan)
                sim.Phase = TimelinePhase.Execute;

            var preview = new PlanSimulationPreview
            {
                SlotReports = new SlotStepReport[_slotCount]
            };

            while (sim.Phase == TimelinePhase.Execute && sim.CurrentSlot < _slotCount)
            {
                int slotIdx = sim.CurrentSlot;
                if (sim.ResolveNextSlot(out SlotStepReport rep))
                {
                    preview.SlotReports[slotIdx] = rep;
                    foreach (var er in rep.EnemyResults)
                    {
                        if (er.TriggeredBreak)
                            preview.Highlights.Add($"{slotIdx + 1}칸: 적{er.EnemyIndex + 1} 격파!");
                    }
                    if (rep.ChantInterrupted)
                        preview.Highlights.Add($"{slotIdx + 1}칸: 영창 방해({rep.InterruptedSpellName})!");
                }
            }

            preview.PredictedPlayerHp = sim.PlayerHp;
            preview.PredictedEnemyHp = (int[])sim._enemyHp.Clone();
            preview.PredictedEnemyGauge = (int[])sim._enemyWeakGauge.Clone();
            preview.PredictedEnemyGroggy = (int[])sim._enemyGroggy.Clone();
            preview.PredictedPhase = sim.Phase;
            return preview;
        }

        /// <summary>
        /// 현재 칸을 처리합니다:
        /// 1. 방패 활성화 → 2. 적 행동/공격과 차단 판정 → 3. 영창 중 피격 흩어짐 & 남은 피해 적용
        /// → 4. 플레이어 공격 피해(그로기 ×2 / 그로기 밖 ×0.5 / 방어 미카운터 추가 ×0.5)
        /// → 5. 카운터 판정 & 게이지 -1 → 0이면 다음 칸부터 그로기 → 6. 그로기 카운트 감소 & 종료 시 게이지 회복
        /// </summary>
        public bool ResolveNextSlot(out SlotStepReport report)
        {
            report = null;
            if (Phase != TimelinePhase.Execute) return false;

            int s = CurrentSlot;
            SlotOrder order = _orders[s];
            int target = order?.TargetEnemyIndex ?? -1;

            bool[] wasGroggyThisSlot = new bool[EnemyCount];
            bool[] blockedMelee = new bool[EnemyCount];
            bool[] interceptedMissile = new bool[EnemyCount];
            bool[] defendingThisSlot = new bool[EnemyCount];

            for (int e = 0; e < EnemyCount; e++)
                wasGroggyThisSlot[e] = IsEnemyAlive(e) && _enemyGroggy[e] > 0;

            // 1. 방패 활성화
            int guard = order != null ? order.Spell.GuardPower : 0;
            report = new SlotStepReport
            {
                SlotIndex = s,
                PlayerOrder = order,
                GuardActivated = guard
            };

            var events = new List<string>();
            if (guard > 0)
                events.Add($"방패({guard}) 활성화");
            else if (IsCastingSlot(s))
            {
                int castSlot = GetOrderCastSlot(s);
                events.Add($"영창 중 ({_orders[castSlot].Spell.Name} → {castSlot + 1}칸)");
            }

            // 2. 적 행동 및 공격/차단 판정
            int totalIncoming = 0;
            for (int e = 0; e < EnemyCount; e++)
            {
                EnemyIntentSlot intent = _intents[e, s];
                var enemyRes = new EnemySlotResult
                {
                    EnemyIndex = e,
                    Intent = intent,
                    WasGroggy = wasGroggyThisSlot[e]
                };

                if (!IsEnemyAlive(e))
                {
                    enemyRes.Outcome = IntentOutcome.Cancelled;
                    _outcomes[e, s] = IntentOutcome.Cancelled;
                    _outcomeNotes[e, s] = "처치됨";
                    events.Add($"적{e + 1} 취소");
                    report.EnemyResults.Add(enemyRes);
                    continue;
                }

                if (wasGroggyThisSlot[e])
                {
                    enemyRes.Outcome = IntentOutcome.Groggy;
                    _outcomes[e, s] = IntentOutcome.Groggy;
                    _outcomeNotes[e, s] = "그로기(취소)";
                    events.Add($"적{e + 1} 그로기 · 행동 취소");
                    report.EnemyResults.Add(enemyRes);
                    continue;
                }

                if (intent.kind == EnemyIntentKind.Approach)
                {
                    _enemyNear[e] = true;
                    enemyRes.MovedNear = true;
                    enemyRes.Outcome = IntentOutcome.Executed;
                    _outcomes[e, s] = IntentOutcome.Executed;
                    _outcomeNotes[e, s] = "접근";
                    events.Add($"적{e + 1} 접근");
                }
                else if (intent.kind == EnemyIntentKind.Retreat)
                {
                    _enemyNear[e] = false;
                    enemyRes.MovedFar = true;
                    enemyRes.Outcome = IntentOutcome.Executed;
                    _outcomes[e, s] = IntentOutcome.Executed;
                    _outcomeNotes[e, s] = "후퇴";
                    events.Add($"적{e + 1} 후퇴");
                }
                else if (intent.kind == EnemyIntentKind.Defend)
                {
                    defendingThisSlot[e] = true;
                    enemyRes.Outcome = IntentOutcome.Executed;
                    _outcomes[e, s] = IntentOutcome.Executed;
                    _outcomeNotes[e, s] = "방어";
                    events.Add($"적{e + 1} 방어");
                }
                else if (intent.power <= 0)
                {
                    enemyRes.Outcome = IntentOutcome.Executed;
                    _outcomes[e, s] = IntentOutcome.Executed;
                    _outcomeNotes[e, s] = intent.Label;
                    events.Add($"적{e + 1} {intent.Label}");
                }
                else if (intent.kind == EnemyIntentKind.Melee && !_enemyNear[e])
                {
                    enemyRes.Outcome = IntentOutcome.Missed;
                    _outcomes[e, s] = IntentOutcome.Missed;
                    _outcomeNotes[e, s] = "빗나감";
                    events.Add($"적{e + 1} 근접 빗나감");
                }
                else
                {
                    bool selfShield = order != null && order.Spell.IsSelfTarget;
                    bool thrownShieldAtThisEnemy = order != null &&
                                                   order.Spell.Body == MagicWordType.Shield &&
                                                   order.Spell.IsLaunched &&
                                                   target == e &&
                                                   intent.kind == EnemyIntentKind.Missile;

                    int blocked = 0;
                    if ((selfShield || thrownShieldAtThisEnemy) && guard > 0)
                    {
                        blocked = Math.Min(intent.power, guard);
                        guard -= blocked;

                        if (selfShield && intent.kind == EnemyIntentKind.Melee && blocked > 0)
                            blockedMelee[e] = true;
                        if (thrownShieldAtThisEnemy && blocked > 0)
                            interceptedMissile[e] = true;
                    }

                    int damage = intent.power - blocked;
                    totalIncoming += damage;
                    enemyRes.BlockedAmount = blocked;
                    enemyRes.DamageToPlayer = damage;
                    enemyRes.Outcome = blocked > 0 ? IntentOutcome.Blocked : IntentOutcome.Executed;

                    _outcomes[e, s] = enemyRes.Outcome;
                    _outcomeNotes[e, s] = blocked > 0
                        ? (damage > 0 ? $"막기{blocked}/피해{damage}" : "완전차단")
                        : $"피해{damage}";

                    events.Add($"적{e + 1} {intent.Label}(차단 {blocked}, 피해 {damage})");
                }

                report.EnemyResults.Add(enemyRes);
            }

            // 3. 플레이어 피격 적용 & 영창 중 피격 시 주문 흩어짐 (기획서 5절·7절 C)
            report.TotalDamageToPlayer = totalIncoming;
            PlayerHp = Math.Max(0, PlayerHp - totalIncoming);

            if (totalIncoming > 0 && IsCastingSlot(s))
            {
                int interruptedCastSlot = GetOrderCastSlot(s);
                if (interruptedCastSlot > s && _orders[interruptedCastSlot] != null)
                {
                    report.ChantInterrupted = true;
                    report.InterruptedSpellName = _orders[interruptedCastSlot].Spell.Name;
                    _orders[interruptedCastSlot] = null;
                    events.Add($"영창 중 피격! [{report.InterruptedSpellName}] 흩어짐");
                }
            }

            // 4. 플레이어 공격 피해 적용 (그로기 ×2 / 그로기 밖 ×0.5 / 방어 미카운터 추가 ×0.5)
            if (order != null && !order.Spell.IsSelfTarget && IsEnemyAlive(target))
            {
                int baseDmg = order.Spell.ComputeBaseDamage(_enemyNear[target], _playerLevelBonus);
                if (baseDmg > 0)
                {
                    int finalDmg;
                    if (wasGroggyThisSlot[target])
                    {
                        finalDmg = baseDmg * 2;
                        report.WasGroggyHit = true;
                    }
                    else
                    {
                        float mult = 0.5f;
                        if (defendingThisSlot[target] && !order.Spell.HasTrait(SpellTrait.Heavy))
                            mult *= 0.5f;
                        finalDmg = Math.Max(1, (int)Math.Floor(baseDmg * mult));
                    }

                    report.PlayerDamageDealt = finalDmg;
                    _enemyHp[target] = Math.Max(0, _enemyHp[target] - finalDmg);
                    if (_enemyHp[target] == 0)
                    {
                        report.TargetKilled = true;
                        for (int later = s + 1; later < _slotCount; later++)
                        {
                            _outcomes[target, later] = IntentOutcome.Cancelled;
                            _outcomeNotes[target, later] = "처치됨";
                        }
                    }

                    string tag = report.WasGroggyHit ? " [그로기 ×2!]" : " (×0.5)";
                    events.Add($"{order.Spell.Name} → 적{target + 1} 피해 {finalDmg}{tag}" + (report.TargetKilled ? " [처치!]" : ""));
                }
                else
                {
                    report.PlayerSpellMissed = true;
                    events.Add($"{order.Spell.Name} → 적{target + 1} 빗나감");
                }
            }

            // 5. 카운터 판정 (주문 발동 칸만, 크기 무관 1칸 감소, 게이지 0이면 다음 칸부터 그로기)
            if (order != null)
            {
                for (int e = 0; e < EnemyCount; e++)
                {
                    if (!IsEnemyAlive(e) || wasGroggyThisSlot[e]) continue;
                    SpellTrait needed = _intents[e, s].EffectiveCounterTrait;
                    if (needed == SpellTrait.None) continue;

                    bool counterSuccess = false;
                    switch (needed)
                    {
                        case SpellTrait.Block:
                            counterSuccess = order.Spell.HasTrait(SpellTrait.Block) && blockedMelee[e];
                            break;
                        case SpellTrait.Intercept:
                            counterSuccess = order.Spell.HasTrait(SpellTrait.Intercept) &&
                                             target == e &&
                                             (interceptedMissile[e] || report.PlayerDamageDealt > 0);
                            break;
                        default:
                            counterSuccess = order.Spell.HasTrait(needed) &&
                                             target == e &&
                                             report.PlayerDamageDealt > 0;
                            break;
                    }

                    if (!counterSuccess) continue;

                    _enemyCounteredThisTurn[e] = true;
                    _enemyWeakGauge[e] = Math.Max(0, _enemyWeakGauge[e] - 1);

                    EnemySlotResult enemyRes = report.EnemyResults[e];
                    enemyRes.Countered = true;

                    if (_enemyWeakGauge[e] == 0)
                    {
                        _enemyGroggy[e] = _enemyGroggyDuration[e];
                        enemyRes.TriggeredBreak = true;
                        enemyRes.Outcome = IntentOutcome.Break;
                        _outcomes[e, s] = IntentOutcome.Break;
                        _outcomeNotes[e, s] = "격파! (그로기)";
                        events.Add($"적{e + 1} 카운터 격파! 다음 {_enemyGroggyDuration[e]}칸 그로기");
                    }
                    else
                    {
                        enemyRes.Outcome = IntentOutcome.Countered;
                        _outcomes[e, s] = IntentOutcome.Countered;
                        _outcomeNotes[e, s] = $"카운터! (게이지 {_enemyWeakGauge[e]})";
                        events.Add($"적{e + 1} 카운터! 게이지 {_enemyWeakGauge[e]}/{_enemyMaxWeakGauge[e]}");
                    }
                }
            }

            // 6. 이번 칸에 그로기였던 적의 그로기 감소 & 종료 시 게이지 최대치 회복
            for (int e = 0; e < EnemyCount; e++)
            {
                EnemySlotResult enemyRes = report.EnemyResults[e];
                if (IsEnemyAlive(e) && wasGroggyThisSlot[e])
                {
                    _enemyGroggy[e] = Math.Max(0, _enemyGroggy[e] - 1);
                    if (_enemyGroggy[e] == 0)
                    {
                        _enemyWeakGauge[e] = _enemyMaxWeakGauge[e];
                        enemyRes.GroggyRecovered = true;
                        events.Add($"적{e + 1} 그로기 종료 · 게이지 회복({_enemyWeakGauge[e]})");
                    }
                }

                enemyRes.GaugeAfter = _enemyWeakGauge[e];
                enemyRes.GroggyAfter = _enemyGroggy[e];
            }

            report.SummaryLine = $"[{s + 1}칸] " + string.Join(" | ", events);
            _log.Add(report.SummaryLine);

            CurrentSlot++;
            if (PlayerHp == 0)
                Phase = TimelinePhase.Defeat;
            else if (_enemyHp.All(h => h == 0))
                Phase = TimelinePhase.Victory;
            else if (CurrentSlot >= _slotCount)
            {
                // 턴 종료 게이지 회복 다이얼 (보스 등)
                for (int e = 0; e < EnemyCount; e++)
                {
                    if (IsEnemyAlive(e) && _enemyRecoverOnNoCounter[e] && !_enemyCounteredThisTurn[e] &&
                        _enemyGroggy[e] == 0 && _enemyWeakGauge[e] < _enemyMaxWeakGauge[e])
                    {
                        _enemyWeakGauge[e] += 1;
                        _log.Add($"[턴 종료] 적{e + 1} 카운터 미발생 → 게이지 1 회복 ({_enemyWeakGauge[e]}/{_enemyMaxWeakGauge[e]})");
                    }
                }
                Phase = TimelinePhase.TurnEnd;
            }

            if (Phase != TimelinePhase.Execute)
            {
                for (int later = CurrentSlot; later < _slotCount; later++)
                {
                    for (int e = 0; e < EnemyCount; e++)
                    {
                        _outcomes[e, later] = IntentOutcome.Cancelled;
                        _outcomeNotes[e, later] = "종료";
                    }
                }
                _discardFunc?.Invoke();
                Hand = Array.Empty<CombatCard>();
            }

            return true;
        }
    }
}
