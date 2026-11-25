using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using TxRpg.Animation;
using TxRpg.Camera;
using TxRpg.Core;
using TxRpg.Core.Events;
using TxRpg.Data;
using TxRpg.Effect;
using TxRpg.Entity;
using TxRpg.Magic;
using TxRpg.Skill;
using TxRpg.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TxRpg.Battle
{
    /// <summary>
    /// TxRpg 배틀 매니저 (전투 개선 기획서 · 약점 게이지 · 카운터 · 그로기 · 1vs1~3 지원).
    /// </summary>
    public class BattleManager : MonoBehaviour
    {
        [Header("이벤트 채널")]
        [SerializeField] private BattleStartEventChannel battleStartChannel;
        [SerializeField] private BattleEndEventChannel battleEndChannel;
        [SerializeField] private TurnChangedEventChannel turnChangedChannel;

        [Header("배틀 루트 및 데이터")]
        [SerializeField] private Transform battleRoot;
        [SerializeField] private UnitDataSO playerUnitData;
        [SerializeField] private StageDataSO[] encounterStages; // 0:돌격기사, 1:방패병, 2:궁수, 3:1v2, 4:1v3, 5:보스

        [Header("연출 프리팹 & UI")]
        [SerializeField] private ProjectileController projectilePrefab;
        [SerializeField] private DamagePopup damagePopupPrefab;
        [SerializeField] private BattleForecastUI forecastUI;
        [SerializeField] private BattleCameraController battleCamera;

        [Header("초기 시작 옵션")]
        [SerializeField] private bool autoStartOnPlay = true;
        [SerializeField] private int defaultStageIndex = 0;

        // 기존 TxRpg 호환 프로퍼티
        public TurnManager TurnManager { get; private set; }
        public ActionQueue ActionQueueInstance { get; private set; }
        public AIController AI { get; private set; }
        public SkillExecutor SkillExecutor { get; set; }
        public SkillSelectUI SkillSelectUI { get; set; }
        public BattleCommandUI BattleCommandUI { get; set; }

        public List<BattleEntity> Players { get; private set; } = new();
        public List<BattleEntity> Enemies { get; private set; } = new();
        public List<BattleEntity> ActionOrder { get; set; } = new();
        public List<BattleEntity> AllEntities => Players.Concat(Enemies).ToList();

        public StageDataSO StageData { get; private set; }
        public BattleResult Result { get; private set; }
        public bool IsBattleOver { get; private set; }

        // 타임라인 & 덱 상태
        public CombatTimeline Timeline { get; private set; }
        public CombatDeck ActiveDeck { get; private set; }
        public bool IsDeckBattle => ActiveDeck != null;
        public bool IsAnimating { get; private set; }
        public int CurrentStageIndex { get; private set; }
        public int CurrentEnemyCount { get; private set; } = 1;
        public int CurrentLevelBonus { get; private set; }

        private EntityFactory _entityFactory;
        private CancellationTokenSource _battleCts;
        private UnitDataSO[] _partyData;

        private void Awake()
        {
            TurnManager = new TurnManager();
            ActionQueueInstance = new ActionQueue();
            AI = new AIController();

            if (battleRoot == null)
                battleRoot = transform;

            _entityFactory = new EntityFactory(battleRoot);
            ServiceLocator.Register(this);

            if (battleCamera == null)
                battleCamera = FindFirstObjectByType<BattleCameraController>();
            if (forecastUI == null)
                forecastUI = FindFirstObjectByType<BattleForecastUI>();
        }

        private void Start()
        {
            if (!autoStartOnPlay) return;

            if (GameSessionState.OpenDeckEditorOnBattleEnter)
            {
                GameSessionState.OpenDeckEditorOnBattleEnter = false;
                StartStagePreset(0);
                forecastUI?.OpenDeckEditor();
            }
            else
            {
                StartStagePreset(defaultStageIndex);
            }
        }

        public void StartStagePreset(int stageIndex, int levelBonus = 0)
        {
            StageDataSO stage = ResolveStageByIndex(stageIndex);
            if (stage == null) return;

            CurrentStageIndex = Mathf.Clamp(stageIndex, 0, Math.Max(0, (encounterStages?.Length ?? 1) - 1));
            CurrentEnemyCount = Mathf.Clamp(stage.enemies != null ? stage.enemies.Length : 1, 1, 3);
            CurrentLevelBonus = Math.Max(0, levelBonus);
            ActiveDeck = null;

            int playerHp = stage.playerHpOverride > 0 ? stage.playerHpOverride : 12;
            SetupArenaAndTimeline(stage, CurrentEnemyCount, playerHp, null, null, CurrentLevelBonus);
        }

        // 기존 호환 호출 (1~3마리)
        public void StartFixedBattle(int enemyCount, int enemyHp = 20)
        {
            int idx = enemyCount switch
            {
                2 => 3,
                3 => 4,
                _ => 0
            };
            StartStagePreset(idx, CurrentLevelBonus);
        }

        public bool StartDeckBattle(IReadOnlyList<MagicWordType> deckWords, int enemyCount, int playerHp, int seed, int levelBonus, out string error)
        {
            enemyCount = Mathf.Clamp(enemyCount, 1, 3);
            if (!CombatDeck.TryCreate(GameSessionState.LearnedWords, deckWords, seed, out CombatDeck deck, out error))
                return false;

            GameSessionState.SetCustomDeck(deckWords);
            GameSessionState.PreferredEnemyCount = enemyCount;
            CurrentEnemyCount = enemyCount;
            CurrentLevelBonus = Math.Max(0, levelBonus);
            ActiveDeck = deck;

            int stageIdx = enemyCount == 1 ? 0 : (enemyCount == 2 ? 3 : 4);
            CurrentStageIndex = stageIdx;
            StageDataSO stage = ResolveStageByIndex(stageIdx);
            SetupArenaAndTimeline(stage, enemyCount, playerHp, deck.Draw, deck.DiscardHand, CurrentLevelBonus);
            return true;
        }

        private StageDataSO ResolveStageByIndex(int index)
        {
            if (encounterStages == null || encounterStages.Length == 0) return null;
            int clamped = Mathf.Clamp(index, 0, encounterStages.Length - 1);
            return encounterStages[clamped] != null ? encounterStages[clamped] : encounterStages[0];
        }

        private void SetupArenaAndTimeline(
            StageDataSO stage,
            int enemyCount,
            int playerHp,
            Func<IReadOnlyList<CombatCard>> drawFunc,
            Action discardFunc,
            int levelBonus)
        {
            _battleCts?.Cancel();
            _battleCts?.Dispose();
            _battleCts = new CancellationTokenSource();
            IsAnimating = false;
            IsBattleOver = false;
            StageData = stage;

            int[] enemyHps = new int[enemyCount];
            int[] gaugeOverrides = new int[enemyCount];
            int[] groggyOverrides = new int[enemyCount];
            EnemyPatternSO[] patterns = new EnemyPatternSO[enemyCount];

            for (int i = 0; i < enemyCount; i++)
            {
                if (stage != null && stage.enemies != null && i < stage.enemies.Length)
                {
                    var spawn = stage.enemies[i];
                    enemyHps[i] = spawn.hpOverride > 0 ? spawn.hpOverride : 20;
                    gaugeOverrides[i] = spawn.weakGaugeOverride;
                    groggyOverrides[i] = spawn.groggyDurationOverride;
                    patterns[i] = spawn.intentPattern;
                }
                else
                {
                    enemyHps[i] = 20;
                }
            }

            _partyData = playerUnitData != null ? new[] { playerUnitData } : Array.Empty<UnitDataSO>();
            SpawnEntities();

            int slots = stage != null && stage.slotCount > 0 ? stage.slotCount : CombatTimeline.DefaultSlotCount;
            int mana = stage != null && stage.manaPerTurn > 0 ? stage.manaPerTurn : CombatTimeline.DefaultManaPerTurn;
            Timeline = new CombatTimeline(
                playerHp,
                enemyHps,
                patterns,
                slots,
                mana,
                drawFunc,
                discardFunc,
                levelBonus,
                gaugeOverrides,
                groggyOverrides);

            SyncAllEntityVisuals();
            RaiseBattleStart();
            RaiseTurnChanged();
            forecastUI?.BindTimeline(this, Timeline);
        }

        public void SpawnEntities()
        {
            foreach (var entity in Players) if (entity != null) Destroy(entity.gameObject);
            foreach (var entity in Enemies) if (entity != null) Destroy(entity.gameObject);
            Players.Clear();
            Enemies.Clear();

            if (StageData == null) return;

            if (_partyData != null && _partyData.Length > 0)
            {
                Players = _entityFactory.CreatePlayerParty(_partyData, StageData.playerSlots);
                for (int i = 0; i < Players.Count; i++)
                    Players[i].SlotIndex = i;
            }

            if (StageData.enemies != null && StageData.enemies.Length > 0)
            {
                var spawnList = StageData.enemies.Take(CurrentEnemyCount).ToArray();
                Enemies = _entityFactory.CreateEnemyFormation(spawnList);
                for (int i = 0; i < Enemies.Count; i++)
                {
                    int idx = i;
                    Enemies[i].SlotIndex = idx;
                    Enemies[i].Clicked += OnEnemyClicked;
                }
            }
        }

        private void OnEnemyClicked(int enemyIndex)
        {
            if (Timeline != null && Timeline.Phase == TimelinePhase.Plan && Timeline.IsEnemyAlive(enemyIndex))
                forecastUI?.SelectTargetEnemy(enemyIndex);
        }

        public async UniTask StepExecuteAsync()
        {
            if (Timeline == null || IsAnimating || Timeline.Phase != TimelinePhase.Execute)
                return;

            IsAnimating = true;
            forecastUI?.RefreshAll();

            try
            {
                if (Timeline.ResolveNextSlot(out SlotStepReport report))
                {
                    await AnimateSlotReportAsync(report, _battleCts.Token);
                }

                UpdateBattleResult();
                SyncAllEntityVisuals();
            }
            catch (OperationCanceledException) { }
            finally
            {
                IsAnimating = false;
                forecastUI?.RefreshAll();
            }
        }

        public async UniTask AutoExecuteAllAsync()
        {
            while (Timeline != null && Timeline.Phase == TimelinePhase.Execute && !_battleCts.IsCancellationRequested)
            {
                await StepExecuteAsync();
                if (Timeline != null && Timeline.Phase == TimelinePhase.Execute)
                    await UniTask.Delay(180, cancellationToken: _battleCts.Token);
            }
        }

        public void NextTurn()
        {
            if (Timeline == null || IsAnimating || Timeline.Phase != TimelinePhase.TurnEnd)
                return;

            Timeline.NextTurn();
            TurnManager.AdvanceTurn();
            SyncAllEntityVisuals();
            RaiseTurnChanged();
            forecastUI?.OnNewTurnStarted();
        }

        public void LoadPuzzleScene()
        {
            SceneManager.LoadScene(GameSessionState.PuzzleSceneName);
        }

        private async UniTask AnimateSlotReportAsync(SlotStepReport report, CancellationToken ct)
        {
            BattleEntity player = Players.Count > 0 ? Players[0] : null;

            // 1. 방패 활성화
            if (player != null && report.GuardActivated > 0)
            {
                player.SetShieldActive(true);
                SpawnPopup($"방패 +{report.GuardActivated}", new Color(0.45f, 0.85f, 1f), player.Hitbox.Top);
                await UniTask.Delay(140, cancellationToken: ct);
            }

            // 2. 적 행동 연출 (접근/후퇴/방어/공격/그로기)
            foreach (EnemySlotResult er in report.EnemyResults)
            {
                if (er.EnemyIndex < 0 || er.EnemyIndex >= Enemies.Count) continue;
                BattleEntity enemy = Enemies[er.EnemyIndex];
                if (enemy == null) continue;

                if (er.Outcome == IntentOutcome.Cancelled)
                    continue;

                if (er.WasGroggy)
                {
                    SpawnPopup("그로기!", new Color(1f, 0.86f, 0.32f), enemy.Hitbox.Top);
                    continue;
                }

                if (er.MovedNear)
                {
                    Vector3 nearPos = (Vector3)enemy.FormationPosition + Vector3.left * 1.6f;
                    await TweenHelper.MoveToAsync(enemy.transform, nearPos, 0.16f, ct);
                }
                else if (er.MovedFar)
                {
                    await TweenHelper.MoveToAsync(enemy.transform, enemy.FormationPosition, 0.16f, ct);
                }
                else if (er.Intent.kind == EnemyIntentKind.Defend)
                {
                    SpawnPopup("방어 자세", new Color(0.75f, 0.85f, 1f), enemy.Hitbox.Top);
                }
                else if (er.Outcome == IntentOutcome.Missed)
                {
                    SpawnPopup("근접 빗나감", new Color(0.75f, 0.75f, 0.75f), enemy.Hitbox.Top);
                }
                else if (er.Intent.power > 0 && player != null)
                {
                    if (er.Intent.kind == EnemyIntentKind.Missile)
                    {
                        Vector3 end = er.BlockedAmount > 0 && er.DamageToPlayer == 0
                            ? Vector3.Lerp(enemy.Hitbox.Center, player.Hitbox.Center, 0.78f)
                            : (Vector3)player.Hitbox.Center;
                        await LaunchProjectileAsync(enemy.Hitbox.Center, end, new Color(0.75f, 0.35f, 1f), 0.9f, ct);
                    }
                    else
                    {
                        Vector3 orig = enemy.transform.position;
                        await TweenHelper.MoveToAsync(enemy.transform, orig + Vector3.left * 1.2f, 0.11f, ct);
                        await TweenHelper.MoveToAsync(enemy.transform, orig, 0.11f, ct);
                    }

                    if (er.BlockedAmount > 0)
                        SpawnPopup($"막기 {er.BlockedAmount}", new Color(0.45f, 0.9f, 1f), player.Hitbox.Top + Vector2.up * 0.35f);

                    if (er.DamageToPlayer > 0)
                    {
                        battleCamera?.Shake(0.22f, 0.16f);
                        SpawnPopup($"-{er.DamageToPlayer}", new Color(1f, 0.35f, 0.35f), player.Hitbox.Top);
                        await player.Animator.PlayHitAsync(ct);
                    }
                }
            }

            // 영창 방해 연출
            if (report.ChantInterrupted && player != null)
            {
                SpawnPopup($"영창 흩어짐!", new Color(1f, 0.32f, 0.32f), player.Hitbox.Top + Vector2.up * 0.7f);
                await UniTask.Delay(140, cancellationToken: ct);
            }

            // 3. 플레이어 공격 주문 연출
            if (player != null && report.PlayerOrder != null && !report.PlayerOrder.Spell.IsSelfTarget)
            {
                int targetIdx = report.PlayerOrder.TargetEnemyIndex;
                BattleEntity target = targetIdx >= 0 && targetIdx < Enemies.Count ? Enemies[targetIdx] : null;
                SpellData spell = report.PlayerOrder.Spell;

                if (target != null)
                {
                    if (spell.IsLaunched)
                    {
                        Color tint = spell.IsBurning
                            ? new Color(1f, 0.45f, 0.15f)
                            : (spell.Body == MagicWordType.Fire ? new Color(1f, 0.35f, 0.2f) : new Color(0.4f, 0.8f, 1f));
                        await LaunchProjectileAsync(player.Hitbox.Center, target.Hitbox.Center, tint, spell.IsStrong ? 1.35f : 1f, ct);
                    }
                    else
                    {
                        Vector3 lunge = (Vector3)player.FormationPosition + Vector3.right * 1.1f;
                        await TweenHelper.MoveToAsync(player.transform, lunge, 0.11f, ct);
                        await TweenHelper.MoveToAsync(player.transform, player.FormationPosition, 0.12f, ct);
                    }

                    if (report.PlayerDamageDealt > 0)
                    {
                        battleCamera?.Shake(report.WasGroggyHit ? 0.32f : 0.16f, report.WasGroggyHit ? 0.24f : 0.15f);
                        string dmgLabel = report.WasGroggyHit ? $"-{report.PlayerDamageDealt} ×2!" : $"-{report.PlayerDamageDealt}";
                        Color dmgColor = report.WasGroggyHit ? new Color(1f, 0.92f, 0.25f) : new Color(1f, 0.75f, 0.38f);
                        SpawnPopup(dmgLabel, dmgColor, target.Hitbox.Top, report.WasGroggyHit ? 1.35f : 1f);
                        await target.Animator.PlayHitAsync(ct);

                        if (report.TargetKilled)
                        {
                            SpawnPopup("처치!", new Color(1f, 0.4f, 0.4f), target.Hitbox.Top + Vector2.up * 0.5f);
                            await target.PlayDeathAsync(ct);
                        }
                    }
                    else if (report.PlayerSpellMissed)
                    {
                        SpawnPopup("빗나감", new Color(0.8f, 0.8f, 0.8f), target.Hitbox.Top);
                    }
                }
            }

            // 4. 카운터 / 약점 격파 팝업 연출
            foreach (EnemySlotResult er in report.EnemyResults)
            {
                if (er.EnemyIndex < 0 || er.EnemyIndex >= Enemies.Count) continue;
                BattleEntity enemy = Enemies[er.EnemyIndex];
                if (enemy == null) continue;

                if (er.TriggeredBreak)
                {
                    battleCamera?.Shake(0.35f, 0.26f);
                    SpawnPopup("약점 격파! 그로기!", new Color(0.35f, 0.95f, 1f), enemy.Hitbox.Top + Vector2.up * 0.65f, 1.3f);
                    await UniTask.Delay(180, cancellationToken: ct);
                }
                else if (er.Countered)
                {
                    SpawnPopup($"카운터! -1", new Color(0.45f, 0.92f, 1f), enemy.Hitbox.Top + Vector2.up * 0.55f, 1.1f);
                }
            }

            if (player != null)
                player.SetShieldActive(false);
        }

        private async UniTask LaunchProjectileAsync(Vector3 from, Vector3 to, Color color, float scale, CancellationToken ct)
        {
            if (projectilePrefab == null)
            {
                await UniTask.Delay(110, cancellationToken: ct);
                return;
            }

            ProjectileController proj = Instantiate(projectilePrefab, from, Quaternion.identity, battleRoot);
            proj.ConfigureVisual(color, scale);
            try
            {
                await proj.LaunchAsync(from, to, ct);
            }
            finally
            {
                if (proj != null) Destroy(proj.gameObject);
            }
        }

        private void SpawnPopup(string text, Color color, Vector3 worldPos, float scale = 1f)
        {
            if (damagePopupPrefab == null) return;
            DamagePopup popup = Instantiate(damagePopupPrefab, worldPos, Quaternion.identity);
            popup.ShowText(text, color, worldPos, scale);
        }

        private void SyncAllEntityVisuals()
        {
            if (Timeline == null) return;

            if (Players.Count > 0 && Players[0] != null)
            {
                string lv = Timeline.PlayerLevelBonus > 0 ? $" · 보너스+{Timeline.PlayerLevelBonus}" : "";
                Players[0].SyncHp(
                    Timeline.PlayerHp,
                    Timeline.PlayerMaxHp,
                    $"기사 HP {Timeline.PlayerHp}/{Timeline.PlayerMaxHp}",
                    $"MP {Timeline.Mana}/{Timeline.MaxMana}{lv}");
            }

            for (int e = 0; e < Enemies.Count && e < Timeline.EnemyCount; e++)
            {
                if (Enemies[e] == null) continue;
                int hp = Timeline.GetEnemyHp(e);
                int maxHp = Timeline.GetEnemyMaxHp(e);
                bool groggy = Timeline.IsEnemyGroggy(e);

                string status;
                if (hp <= 0)
                {
                    status = "처치됨";
                }
                else if (groggy)
                {
                    status = $"그로기 {Timeline.GetEnemyGroggyRemaining(e)}칸 (×2)";
                }
                else
                {
                    string gaugeDots = BuildGaugeDots(Timeline.GetEnemyWeakGauge(e), Timeline.GetEnemyMaxWeakGauge(e));
                    string range = Timeline.IsEnemyNear(e) ? "근" : "원";
                    status = $"약점 {gaugeDots} · {range}";
                }

                Enemies[e].SyncHp(hp, maxHp, $"{Timeline.GetEnemyName(e)} ({hp}/{maxHp})", status, groggy);
            }
        }

        public static string BuildGaugeDots(int current, int max)
        {
            current = Mathf.Clamp(current, 0, max);
            return new string('●', current) + new string('○', Math.Max(0, max - current));
        }

        public void HighlightTargetEnemy(int selectedIndex)
        {
            for (int e = 0; e < Enemies.Count; e++)
            {
                if (Enemies[e] != null)
                    Enemies[e].SetTargetHighlighted(e == selectedIndex && Timeline != null && Timeline.IsEnemyAlive(e));
            }
        }

        private void UpdateBattleResult()
        {
            if (Timeline == null) return;
            if (Timeline.Phase == TimelinePhase.Victory)
            {
                IsBattleOver = true;
                Result = BattleResult.Victory;
                RaiseBattleEnd();
            }
            else if (Timeline.Phase == TimelinePhase.Defeat)
            {
                IsBattleOver = true;
                Result = BattleResult.Defeat;
                RaiseBattleEnd();
            }
        }

        public bool CheckBattleEnd() => IsBattleOver;

        public void SetFlee()
        {
            IsBattleOver = true;
            Result = BattleResult.Flee;
        }

        public void RaiseBattleStart()
        {
            battleStartChannel?.Raise(new BattleStartPayload { StageName = StageData != null ? StageData.stageName : "BattleTest" });
        }

        public void RaiseBattleEnd()
        {
            battleEndChannel?.Raise(new BattleEndPayload { IsVictory = Result == BattleResult.Victory });
        }

        public void RaiseTurnChanged()
        {
            turnChangedChannel?.Raise(new TurnChangedPayload
            {
                TurnNumber = Timeline != null ? Timeline.Turn : TurnManager.CurrentTurn,
                CurrentEntity = Players.Count > 0 ? Players[0].gameObject : null
            });
        }

        private void OnDestroy()
        {
            _battleCts?.Cancel();
            _battleCts?.Dispose();
            ServiceLocator.Clear();
        }
    }
}
