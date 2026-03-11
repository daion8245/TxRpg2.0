using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using TxRpg.Core;
using TxRpg.Core.Events;
using TxRpg.Data;
using TxRpg.Entity;
using TxRpg.Skill;
using TxRpg.UI;
using UnityEngine;

namespace TxRpg.Battle
{
    public class BattleManager : MonoBehaviour
    {
        [Header("이벤트 채널")]
        [SerializeField] private BattleStartEventChannel battleStartChannel;
        [SerializeField] private BattleEndEventChannel battleEndChannel;
        [SerializeField] private TurnChangedEventChannel turnChangedChannel;

        [Header("배틀 루트")]
        [SerializeField] private Transform battleRoot;

        // 시스템
        public TurnManager TurnManager { get; private set; }
        public ActionQueue ActionQueueInstance { get; private set; }
        public AIController AI { get; private set; }
        public SkillExecutor SkillExecutor { get; set; }
        public SkillSelectUI SkillSelectUI { get; set; }
        public BattleCommandUI BattleCommandUI { get; set; }

        // 엔티티
        public List<BattleEntity> Players { get; private set; } = new();
        public List<BattleEntity> Enemies { get; private set; } = new();
        public List<BattleEntity> ActionOrder { get; set; } = new();
        public List<BattleEntity> AllEntities => Players.Concat(Enemies).ToList();

        // 상태
        public StageDataSO StageData { get; private set; }
        public BattleResult Result { get; private set; }
        public bool IsBattleOver { get; private set; }

        private EntityFactory _entityFactory;
        private BattleStateMachine _stateMachine;
        private CancellationTokenSource _battleCts;

        private void Awake()
        {
            TurnManager = new TurnManager();
            ActionQueueInstance = new ActionQueue();
            AI = new AIController();

            if (battleRoot == null)
                battleRoot = transform;

            _entityFactory = new EntityFactory(battleRoot);

            ServiceLocator.Register(this);
        }

        public void StartBattle(StageDataSO stageData, UnitDataSO[] party)
        {
            StageData = stageData;
            IsBattleOver = false;

            _battleCts?.Cancel();
            _battleCts = new CancellationTokenSource();

            _stateMachine = new BattleStateMachine(this);

            // 파티 데이터 저장 (SpawnEntities에서 사용)
            _partyData = party;

            _stateMachine.RunBattle(_battleCts.Token).Forget();
        }

        private UnitDataSO[] _partyData;

        public void SpawnEntities()
        {
            // 기존 엔티티 정리
            foreach (var entity in Players) if (entity != null) Destroy(entity.gameObject);
            foreach (var entity in Enemies) if (entity != null) Destroy(entity.gameObject);
            Players.Clear();
            Enemies.Clear();

            // 아군 생성
            Players = _entityFactory.CreatePlayerParty(_partyData, StageData.playerSlots);

            // 적군 생성
            Enemies = _entityFactory.CreateEnemyFormation(StageData.enemies);
        }

        public bool CheckBattleEnd()
        {
            if (Enemies.All(e => !e.IsAlive))
            {
                IsBattleOver = true;
                Result = BattleResult.Victory;
                return true;
            }

            if (Players.All(e => !e.IsAlive))
            {
                IsBattleOver = true;
                Result = BattleResult.Defeat;
                return true;
            }

            return false;
        }

        public void SetFlee()
        {
            IsBattleOver = true;
            Result = BattleResult.Flee;
        }

        public void RaiseBattleStart()
        {
            battleStartChannel?.Raise(new BattleStartPayload { StageName = StageData.stageName });
        }

        public void RaiseBattleEnd()
        {
            battleEndChannel?.Raise(new BattleEndPayload { IsVictory = Result == BattleResult.Victory });
        }

        public void RaiseTurnChanged()
        {
            turnChangedChannel?.Raise(new TurnChangedPayload
            {
                TurnNumber = TurnManager.CurrentTurn,
                CurrentEntity = ActionOrder.Count > 0 ? ActionOrder[0].gameObject : null
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
