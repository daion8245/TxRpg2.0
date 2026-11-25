using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Asmegica
{
    public sealed class GrayboxBattleView
    {
        private const float LogicalWidth = 960f;
        private const int CardsPerRow = 6, LogLines = 8;

        private readonly GrayboxBattle _battle;
        private readonly List<int> _draft = new List<int>(); // 사용 순서대로 정렬된 실제 Card.Id
        private int _slot, _target, _moveFrom = -1;
        private string _msg = "";

        public bool WantsExit { get; private set; }
        public System.Func<string> PileInfo { get; set; } // 편성 덱 전투에서만 더미 상태를 표시한다.

        public GrayboxBattleView(GrayboxBattle battle) => _battle = battle;

        public void Draw()
        {
            string phase = _battle.PhaseName;
            bool planning = _battle.Phase == RoundPhase.Plan;
            int before = _draft.Count; // 손패에서 사라진 카드는 초안에서 제외
            _draft.RemoveAll(id => !_battle.Hand.Any(c => c.Id == id));
            if (_draft.Count != before) _msg = $"손패에서 사라진 카드 {before - _draft.Count}장을 초안에서 뺐습니다.";

            GUILayout.BeginVertical(GUILayout.Width(LogicalWidth));
            GUILayout.Label($"전투 계획 · 턴 {_battle.Turn} · 단계: {PhaseKo(phase)}");
            GUILayout.Label($"플레이어 HP {_battle.PlayerHp} · 마나 {_battle.Mana} · 적 {_battle.EnemyCount}명");
            if (PileInfo != null) GUILayout.Label(PileInfo());

            int clicked = ForecastGrid.Draw(_battle, _slot);
            if (planning && clicked != _slot) ClickSlot(clicked);
            if (planning && _moveFrom >= 0) GUILayout.Label("이동할 빈 칸을 선택하세요.");
            GUI.enabled = planning;
            GUILayout.Label("손패 (클릭: 초안 추가/제거 · [예약] 카드는 선택 불가)");
            int col = 0;
            GUILayout.BeginHorizontal();
            foreach (Card card in _battle.Hand)
            {
                if (col++ == CardsPerRow) { GUILayout.EndHorizontal(); GUILayout.BeginHorizontal(); col = 1; }
                bool reserved = _battle.IsReserved(card.Id);
                string tag = reserved ? "[예약]" : _draft.Contains(card.Id) ? "[초안]" : "";
                GUI.enabled = planning && !reserved;
                if (GUILayout.Button($"{SpellResolver.WordName(card.Word)} #{card.Id}\n{tag}", GUILayout.Width(148), GUILayout.Height(44)))
                    ToggleDraft(card.Id);
                GUI.enabled = planning;
            }
            GUILayout.EndHorizontal();

            GUILayout.Label("초안 (사용 순서)");
            for (int i = 0; i < _draft.Count; i++)
                GUILayout.Label($"{i + 1}. {SpellResolver.WordName(CardOf(_draft[i]).Word)} (카드 #{_draft[i]})");
            GUILayout.Label(Preview());
            if (GUILayout.Button("초안 비우기")) { _draft.Clear(); _msg = "초안을 비웠습니다."; }

            GUILayout.Label("대상 적 선택");
            GUILayout.BeginHorizontal();
            for (int e = 0; e < _battle.EnemyCount; e++)
                {
                    GUI.enabled = planning && _battle.GetEnemyHp(e) > 0;
                    string range = _battle.IsNear(e) ? "근거리" : "원거리";
                    if (GUILayout.Button((_target == e ? "● " : "") + $"적 {e + 1} · {range}", GUILayout.Width(180))) _target = e;
                }
            GUI.enabled = planning;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("배치")) Place();
            if (GUILayout.Button(_moveFrom < 0 ? "이동" : "이동 취소")) ToggleMove();
            if (GUILayout.Button("제거")) Remove();
            if (GUILayout.Button("대상 변경")) Retarget();
            if (GUILayout.Button($"계획 확정 · {_battle.PlannedCost}/{_battle.Mana} 마나")) Confirm();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUI.enabled = _battle.Phase == RoundPhase.Execute;
            if (GUILayout.Button(_battle.Phase == RoundPhase.Execute ? $"{_battle.CurrentSlot + 1}칸 실행" : "칸 실행")) { _battle.Step(); _msg = "한 단계 진행했습니다."; }
            GUI.enabled = _battle.Phase == RoundPhase.TurnEnd;
            if (GUILayout.Button("다음 턴")) { _battle.NextTurn(); _moveFrom = -1;_target = Enumerable.Range(0, _battle.EnemyCount).First(e => _battle.GetEnemyHp(e) > 0); _msg = "새 손패를 뽑았습니다."; }
            GUI.enabled = true;
            if (GUILayout.Button("나가기")) WantsExit = true;
            GUILayout.EndHorizontal();

            GUILayout.Label(_msg);
            GUILayout.Label("최근 결과");
            var log = _battle.Log;
            for (int i = log.Count - 1; i >= 0 && log.Count - i <= LogLines; i--) GUILayout.Label(log[i]);
            GUILayout.EndVertical();
        }

        private void ClickSlot(int s)
        {
            if (_moveFrom < 0) { _slot = s; return; }
            bool ok = _battle.TryMove(_moveFrom, s, out string err);
            _msg = ok ? $"슬롯 {_moveFrom + 1} -> {s + 1} 이동했습니다." : err;
            if (ok) _slot = s;
            _moveFrom = -1;
        }

        private void ToggleDraft(int cardId) { if (!_draft.Remove(cardId)) _draft.Add(cardId); }

        private void ToggleMove()
        {
            if (_moveFrom < 0 && !_battle.Occupied(_slot)) { _msg = "비어 있는 칸은 이동할 수 없습니다."; return; }
            _moveFrom = _moveFrom < 0 ? _slot : -1;
            _msg = _moveFrom < 0 ? "이동을 취소했습니다." : $"슬롯 {_slot + 1}을 옮깁니다. 목적지 슬롯을 클릭하세요.";
        }

        private void Place()
        {
            bool ok = _battle.TryPlace(_slot, _draft.ToArray(), _target, out string err);
            if (ok) { _draft.Clear(); _moveFrom = -1; }
            _msg = ok ? $"슬롯 {_slot + 1}에 {_battle.PlanLabel(_slot)} 배치" : err;
        }

        private void Remove() { bool ok = _battle.TryRemove(_slot, out string err); if (ok) _moveFrom = -1; _msg = ok ? $"슬롯 {_slot + 1} 계획을 제거했습니다." : err; }
        private void Retarget() { bool ok = _battle.TryRetarget(_slot, _target, out string err); _msg = ok ? $"대상 변경: 슬롯 {_slot + 1} -> 적 {_target + 1}" : err; }

        private void Confirm()
        {
            if (_draft.Count > 0) { _msg = "배치되지 않은 초안이 있어 확정하지 않았습니다. 배치하거나 '초안 비우기'를 누르세요."; return; }
            bool ok = _battle.TryConfirm(out string err); if (ok) _moveFrom = -1;
            _msg = ok ? "계획을 확정했습니다." : err;
        }

        private string Preview()
        {
            if (_draft.Count == 0) return "미리보기: 초안 없음";
            var words = _draft.Select(id => CardOf(id).Word).ToList();
            return SpellResolver.TryBuildSpell(words, out Spell spell, out string err)
                ? $"미리보기: {spell.Name} · 비용 {spell.Cost} (보유 마나 {_battle.Mana})"
                : $"미리보기 불가: {err}";
        }

        private Card CardOf(int id) => _battle.Hand.First(c => c.Id == id);

        private static string PhaseKo(string p) => p switch { "Plan" => "계획", "Execute" => "실행", "TurnEnd" => "턴 종료", "Victory" => "승리", "Defeat" => "패배", _ => p };
    }
}
