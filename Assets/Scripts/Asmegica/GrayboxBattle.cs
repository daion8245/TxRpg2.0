using System;
using System.Collections.Generic;
using System.Linq;

namespace Asmegica
{
    public readonly struct Card
    {
        public int Id { get; }
        public MagicWord Word { get; }
        public Card(int id, MagicWord word) { Id = id; Word = word; }
    }

    public enum RoundPhase { Plan, Execute, TurnEnd, Victory, Defeat }
    public enum IntentKind { Wait, Approach, Prepare, Melee, Missile, Retreat }

    public readonly struct Intent
    {
        public IntentKind Kind { get; }
        public int Power { get; }
        public Intent(IntentKind kind, int power = 0) { Kind = kind; Power = power; }
        public string Label
        {
            get
            {
                switch (Kind)
                {
                    case IntentKind.Approach: return "접근";
                    case IntentKind.Prepare: return "공격 준비";
                    case IntentKind.Melee: return "근접 " + Power;
                    case IntentKind.Missile: return "투사체 " + Power;
                    case IntentKind.Retreat: return "후퇴";
                    default: return "대기";
                }
            }
        }
    }

    public sealed class GrayboxBattle
    {
        private sealed class Order
        {
            public int[] Cards;
            public Spell Spell;
            public int Target;
        }

        private static readonly Intent[,] Patterns = {
            { new Intent(IntentKind.Wait), new Intent(IntentKind.Approach), new Intent(IntentKind.Prepare), new Intent(IntentKind.Missile, 3), new Intent(IntentKind.Retreat) },
            { new Intent(IntentKind.Approach), new Intent(IntentKind.Prepare), new Intent(IntentKind.Melee, 2), new Intent(IntentKind.Wait), new Intent(IntentKind.Retreat) },
            { new Intent(IntentKind.Prepare), new Intent(IntentKind.Missile, 1), new Intent(IntentKind.Prepare), new Intent(IntentKind.Missile, 2), new Intent(IntentKind.Wait) }
        };
        private readonly Order[] orders = new Order[5];
        private readonly int[] hp;
        private readonly bool[] near;
        private readonly List<string> log = new List<string>();
        private readonly Func<IReadOnlyList<Card>> draw;
        private readonly Action<IReadOnlyList<Card>> discard;
        private int slot;
        public int PlayerHp { get; private set; }
        public int Mana { get; private set; }
        public int Turn { get; private set; }
        public int EnemyCount => hp.Length;
        public int CurrentSlot => slot;
        public RoundPhase Phase { get; private set; }
        public string PhaseName => Phase.ToString();
        public IReadOnlyList<Card> Hand { get; private set; }
        public IReadOnlyList<string> Log { get; }
        public int PlannedCost => orders.Where(o => o != null).Sum(o => o.Spell.Cost);

        public GrayboxBattle(int enemyCount, int enemyHp = 6, Func<IReadOnlyList<Card>> draw = null,
            Action<IReadOnlyList<Card>> discard = null, int playerHp = 6)
        {
            if (enemyCount < 1 || enemyCount > 3 || enemyHp < 1 || playerHp < 1) throw new ArgumentOutOfRangeException();
            hp = Enumerable.Repeat(enemyHp, enemyCount).ToArray();
            near = new bool[enemyCount];
            PlayerHp = playerHp;
            this.draw = draw ?? (() => new[] { new Card(0, MagicWord.Fire), new Card(1, MagicWord.Launch), new Card(2, MagicWord.Shield) });
            this.discard = discard;
            Log = log.AsReadOnly();
            BeginTurn();
        }

        private void BeginTurn()
        {
            Hand = Array.AsReadOnly(draw().ToArray());
            if (Hand.Select(c => c.Id).Distinct().Count() != Hand.Count) throw new ArgumentException("손패의 카드 ID가 중복됩니다.");
            Array.Clear(orders, 0, orders.Length);
            log.Clear();
            slot = 0;
            Turn++;
            Mana = 3;
            Phase = RoundPhase.Plan;
        }

        public void NextTurn() { if (Phase == RoundPhase.TurnEnd) BeginTurn(); }
        public int GetEnemyHp(int enemy) => hp[enemy];
        public bool IsNear(int enemy) => near[enemy];
        public string IntentLabel(int enemy, int index) => Patterns[enemy, index].Label;
        public bool IsReserved(int id) => orders.Any(o => o != null && Array.IndexOf(o.Cards, id) >= 0);
        public bool Occupied(int index) => orders[index] != null;
        public string PlanLabel(int index)
        {
            var order = orders[index];
            if (order == null) return "빈 칸";
            return order.Spell.Name + (IsSelf(order.Spell) ? " · 자신" : " → 적 " + (order.Target + 1));
        }
        private static bool IsSelf(Spell spell) => spell.Body == MagicWord.Shield && !spell.IsLaunched;
        private bool Living(int id) => id >= 0 && id < hp.Length && hp[id] > 0;
        private static bool Fail(string reason, out string error) { error = reason; return false; }
        private bool Editable(int index, out string error)
        {
            error = "";
            if (Phase != RoundPhase.Plan) return Fail("계획 편집 시간이 지났습니다.", out error);
            return (index >= 0 && index < 5) || Fail("1~5번 칸을 선택하세요.", out error);
        }

        public bool TryPlace(int index, IReadOnlyList<int> ids, int target, out string error)
        {
            if (!Editable(index, out error)) return false;
            if (Occupied(index)) return Fail("비어 있는 칸을 선택하세요.", out error);
            if (ids == null) return Fail("카드를 선택하세요.", out error);
            var words = new List<MagicWord>();
            var unique = new HashSet<int>();
            foreach (int id in ids)
            {
                if (!unique.Add(id) || IsReserved(id)) return Fail("이미 예약한 카드입니다.", out error);
                int handIndex = -1;
                for (int i = 0; i < Hand.Count; i++) if (Hand[i].Id == id) { handIndex = i; break; }
                if (handIndex < 0) return Fail("손에 없는 카드입니다.", out error);
                words.Add(Hand[handIndex].Word);
            }
            if (!SpellResolver.TryBuildSpell(words, out var spell, out error)) return false;
            if (!IsSelf(spell) && !Living(target)) return Fail("살아 있는 적을 선택하세요.", out error);
            orders[index] = new Order { Cards = ids.ToArray(), Spell = spell, Target = IsSelf(spell) ? -1 : target };
            return true;
        }

        public bool TryRemove(int index, out string error)
        {
            if (!Editable(index, out error)) return false;
            if (!Occupied(index)) return Fail("풀 주문이 없습니다.", out error);
            orders[index] = null;
            return true;
        }
        public bool TryMove(int from, int to, out string error)
        {
            if (!Editable(from, out error) || !Editable(to, out error)) return false;
            if (!Occupied(from) || Occupied(to)) return Fail("주문과 빈 목적지 칸을 선택하세요.", out error);
            orders[to] = orders[from];
            orders[from] = null;
            return true;
        }
        public bool TryRetarget(int index, int enemy, out string error)
        {
            if (!Editable(index, out error)) return false;
            if (!Occupied(index) || IsSelf(orders[index].Spell) || !Living(enemy)) return Fail("공격 주문과 살아 있는 적을 선택하세요.", out error);
            orders[index].Target = enemy;
            return true;
        }
        public bool TryConfirm(out string error)
        {
            if (!Editable(0, out error)) return false;
            var reserved = new HashSet<int>();
            foreach (var order in orders.Where(o => o != null))
                foreach (int id in order.Cards)
                    if (!reserved.Add(id) || !Hand.Any(c => c.Id == id)) return Fail("카드 예약을 다시 확인하세요.", out error);
            int cost = PlannedCost;
            if (cost > Mana) return Fail("마나가 " + (cost - Mana) + " 부족합니다.", out error);
            Mana -= cost;
            Phase = RoundPhase.Execute;
            return true;
        }

        public void Step()
        {
            if (Phase != RoundPhase.Execute) return;
            var events = new List<string>();
            var order = orders[slot];
            int target = order?.Target ?? -1;
            int hit = 0;
            int guard = order?.Spell.Body == MagicWord.Shield ? 2 + (order.Spell.IsStrong ? 1 : 0) : 0;
            int incoming = 0;
            for (int e = 0; e < EnemyCount; e++)
            {
                var intent = Patterns[e, slot];
                string actor = "적 " + (e + 1) + " ";
                if (!Living(e)) { events.Add(actor + intent.Label + " 취소"); continue; }
                if (intent.Kind == IntentKind.Approach) near[e] = true;
                if (intent.Kind == IntentKind.Retreat) near[e] = false;
                if (intent.Power == 0) { events.Add(actor + intent.Label); continue; }
                if (intent.Kind == IntentKind.Melee && !near[e]) { events.Add(actor + "근접 빗나감"); continue; }
                bool eligible = order != null && (IsSelf(order.Spell) || (target == e && intent.Kind == IntentKind.Missile));
                int damage = eligible ? SlotImpact.Absorb(intent.Power, ref guard) : intent.Power;
                incoming += damage;
                events.Add(actor + intent.Label + " · 차단 " + (intent.Power - damage) + " · 받은 피해 " + damage);
            }
            if (order != null && !IsSelf(order.Spell))
            {
                if (Living(target)) hit = SlotImpact.SpellDamage(order.Spell, near[target]);
                events.Add(order.Spell.Name + " → 적 " + (target + 1) + (hit > 0 ? " 피해 " + hit : " 빗나감"));
            }
            if (hit > 0) hp[target] = Math.Max(0, hp[target] - hit);
            PlayerHp = Math.Max(0, PlayerHp - incoming);
            log.Add((slot + 1) + "칸   " + string.Join("  /  ", events));
            slot++;
            if (PlayerHp == 0) Phase = RoundPhase.Defeat;
            else if (hp.All(value => value == 0)) Phase = RoundPhase.Victory;
            else if (slot == 5) Phase = RoundPhase.TurnEnd;
            if (Phase == RoundPhase.Execute) return;
            for (int later = slot; later < 5; later++)
                log.Add((later + 1) + "칸   " + string.Join("  /  ", Enumerable.Range(0, EnemyCount)
                    .Select(e => "적 " + (e + 1) + " " + IntentLabel(e, later) + " 취소 · 전투 종료")));
            discard?.Invoke(Hand);
            Hand = Array.Empty<Card>();
        }
    }
}
