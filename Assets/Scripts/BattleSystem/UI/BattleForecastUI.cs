using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using TMPro;
using TxRpg.Battle;
using TxRpg.Core;
using TxRpg.Data;
using TxRpg.Magic;
using UnityEngine;
using UnityEngine.UI;

namespace TxRpg.UI
{
    [Serializable]
    public class ForecastSlotView
    {
        public Button slotButton;
        public Image backgroundImage;
        public TMP_Text headerText;
        public TMP_Text[] enemyIntentTexts; // length 3
        public TMP_Text playerPlanText;
    }

    [Serializable]
    public class DeckWordRowView
    {
        public MagicWordType word;
        public TMP_Text labelText;
        public TMP_Text countText;
        public Button minusButton;
        public Button plusButton;
    }

    /// <summary>
    /// 전투 개선 기획서(약점 게이지 · 카운터 아이콘 · 그로기 · 실시간 FutureVision 시뮬레이션 미리보기)를
    /// 씬의 UGUI + TMP 요소에 바인딩하는 컨트롤러.
    /// </summary>
    public class BattleForecastUI : MonoBehaviour
    {
        [Header("상단 HUD & 인카운터 프리셋")]
        [SerializeField] private TMP_Text statusHeaderText;
        [SerializeField] private TMP_Text pileInfoText;
        [SerializeField] private Button presetKnightButton;     // Stage 0: 돌격 기사 (7절 예시)
        [SerializeField] private Button presetShielderButton;   // Stage 1: 방패병
        [SerializeField] private Button presetArcherButton;     // Stage 2: 궁수
        [SerializeField] private Button preset1v2Button;        // Stage 3: 1vs2
        [SerializeField] private Button preset1v3Button;        // Stage 4: 1vs3
        [SerializeField] private Button presetBossButton;       // Stage 5: 보스 (게이지 5)
        [SerializeField] private Button openDeckEditorButton;
        [SerializeField] private Button goToPuzzleButton;

        [Header("5칸 예지 그리드")]
        [SerializeField] private ForecastSlotView[] slotViews; // 5개

        [Header("손패 (UGUI 버튼 템플릿)")]
        [SerializeField] private Transform handCardRoot;
        [SerializeField] private Button cardButtonTemplate;

        [Header("주문 초안 & 대상 & 조작")]
        [SerializeField] private TMP_Text draftSequenceText;
        [SerializeField] private TMP_Text spellPreviewText;
        [SerializeField] private TMP_Text feedbackText;
        [SerializeField] private TMP_Text battleLogText;
        [SerializeField] private Button[] targetEnemyButtons; // 3개
        [SerializeField] private TMP_Text[] targetEnemyLabels; // 3개
        [SerializeField] private Button placeButton;
        [SerializeField] private Button moveButton;
        [SerializeField] private TMP_Text moveButtonLabel;
        [SerializeField] private Button removeButton;
        [SerializeField] private Button retargetButton;
        [SerializeField] private Button clearDraftButton;
        [SerializeField] private Button confirmButton;
        [SerializeField] private TMP_Text confirmButtonLabel;
        [SerializeField] private Button stepExecuteButton;
        [SerializeField] private TMP_Text stepExecuteLabel;
        [SerializeField] private Button autoExecuteButton;
        [SerializeField] private Button nextTurnButton;

        [Header("덱 편성 모달 패널")]
        [SerializeField] private GameObject deckModalPanel;
        [SerializeField] private DeckWordRowView[] deckWordRows; // 4개
        [SerializeField] private Button[] deckEnemyCountButtons; // 1, 2, 3마리
        [SerializeField] private Button[] deckPlayerHpButtons; // 10, 14, 20
        [SerializeField] private Button[] deckLevelButtons; // Lv1(적정), Lv3(고레벨)
        [SerializeField] private TMP_InputField seedInputField;
        [SerializeField] private Button randomSeedButton;
        [SerializeField] private Button unlockAllWordsButton;
        [SerializeField] private TMP_Text deckValidationText;
        [SerializeField] private Button startDeckBattleButton;
        [SerializeField] private Button closeDeckModalButton;

        [Header("전투 결과 배너")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TMP_Text resultTitleText;
        [SerializeField] private Button retryBattleButton;
        [SerializeField] private Button resultToPuzzleButton;

        private BattleManager _manager;
        private CombatTimeline _timeline;

        private readonly List<int> _draftCardIds = new();
        private readonly List<Button> _spawnedCardButtons = new();
        private int _selectedSlot = 1;
        private int _selectedTarget;
        private int _moveFromSlot = -1;
        private string _feedback = "아이콘([화염]/[투사체]/[막기]/[요격]/[강타])에 맞는 주문으로 카운터해 약점을 격파하세요!";

        // 덱 편성 상태
        private readonly int[] _deckCounts = new int[4];
        private int _deckEnemyCount = 1;
        private int _deckPlayerHp = 12;
        private int _deckLevelBonus = 0;
        private static readonly int[] PlayerHpOptions = { 10, 14, 20 };
        private static readonly int[] LevelBonusOptions = { 0, 4 };

        private static readonly Color ColSlotNormal = new(0.14f, 0.16f, 0.22f, 0.92f);
        private static readonly Color ColSlotSelected = new(0.24f, 0.34f, 0.52f, 0.96f);
        private static readonly Color ColSlotCasting = new(0.23f, 0.20f, 0.13f, 0.94f);
        private static readonly Color ColSlotGroggy = new(0.14f, 0.26f, 0.30f, 0.96f);
        private static readonly Color ColSlotExecuting = new(0.48f, 0.36f, 0.14f, 0.98f);
        private static readonly Color ColCardNormal = new(0.20f, 0.23f, 0.30f, 1f);
        private static readonly Color ColCardDraft = new(0.25f, 0.52f, 0.36f, 1f);
        private static readonly Color ColCardReserved = new(0.15f, 0.15f, 0.17f, 0.65f);
        private static readonly Color ColTargetNormal = new(0.20f, 0.22f, 0.28f, 1f);
        private static readonly Color ColTargetSelected = new(0.65f, 0.26f, 0.22f, 1f);

        private bool _listenersBound;

        private void Awake()
        {
            if (cardButtonTemplate != null) cardButtonTemplate.gameObject.SetActive(false);
            if (deckModalPanel != null) deckModalPanel.SetActive(false);
            if (resultPanel != null) resultPanel.SetActive(false);
            BindStaticListeners();
        }

        private void BindStaticListeners()
        {
            if (_listenersBound) return;
            _listenersBound = true;

            presetKnightButton?.onClick.AddListener(() => _manager?.StartStagePreset(0));
            presetShielderButton?.onClick.AddListener(() => _manager?.StartStagePreset(1));
            presetArcherButton?.onClick.AddListener(() => _manager?.StartStagePreset(2));
            preset1v2Button?.onClick.AddListener(() => _manager?.StartStagePreset(3));
            preset1v3Button?.onClick.AddListener(() => _manager?.StartStagePreset(4));
            presetBossButton?.onClick.AddListener(() => _manager?.StartStagePreset(5));
            openDeckEditorButton?.onClick.AddListener(OpenDeckEditor);
            goToPuzzleButton?.onClick.AddListener(() => _manager?.LoadPuzzleScene());

            if (slotViews != null)
            {
                for (int i = 0; i < slotViews.Length; i++)
                {
                    int slotIdx = i;
                    slotViews[i]?.slotButton?.onClick.AddListener(() => OnClickSlot(slotIdx));
                }
            }

            if (targetEnemyButtons != null)
            {
                for (int i = 0; i < targetEnemyButtons.Length; i++)
                {
                    int enemyIdx = i;
                    targetEnemyButtons[i]?.onClick.AddListener(() => SelectTargetEnemy(enemyIdx));
                }
            }

            placeButton?.onClick.AddListener(OnClickPlace);
            moveButton?.onClick.AddListener(OnClickToggleMove);
            removeButton?.onClick.AddListener(OnClickRemove);
            retargetButton?.onClick.AddListener(OnClickRetarget);
            clearDraftButton?.onClick.AddListener(() =>
            {
                _draftCardIds.Clear();
                _feedback = "초안을 비웠습니다.";
                RefreshAll();
            });
            confirmButton?.onClick.AddListener(OnClickConfirm);
            stepExecuteButton?.onClick.AddListener(() => _manager?.StepExecuteAsync().Forget());
            autoExecuteButton?.onClick.AddListener(() => _manager?.AutoExecuteAllAsync().Forget());
            nextTurnButton?.onClick.AddListener(() => _manager?.NextTurn());

            if (deckWordRows != null)
            {
                for (int i = 0; i < deckWordRows.Length; i++)
                {
                    int idx = i;
                    deckWordRows[i]?.minusButton?.onClick.AddListener(() => AdjustDeckCount(idx, -1));
                    deckWordRows[i]?.plusButton?.onClick.AddListener(() => AdjustDeckCount(idx, 1));
                }
            }

            if (deckEnemyCountButtons != null)
            {
                for (int i = 0; i < deckEnemyCountButtons.Length; i++)
                {
                    int count = i + 1;
                    deckEnemyCountButtons[i]?.onClick.AddListener(() => { _deckEnemyCount = count; RefreshDeckModal(); });
                }
            }

            if (deckPlayerHpButtons != null)
            {
                for (int i = 0; i < deckPlayerHpButtons.Length && i < PlayerHpOptions.Length; i++)
                {
                    int hp = PlayerHpOptions[i];
                    deckPlayerHpButtons[i]?.onClick.AddListener(() => { _deckPlayerHp = hp; RefreshDeckModal(); });
                }
            }

            if (deckLevelButtons != null)
            {
                for (int i = 0; i < deckLevelButtons.Length && i < LevelBonusOptions.Length; i++)
                {
                    int bonus = LevelBonusOptions[i];
                    deckLevelButtons[i]?.onClick.AddListener(() => { _deckLevelBonus = bonus; RefreshDeckModal(); });
                }
            }

            randomSeedButton?.onClick.AddListener(() =>
            {
                if (seedInputField != null)
                    seedInputField.text = UnityEngine.Random.Range(1, 999999).ToString();
            });
            unlockAllWordsButton?.onClick.AddListener(() =>
            {
                GameSessionState.LearnAllWords();
                for (int i = 0; i < _deckCounts.Length; i++)
                    if (_deckCounts[i] == 0) _deckCounts[i] = 1;
                RefreshDeckModal();
            });
            startDeckBattleButton?.onClick.AddListener(OnClickStartDeckBattle);
            closeDeckModalButton?.onClick.AddListener(() =>
            {
                if (deckModalPanel != null) deckModalPanel.SetActive(false);
            });

            retryBattleButton?.onClick.AddListener(() =>
            {
                if (_manager == null) return;
                if (_manager.IsDeckBattle) OpenDeckEditor();
                else _manager.StartStagePreset(_manager.CurrentStageIndex, _manager.CurrentLevelBonus);
            });
            resultToPuzzleButton?.onClick.AddListener(() => _manager?.LoadPuzzleScene());
        }

        public void BindTimeline(BattleManager manager, CombatTimeline timeline)
        {
            _manager = manager;
            _timeline = timeline;
            _draftCardIds.Clear();
            _selectedSlot = 1;
            _selectedTarget = timeline.FirstAliveEnemyIndex();
            _moveFromSlot = -1;
            _feedback = _manager.IsDeckBattle
                ? $"편성 덱 전투 시작! 카운터로 약점 게이지를 격파하고 그로기에 큰 주문을 맞추세요."
                : $"{_manager.StageData?.stageName ?? "전투"} 시작! 카운터 조건([화염]/[투사체]/[막기]/[요격]/[강타])을 보고 그로기를 만드세요.";

            if (deckModalPanel != null) deckModalPanel.SetActive(false);
            if (resultPanel != null) resultPanel.SetActive(false);

            RebuildHandCards();
            RefreshAll();
        }

        public void OnNewTurnStarted()
        {
            _draftCardIds.Clear();
            _moveFromSlot = -1;
            _selectedTarget = _timeline.FirstAliveEnemyIndex();
            _feedback = $"턴 {_timeline.Turn} 시작! 약점 게이지와 그로기 상태가 이어집니다.";
            RebuildHandCards();
            RefreshAll();
        }

        public void SelectTargetEnemy(int enemyIndex)
        {
            if (_timeline == null || !_timeline.IsEnemyAlive(enemyIndex)) return;
            _selectedTarget = enemyIndex;
            _feedback = $"대상을 '{_timeline.GetEnemyName(enemyIndex)}'(적 {enemyIndex + 1})로 선택했습니다.";
            RefreshAll();
        }

        public void OpenDeckEditor()
        {
            if (deckModalPanel == null) return;

            for (int i = 0; i < 4; i++) _deckCounts[i] = 0;
            if (GameSessionState.CustomDeck.Count > 0)
            {
                foreach (var w in GameSessionState.CustomDeck)
                    if (GameSessionState.IsLearned(w))
                        _deckCounts[(int)w] = Mathf.Min(CombatDeck.MaxPerWord, _deckCounts[(int)w] + 1);
            }
            else
            {
                for (int i = 0; i < 4; i++)
                    if (GameSessionState.IsLearned((MagicWordType)i))
                        _deckCounts[i] = 1;
            }

            if (seedInputField != null && string.IsNullOrWhiteSpace(seedInputField.text))
                seedInputField.text = "1";

            _deckEnemyCount = Mathf.Clamp(_manager != null ? _manager.CurrentEnemyCount : 1, 1, 3);
            deckModalPanel.SetActive(true);
            RefreshDeckModal();
        }

        private void RebuildHandCards()
        {
            foreach (Button btn in _spawnedCardButtons)
                if (btn != null) Destroy(btn.gameObject);
            _spawnedCardButtons.Clear();

            if (_timeline == null || handCardRoot == null || cardButtonTemplate == null) return;

            foreach (CombatCard card in _timeline.Hand)
            {
                Button btn = Instantiate(cardButtonTemplate, handCardRoot);
                btn.gameObject.SetActive(true);
                int cardId = card.Id;
                btn.onClick.AddListener(() => OnClickHandCard(cardId));
                _spawnedCardButtons.Add(btn);
            }
        }

        public void RefreshAll()
        {
            if (_timeline == null) return;

            bool planning = _timeline.Phase == TimelinePhase.Plan && !_manager.IsAnimating;
            _draftCardIds.RemoveAll(id => !_timeline.Hand.Any(c => c.Id == id));

            if (!_timeline.IsEnemyAlive(_selectedTarget))
                _selectedTarget = _timeline.FirstAliveEnemyIndex();

            _manager?.HighlightTargetEnemy(_selectedTarget);

            // 편집 중 실시간 FutureVision 시뮬레이션 (기획서 5절·10절)
            PlanSimulationPreview sim = planning ? _timeline.SimulateCurrentPlan() : null;

            // 상단 HUD
            if (statusHeaderText != null)
            {
                statusHeaderText.text =
                    $"턴 {_timeline.Turn} | {PhaseNameKo(_timeline.Phase)} | " +
                    $"HP {_timeline.PlayerHp}/{_timeline.PlayerMaxHp} | " +
                    $"마나 {_timeline.Mana}/{_timeline.MaxMana} | 적 {_timeline.EnemyCount}마리";
            }

            if (pileInfoText != null)
            {
                if (sim != null)
                {
                    string enemyPred = string.Join(", ", Enumerable.Range(0, _timeline.EnemyCount)
                        .Select(e => $"적{e + 1} HP {sim.PredictedEnemyHp[e]}(게이지 {sim.PredictedEnemyGauge[e]}{(sim.PredictedEnemyGroggy[e] > 0 ? $"/그로기{sim.PredictedEnemyGroggy[e]}" : "")})"));
                    string hl = sim.Highlights.Count > 0 ? " · " + string.Join(" / ", sim.Highlights) : "";
                    pileInfoText.text = $"예지 결과: 내 HP {sim.PredictedPlayerHp} | {enemyPred}{hl}";
                }
                else
                {
                    pileInfoText.text = _manager.IsDeckBattle
                        ? $"덱 모드 (시드 {_manager.ActiveDeck.Seed}) · 뽑을 더미 {_manager.ActiveDeck.DrawPile.Count} · 버린 더미 {_manager.ActiveDeck.DiscardPile.Count}"
                        : $"{_manager.StageData?.stageName ?? "전투"} · 그로기 밖 ×0.5 / 그로기 중 ×2";
                }
            }

            // 5칸 예지 그리드 갱신
            if (slotViews != null)
            {
                for (int s = 0; s < slotViews.Length && s < _timeline.SlotCount; s++)
                {
                    ForecastSlotView view = slotViews[s];
                    if (view == null) continue;

                    SlotStepReport simRep = sim != null && sim.SlotReports != null && s < sim.SlotReports.Length
                        ? sim.SlotReports[s]
                        : null;

                    bool anyGroggyInSlot = simRep != null && simRep.EnemyResults.Any(r => r.WasGroggy);

                    if (view.headerText != null)
                    {
                        string mark = (_timeline.Phase == TimelinePhase.Execute && _timeline.CurrentSlot == s)
                            ? " ▶"
                            : (_moveFromSlot == s ? " [이동]" : (_selectedSlot == s && planning ? " ●" : ""));
                        string groggyTag = anyGroggyInSlot ? " [그로기×2]" : "";
                        view.headerText.text = $"{s + 1}칸{mark}{groggyTag}";
                    }

                    if (view.backgroundImage != null)
                    {
                        if (_timeline.Phase == TimelinePhase.Execute && _timeline.CurrentSlot == s)
                            view.backgroundImage.color = ColSlotExecuting;
                        else if (_selectedSlot == s && planning)
                            view.backgroundImage.color = ColSlotSelected;
                        else if (anyGroggyInSlot)
                            view.backgroundImage.color = ColSlotGroggy;
                        else if (_timeline.IsCastingSlot(s))
                            view.backgroundImage.color = ColSlotCasting;
                        else
                            view.backgroundImage.color = ColSlotNormal;
                    }

                    if (view.enemyIntentTexts != null)
                    {
                        for (int e = 0; e < view.enemyIntentTexts.Length; e++)
                        {
                            TMP_Text intentTxt = view.enemyIntentTexts[e];
                            if (intentTxt == null) continue;

                            bool hasEnemy = e < _timeline.EnemyCount;
                            intentTxt.gameObject.SetActive(hasEnemy);
                            if (!hasEnemy) continue;

                            EnemyIntentSlot intent = _timeline.GetIntent(e, s);
                            string badge = intent.CounterBadge;
                            string head = string.IsNullOrEmpty(badge)
                                ? $"적{e + 1}: {intent.Label}"
                                : $"적{e + 1}: {intent.Label} <color=#7BE3FF>{badge}</color>";

                            if (planning && simRep != null && e < simRep.EnemyResults.Count)
                            {
                                var er = simRep.EnemyResults[e];
                                string simNote = FormatSimulatedEnemyTag(er);
                                intentTxt.text = string.IsNullOrEmpty(simNote) ? head : $"{head}\n{simNote}";
                                intentTxt.color = OutcomeColor(er.Outcome, intent.power > 0);
                            }
                            else
                            {
                                IntentOutcome outcome = _timeline.GetOutcome(e, s);
                                string note = _timeline.GetOutcomeNote(e, s);
                                intentTxt.text = (outcome != IntentOutcome.Pending && !string.IsNullOrEmpty(note))
                                    ? $"{head}\n[{note}]"
                                    : head;
                                intentTxt.color = OutcomeColor(outcome, intent.power > 0);
                            }
                        }
                    }

                    if (view.playerPlanText != null)
                    {
                        string plan = _timeline.GetPlanLabel(s);
                        if (planning && simRep != null)
                        {
                            if (simRep.ChantInterrupted)
                                plan += "\n<color=#FF6666>[영창 방해!]</color>";
                            else if (simRep.PlayerOrder != null && !simRep.PlayerOrder.Spell.IsSelfTarget)
                            {
                                if (simRep.PlayerDamageDealt > 0)
                                    plan += simRep.WasGroggyHit
                                        ? $"\n<color=#FFE655>예상 -{simRep.PlayerDamageDealt} (×2!)</color>"
                                        : $"\n<color=#B8D4E8>예상 -{simRep.PlayerDamageDealt} (×0.5)</color>";
                                else if (simRep.PlayerSpellMissed)
                                    plan += "\n<color=#AAAAAA>예상 빗나감</color>";
                            }
                        }

                        view.playerPlanText.text = plan;
                        if (_timeline.GetOrder(s) != null)
                            view.playerPlanText.color = new Color(0.55f, 0.95f, 0.72f);
                        else if (_timeline.IsCastingSlot(s))
                            view.playerPlanText.color = new Color(0.95f, 0.80f, 0.42f);
                        else
                            view.playerPlanText.color = new Color(0.62f, 0.66f, 0.74f);
                    }

                    if (view.slotButton != null)
                        view.slotButton.interactable = planning;
                }
            }

            // 손패 버튼 갱신 (특징 아이콘 포함)
            for (int i = 0; i < _spawnedCardButtons.Count && i < _timeline.Hand.Count; i++)
            {
                CombatCard card = _timeline.Hand[i];
                Button btn = _spawnedCardButtons[i];
                bool reserved = _timeline.IsCardReserved(card.Id);
                bool inDraft = _draftCardIds.Contains(card.Id);

                btn.interactable = planning && !reserved;
                Image bg = btn.GetComponent<Image>();
                if (bg != null)
                    bg.color = reserved ? ColCardReserved : (inDraft ? ColCardDraft : ColCardNormal);

                TMP_Text label = btn.GetComponentInChildren<TMP_Text>();
                if (label != null)
                {
                    string trait = WordTraitShort(card.Word);
                    string state = reserved ? "[예약]" : (inDraft ? "[초안]" : "비용1");
                    label.text = $"<b>{SpellResolver.WordName(card.Word)}</b> #{card.Id}\n<size=12><color=#8CE6FF>{trait}</color> · {state}</size>";
                }
            }

            // 초안 & 미리보기
            if (draftSequenceText != null)
            {
                if (_draftCardIds.Count == 0)
                    draftSequenceText.text = $"선택 칸: {_selectedSlot + 1}칸  |  초안: (카드를 클릭해 묶으세요)";
                else
                {
                    string seq = string.Join(" + ", _draftCardIds.Select(id => SpellResolver.WordName(CardById(id).Word)));
                    draftSequenceText.text = $"선택 칸: {_selectedSlot + 1}칸  |  초안: [{seq}]";
                }
            }

            if (spellPreviewText != null)
            {
                if (_draftCardIds.Count == 0)
                {
                    spellPreviewText.text = "미리보기: 대기 중 (단어 특징으로 적 행동을 카운터하세요)";
                    spellPreviewText.color = new Color(0.75f, 0.8f, 0.88f);
                }
                else
                {
                    var words = _draftCardIds.Select(id => CardById(id).Word).ToList();
                    if (SpellResolver.TryBuildSpell(words, out SpellData spell, out string err))
                    {
                        string traits = SpellTraitHelper.FormatAll(spell.Traits);
                        string power = spell.IsSelfTarget
                            ? $"막기 {spell.GuardPower}"
                            : $"기본위력 {spell.ComputeBaseDamage(true, _timeline.PlayerLevelBonus)}";

                        if (_timeline.CanPlaceSpan(_selectedSlot, spell.Cost, -1, out string spanErr))
                        {
                            int startSlot = _selectedSlot - (spell.Cost - 1);
                            string spanText = spell.Cost > 1 ? $"{startSlot + 1}~{_selectedSlot + 1}칸 점유" : $"{_selectedSlot + 1}칸 발동";
                            spellPreviewText.text = $"<b>{spell.Name}</b> (비용 {spell.Cost} · {spanText}) · 특징[{traits}] · {power}";
                            spellPreviewText.color = new Color(0.55f, 0.95f, 0.72f);
                        }
                        else
                        {
                            spellPreviewText.text = $"<b>{spell.Name}</b> (비용 {spell.Cost}) — {spanErr}";
                            spellPreviewText.color = new Color(1f, 0.72f, 0.35f);
                        }
                    }
                    else
                    {
                        spellPreviewText.text = $"배치 불가: {err}";
                        spellPreviewText.color = new Color(1f, 0.45f, 0.45f);
                    }
                }
            }

            // 대상 적 버튼 (1~3) — 약점 게이지 & 그로기 표시
            if (targetEnemyButtons != null)
            {
                for (int e = 0; e < targetEnemyButtons.Length; e++)
                {
                    bool visible = e < _timeline.EnemyCount;
                    targetEnemyButtons[e].gameObject.SetActive(visible);
                    if (!visible) continue;

                    bool alive = _timeline.IsEnemyAlive(e);
                    targetEnemyButtons[e].interactable = planning && alive;
                    Image bg = targetEnemyButtons[e].GetComponent<Image>();
                    if (bg != null)
                        bg.color = (_selectedTarget == e && alive) ? ColTargetSelected : ColTargetNormal;

                    if (targetEnemyLabels != null && e < targetEnemyLabels.Length && targetEnemyLabels[e] != null)
                    {
                        string state = !alive
                            ? "처치"
                            : (_timeline.IsEnemyGroggy(e)
                                ? $"그로기({_timeline.GetEnemyGroggyRemaining(e)})"
                                : $"약점 {BattleManager.BuildGaugeDots(_timeline.GetEnemyWeakGauge(e), _timeline.GetEnemyMaxWeakGauge(e))}");
                        targetEnemyLabels[e].text = $"{(_selectedTarget == e && alive ? "● " : "")}적{e + 1} ({_timeline.GetEnemyHp(e)}) {state}";
                    }
                }
            }

            // 조작 버튼 상태
            if (placeButton != null) placeButton.interactable = planning && _draftCardIds.Count > 0;
            if (moveButton != null) moveButton.interactable = planning && (_moveFromSlot >= 0 || _timeline.IsSlotOccupied(_selectedSlot));
            if (moveButtonLabel != null) moveButtonLabel.text = _moveFromSlot < 0 ? "칸 이동" : "이동 취소";
            if (removeButton != null) removeButton.interactable = planning && _timeline.IsSlotOccupied(_selectedSlot);
            if (retargetButton != null) retargetButton.interactable = planning && _timeline.IsSlotOccupied(_selectedSlot);
            if (clearDraftButton != null) clearDraftButton.interactable = planning && _draftCardIds.Count > 0;

            if (confirmButton != null) confirmButton.interactable = planning;
            if (confirmButtonLabel != null) confirmButtonLabel.text = $"계획 확정 ({_timeline.PlannedCost}/{_timeline.Mana} MP)";

            bool canExec = _timeline.Phase == TimelinePhase.Execute && !_manager.IsAnimating;
            if (stepExecuteButton != null) stepExecuteButton.interactable = canExec;
            if (stepExecuteLabel != null) stepExecuteLabel.text = canExec ? $"{_timeline.CurrentSlot + 1}칸 실행" : "칸 실행";
            if (autoExecuteButton != null) autoExecuteButton.interactable = canExec;
            if (nextTurnButton != null) nextTurnButton.interactable = _timeline.Phase == TimelinePhase.TurnEnd && !_manager.IsAnimating;

            if (feedbackText != null) feedbackText.text = _feedback;

            if (battleLogText != null)
            {
                var log = _timeline.Log;
                int start = Mathf.Max(0, log.Count - 6);
                battleLogText.text = log.Count == 0
                    ? "전투 로그: 카운터 → 약점 격파 → 그로기(×2)에 큰 주문을 맞추세요."
                    : string.Join("\n", log.Skip(start));
            }

            // 결과 배너
            bool finished = _timeline.Phase == TimelinePhase.Victory || _timeline.Phase == TimelinePhase.Defeat;
            if (resultPanel != null)
            {
                resultPanel.SetActive(finished && !_manager.IsAnimating);
                if (finished && resultTitleText != null)
                {
                    resultTitleText.text = _timeline.Phase == TimelinePhase.Victory
                        ? $"전투 승리! (남은 HP {_timeline.PlayerHp}/{_timeline.PlayerMaxHp})"
                        : "전투 패배... 카운터 타이밍을 다시 조정해 보세요.";
                }
            }
        }

        private static string FormatSimulatedEnemyTag(EnemySlotResult er)
        {
            if (er.Outcome == IntentOutcome.Cancelled) return "<color=#888888>[취소]</color>";
            if (er.WasGroggy) return "<color=#FFE655>[그로기 · 취소]</color>";
            if (er.TriggeredBreak) return "<color=#4DF0FF><b>[격파! → 그로기]</b></color>";
            if (er.Countered) return $"<color=#6CE3FF>[카운터! 게이지 {er.GaugeAfter}]</color>";
            if (er.BlockedAmount > 0) return $"<color=#78D8FF>[막기 {er.BlockedAmount}{(er.DamageToPlayer > 0 ? $"/피해{er.DamageToPlayer}" : "")}]</color>";
            if (er.DamageToPlayer > 0) return $"<color=#FF7878>[피격 -{er.DamageToPlayer}]</color>";
            return string.Empty;
        }

        private static string WordTraitShort(MagicWordType w) => w switch
        {
            MagicWordType.Fire => "화염",
            MagicWordType.Shield => "막기/요격",
            MagicWordType.Launch => "투사체",
            MagicWordType.Strong => "강타",
            _ => ""
        };

        private void OnClickSlot(int slotIdx)
        {
            if (_timeline == null || _timeline.Phase != TimelinePhase.Plan) return;

            if (_moveFromSlot >= 0)
            {
                if (_timeline.TryMove(_moveFromSlot, slotIdx, out string err))
                {
                    _feedback = $"{_moveFromSlot + 1}칸 주문을 {slotIdx + 1}칸으로 이동했습니다.";
                    _selectedSlot = slotIdx;
                }
                else
                {
                    _feedback = err;
                }
                _moveFromSlot = -1;
            }
            else
            {
                _selectedSlot = slotIdx;
                _feedback = $"{slotIdx + 1}칸을 선택했습니다.";
            }

            RefreshAll();
        }

        private void OnClickHandCard(int cardId)
        {
            if (!_draftCardIds.Remove(cardId))
                _draftCardIds.Add(cardId);
            RefreshAll();
        }

        private void OnClickPlace()
        {
            if (_timeline.TryPlace(_selectedSlot, _draftCardIds, _selectedTarget, out string err))
            {
                _feedback = $"{_selectedSlot + 1}칸에 [{_timeline.GetPlanLabel(_selectedSlot)}] 배치 완료.";
                _draftCardIds.Clear();
                _moveFromSlot = -1;
            }
            else
            {
                _feedback = err;
            }
            RefreshAll();
        }

        private void OnClickToggleMove()
        {
            if (_moveFromSlot < 0)
            {
                if (!_timeline.IsSlotOccupied(_selectedSlot))
                {
                    _feedback = "비어 있는 칸은 이동할 수 없습니다.";
                }
                else
                {
                    _moveFromSlot = _timeline.GetOrderCastSlot(_selectedSlot);
                    _feedback = $"{_moveFromSlot + 1}칸 주문을 이동할 목적지 발동 칸을 클릭하세요.";
                }
            }
            else
            {
                _moveFromSlot = -1;
                _feedback = "칸 이동을 취소했습니다.";
            }
            RefreshAll();
        }

        private void OnClickRemove()
        {
            if (_timeline.TryRemove(_selectedSlot, out string err))
            {
                _moveFromSlot = -1;
                _feedback = $"{_selectedSlot + 1}칸 주문을 해제했습니다.";
            }
            else
            {
                _feedback = err;
            }
            RefreshAll();
        }

        private void OnClickRetarget()
        {
            if (_timeline.TryRetarget(_selectedSlot, _selectedTarget, out string err))
                _feedback = $"{_selectedSlot + 1}칸 대상을 적 {_selectedTarget + 1}(으)로 변경했습니다.";
            else
                _feedback = err;
            RefreshAll();
        }

        private void OnClickConfirm()
        {
            if (_draftCardIds.Count > 0)
            {
                _feedback = "배치하지 않은 초안 카드가 있습니다. 배치하거나 '초안 비우기'를 누르세요.";
                RefreshAll();
                return;
            }

            if (_timeline.TryConfirm(out string err))
            {
                _moveFromSlot = -1;
                _feedback = "계획 확정 완료! '칸 실행' 또는 '전체 실행'으로 진행하세요.";
            }
            else
            {
                _feedback = err;
            }
            RefreshAll();
        }

        private void AdjustDeckCount(int idx, int delta)
        {
            MagicWordType word = (MagicWordType)idx;
            if (!GameSessionState.IsLearned(word)) return;
            int total = _deckCounts.Sum();
            int next = _deckCounts[idx] + delta;
            if (next < 0 || next > CombatDeck.MaxPerWord) return;
            if (delta > 0 && total >= CombatDeck.MaxSize) return;
            _deckCounts[idx] = next;
            RefreshDeckModal();
        }

        private List<MagicWordType> BuildDeckWordList()
        {
            var list = new List<MagicWordType>();
            for (int i = 0; i < 4; i++)
                for (int c = 0; c < _deckCounts[i]; c++)
                    list.Add((MagicWordType)i);
            return list;
        }

        private void RefreshDeckModal()
        {
            if (deckWordRows != null)
            {
                for (int i = 0; i < deckWordRows.Length && i < 4; i++)
                {
                    DeckWordRowView row = deckWordRows[i];
                    if (row == null) continue;
                    MagicWordType word = (MagicWordType)i;
                    bool learned = GameSessionState.IsLearned(word);

                    if (row.labelText != null)
                        row.labelText.text = $"{SpellResolver.WordName(word)} [{WordTraitShort(word)}]" + (learned ? "" : " (미습득)");
                    if (row.countText != null)
                        row.countText.text = $"{_deckCounts[i]}장";
                    if (row.minusButton != null)
                        row.minusButton.interactable = learned && _deckCounts[i] > 0;
                    if (row.plusButton != null)
                        row.plusButton.interactable = learned && _deckCounts[i] < CombatDeck.MaxPerWord && _deckCounts.Sum() < CombatDeck.MaxSize;
                }
            }

            if (deckEnemyCountButtons != null)
            {
                for (int i = 0; i < deckEnemyCountButtons.Length; i++)
                {
                    Image img = deckEnemyCountButtons[i].GetComponent<Image>();
                    if (img != null) img.color = (_deckEnemyCount == i + 1) ? ColSlotSelected : ColCardNormal;
                }
            }

            if (deckPlayerHpButtons != null)
            {
                for (int i = 0; i < deckPlayerHpButtons.Length && i < PlayerHpOptions.Length; i++)
                {
                    Image img = deckPlayerHpButtons[i].GetComponent<Image>();
                    if (img != null) img.color = (_deckPlayerHp == PlayerHpOptions[i]) ? ColSlotSelected : ColCardNormal;
                }
            }

            if (deckLevelButtons != null)
            {
                for (int i = 0; i < deckLevelButtons.Length && i < LevelBonusOptions.Length; i++)
                {
                    Image img = deckLevelButtons[i].GetComponent<Image>();
                    if (img != null) img.color = (_deckLevelBonus == LevelBonusOptions[i]) ? ColSlotSelected : ColCardNormal;
                }
            }

            var words = BuildDeckWordList();
            bool valid = CombatDeck.TryValidate(GameSessionState.LearnedWords, words, out string err);
            if (deckValidationText != null)
            {
                deckValidationText.text = valid
                    ? $"덱 {words.Count}장 · 손패 {CombatDeck.HandSize}장 · 적 {_deckEnemyCount}마리 · HP {_deckPlayerHp} · 레벨보너스 +{_deckLevelBonus}"
                    : err;
                deckValidationText.color = valid ? new Color(0.55f, 0.95f, 0.72f) : new Color(1f, 0.45f, 0.45f);
            }
            if (startDeckBattleButton != null)
                startDeckBattleButton.interactable = valid;
        }

        private void OnClickStartDeckBattle()
        {
            int seed = 1;
            if (seedInputField != null && !int.TryParse(seedInputField.text, out seed))
            {
                if (deckValidationText != null) deckValidationText.text = "시드는 정수로 입력하세요.";
                return;
            }

            if (_manager == null) return;
            if (_manager.StartDeckBattle(BuildDeckWordList(), _deckEnemyCount, _deckPlayerHp, seed, _deckLevelBonus, out string err))
            {
                if (deckModalPanel != null) deckModalPanel.SetActive(false);
            }
            else if (deckValidationText != null)
            {
                deckValidationText.text = err;
            }
        }

        private CombatCard CardById(int id) => _timeline.Hand.First(c => c.Id == id);

        private static string PhaseNameKo(TimelinePhase phase) => phase switch
        {
            TimelinePhase.Plan => "계획 편집",
            TimelinePhase.Execute => "칸 실행",
            TimelinePhase.TurnEnd => "턴 종료",
            TimelinePhase.Victory => "승리",
            TimelinePhase.Defeat => "패배",
            _ => phase.ToString()
        };

        private static Color OutcomeColor(IntentOutcome outcome, bool isAttack) => outcome switch
        {
            IntentOutcome.Cancelled => new Color(0.55f, 0.55f, 0.6f),
            IntentOutcome.Groggy => new Color(1f, 0.88f, 0.35f),
            IntentOutcome.Break => new Color(0.35f, 0.96f, 1f),
            IntentOutcome.Countered => new Color(0.45f, 0.90f, 1f),
            IntentOutcome.Blocked => new Color(0.52f, 0.86f, 1f),
            IntentOutcome.Missed => new Color(0.7f, 0.7f, 0.7f),
            IntentOutcome.Executed => isAttack ? new Color(1f, 0.52f, 0.48f) : new Color(0.85f, 0.88f, 0.95f),
            _ => isAttack ? new Color(1f, 0.74f, 0.45f) : new Color(0.82f, 0.86f, 0.92f)
        };
    }
}
