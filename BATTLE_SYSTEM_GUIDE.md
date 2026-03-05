# 유니티 배틀 시스템 적용 가이드 (초보자용)

> TxRpg2.0 배틀 시스템을 처음 접하는 개발자를 위한 단계별 가이드입니다.

---

## 먼저 알아야 할 것: 클래스 분류

이 배틀 시스템에는 두 종류의 클래스가 있습니다.
**이 분류를 이해하는 것이 가장 중요합니다!**

### MonoBehaviour 클래스 (씬에 GameObject로 배치해야 함)

| 클래스 | 역할 | 비고 |
|--------|------|------|
| `BattleManager` | 배틀 전체 관리 | Awake()에서 내부 클래스들을 `new`로 자동 생성 |
| `SoundManager` | 사운드 재생 | |
| `EffectManager` | 시각 이펙트 | |
| `BattleCameraController` | 카메라 제어 | Camera 컴포넌트 필요 |
| `SkillSelectUI` | 스킬 선택 UI | Canvas 하위에 배치 |
| `BattleHUD` | HP바 등 전투 HUD | Canvas 하위에 배치 |
| `BattleEntity` | 캐릭터 엔티티 | **프리팹**으로 존재, 런타임에 자동 생성됨 |
| `DamagePopup` | 데미지 숫자 팝업 | **프리팹**으로 존재, 풀링 |

### 일반 C# 클래스 (씬에 배치하지 않음! 코드에서 `new`로 자동 생성됨)

| 클래스 | 누가 생성하나? | 역할 |
|--------|--------------|------|
| `BattleStateMachine` | BattleManager.StartBattle() | 배틀 흐름 상태 머신 |
| `TurnManager` | BattleManager.Awake() | 턴 카운터, 속도순 정렬 |
| `ActionQueue` | BattleManager.Awake() | 행동 대기열 |
| `AIController` | BattleManager.Awake() | 적 AI 행동 결정 |
| `EntityFactory` | BattleManager.Awake() | 프리팹으로 BattleEntity 생성 |
| `SkillExecutor` | 코드 내부 | 스킬 타임라인 실행 |
| `EntityStats` | BattleEntity.Initialize() | HP/MP/ATK 등 스탯 |
| `EntitySkills` | BattleEntity.Initialize() | 스킬 목록 + 쿨다운 |
| `EntityBuffs` | BattleEntity.Initialize() | 버프/디버프 관리 |
| `EntityHitbox` | BattleEntity.Initialize() | 피격 판정 위치 |
| `EntityAnimator` | BattleEntity.Initialize() | 애니메이션 제어 |
| `SpriteAnimAdapter` | BattleEntity.Initialize() | Animator 컴포넌트 래핑 |
| `DamageCalculator` | 생성 불필요 (static 클래스) | 데미지 공식 계산 |

> 즉, **BattleManager 하나만 씬에 배치하면** TurnManager, ActionQueue, AIController 등은
> Awake()에서 자동으로 만들어집니다. 직접 씬에 추가할 필요가 없습니다!

---

## 배틀 시스템이 어떻게 동작하나?

```
[게임 시작]
    ↓
BattleManager.StartBattle(stageData, party)  ← 배틀 시작 명령
    ↓
BattleManager.Awake()에서 이미 생성된 것들:
  - TurnManager (new)
  - ActionQueue (new)
  - AIController (new)
  - EntityFactory (new)
    ↓
[배틀 루프 - BattleStateMachine이 관리]
TurnStartPhase    → 턴 시작 (속도순 정렬, 버프 처리)
ActionSelectPhase → 행동 선택 (플레이어: SkillSelectUI / 적: AIController)
ExecutePhase      → 행동 실행 (SkillExecutor가 타임라인 재생)
TurnEndPhase      → 턴 종료 (정리)
    ↓
ResultPhase       → 승리/패배 결과 화면
```

---

## 단계별 적용 가이드

### STEP 1: 데이터 에셋 만들기 (ScriptableObject)

> **ScriptableObject** = 게임 데이터를 담는 "데이터 파일"이라고 생각하세요.
> 스크립트(.cs)는 이미 만들어져 있고, 여러분은 Unity 에디터에서 **에셋(.asset)을 생성하고 값을 입력**하면 됩니다.

#### 캐릭터 데이터 에셋 (UnitDataSO)
- Unity 에디터에서: 프로젝트 창 우클릭 → Create → UnitData
- 스크립트 위치: `Assets/Scripts/BattleSystem/Data/UnitDataSO.cs`

| 항목 | 설명 |
|------|------|
| Name | 캐릭터 이름 |
| HP / MP | 체력 / 마나 |
| ATK | 공격력 |
| DEF | 방어력 |
| SPD | 속도 (턴 순서 결정) |
| KD | 크리티컬 확률 (%) |
| KR | 크리티컬 피해 (%) |
| Prefab | 캐릭터 프리팹 (BattleEntity가 붙은 것) |
| Skills | 보유 스킬 목록 (SkillDataSO 에셋 연결) |

#### 스킬 데이터 에셋 (SkillDataSO)
- 스크립트 위치: `Assets/Scripts/BattleSystem/Data/SkillDataSO.cs`

| 항목 | 선택지 |
|------|--------|
| Category | Physical / Magical / Healing / Buff / Debuff |
| Element | None / Fire / Ice / Lightning / Dark / Holy |
| TargetType | 단일적 / 전체적 / 단일아군 / 전체아군 / 자신 |
| Power | 스킬 위력 (데미지 공식에 사용) |
| MP Cost | 마나 소모량 |
| Cooldown | 재사용 대기 턴 수 |
| Timeline | 스킬 연출 순서 (아래 STEP 6 참고) |

#### 스테이지 데이터 에셋 (StageDataSO)
- 스크립트 위치: `Assets/Scripts/BattleSystem/Data/StageDataSO.cs`
- 등장 몬스터 목록, 플레이어 배치 슬롯, 보상(골드/아이템) 설정

---

### STEP 2: 씬 설정하기

**배틀 씬 열기**
```
Assets/Scenes/Batttle/BattleTest.unity
```

씬에 배치해야 할 **실제 MonoBehaviour 오브젝트**:
```
Scene
├── BattleManager              ← 배틀 전체 관리 (핵심!)
│   (내부에서 TurnManager, ActionQueue, AIController, EntityFactory를 자동 생성)
│
├── SoundManager               ← 사운드 재생
├── EffectManager              ← 시각 이펙트
├── BattleCameraController     ← 카메라 (Camera 컴포넌트 필요)
│
└── Canvas                     ← UI
    ├── SkillSelectUI          ← 스킬 선택 창
    └── BattleHUD              ← HP바, 턴 표시
```

**씬에 배치하면 안 되는 것들** (코드에서 자동 생성됨):
- ~~TurnManager~~ → BattleManager가 `new TurnManager()`로 생성
- ~~ActionQueue~~ → BattleManager가 `new ActionQueue()`로 생성
- ~~AIController~~ → BattleManager가 `new AIController()`로 생성
- ~~SkillExecutor~~ → 코드 내부에서 자동 생성
- ~~EntityFactory~~ → BattleManager가 `new EntityFactory(battleRoot)`로 생성

#### 인스펙터에서 이벤트 채널 연결하기

각 MonoBehaviour에는 `[SerializeField]`로 선언된 이벤트 채널 필드가 있습니다.
**반드시 인스펙터에서 해당 ScriptableObject 에셋을 드래그&드롭으로 연결해야 합니다!**

| 오브젝트 | 연결해야 할 이벤트 채널 |
|----------|----------------------|
| BattleManager | BattleStartEventChannel, BattleEndEventChannel, TurnChangedEventChannel |
| SoundManager | PlaySFXEventChannel, PlayBGMEventChannel, StopBGMEventChannel |
| EffectManager | SpawnEffectEventChannel |
| BattleCameraController | CameraShakeEventChannel, CameraZoomEventChannel |
| BattleHUD | TurnChangedEventChannel, DamageEventChannel |

> 이벤트 채널 에셋은 `Assets/Scripts/BattleSystem/Core/EventBus/Events/` 폴더에서
> 우클릭 → Create 메뉴로 생성할 수 있습니다.

---

### STEP 3: 캐릭터 프리팹 만들기

스크립트 위치: `Assets/Scripts/BattleSystem/Entity/BattleEntity.cs`

프리팹에 추가할 **실제 Unity 컴포넌트**:
```
[캐릭터 프리팹 - GameObject]
├── BattleEntity (스크립트)    ← 유일하게 추가하는 커스텀 스크립트!
├── Animator                    ← Unity 기본 컴포넌트 (애니메이션 재생용)
├── SpriteRenderer              ← Unity 기본 컴포넌트 (캐릭터 그림 표시)
├── Collider2D                  ← Unity 기본 컴포넌트 (피격 판정용)
└── (선택) "HitPoint" 자식 오브젝트  ← 이펙트가 생성될 위치
```

**프리팹에 추가하면 안 되는 것들** (BattleEntity.Initialize()가 코드에서 자동 생성):
- ~~EntityStats~~ → `new EntityStats(unitData)` 로 자동 생성
- ~~EntitySkills~~ → `new EntitySkills(skillArray)` 로 자동 생성
- ~~EntityBuffs~~ → `new EntityBuffs(stats)` 로 자동 생성
- ~~EntityHitbox~~ → `new EntityHitbox(collider, hitPoint)` 로 자동 생성
- ~~EntityAnimator~~ → `new EntityAnimator(adapter, spriteRenderer)` 로 자동 생성
- ~~SpriteAnimAdapter~~ → `new SpriteAnimAdapter(animator)` 로 자동 생성

> 요약: 프리팹에는 `BattleEntity` 스크립트 + Unity 기본 컴포넌트(Animator, SpriteRenderer, Collider2D)만 추가하세요.
> 나머지는 `BattleEntity.Initialize(UnitDataSO data, EntityTeam team)` 호출 시 전부 자동으로 만들어집니다.

**필요한 애니메이션 상태** (Animator Controller에 설정):
- `Idle` — 대기
- `Attack` — 공격
- `Hit` — 피격
- `Die` — 사망

---

### STEP 4: 코드로 배틀 시작하기

```csharp
public class GameManager : MonoBehaviour
{
    [SerializeField] private StageDataSO stageData;    // 스테이지 데이터 에셋
    [SerializeField] private UnitDataSO[] playerParty; // 플레이어 파티 데이터 에셋

    void StartBattle()
    {
        var battleManager = FindObjectOfType<BattleManager>();
        battleManager.StartBattle(stageData, playerParty);
    }
}
```

어디서든 BattleManager에 접근하려면:
```csharp
// ServiceLocator를 통한 전역 접근
var battleManager = ServiceLocator.Get<BattleManager>();
```

ServiceLocator에 등록되는 서비스 (Awake에서 자동 등록):
- `BattleManager`, `SoundManager`, `EffectManager`, `BattleCameraController`

관련 파일: `Assets/Scripts/BattleSystem/Core/ServiceLocator.cs`

---

### STEP 5: 데미지 계산 이해하기

관련 파일: `Assets/Scripts/BattleSystem/Battle/DamageCalculator.cs` (static 클래스)

```
물리 공격: 데미지 = (위력/100) × ATK × (1 - DEF/(DEF+100))
마법 공격: 데미지 = (위력/100) × ATK×0.8 × (1 - DEF×0.5/(DEF×0.5+100))
회복:      회복량 = (위력/100) × ATK×0.5

크리티컬:
  발동 확률 = 공격자의 KD %
  피해 배율 = 1.5 + 공격자의 KR/100

최종 데미지: ±10% 랜덤 변동
```

호출 방법 (인스턴스 생성 불필요):
```csharp
DamageResult result = DamageCalculator.Calculate(attackerStats, defenderStats, skillData);
```

---

### STEP 6: 스킬 연출(Timeline) 만들기

SkillDataSO 에셋의 Timeline 배열에 연출 순서를 정의합니다:

```
[슬래시 스킬 예시]
1. PlayAnimation     → 공격 모션 시작
2. MoveToTarget      → 적에게 이동
3. ApplyDamage       → 데미지 적용
4. SpawnEffect       → 타격 이펙트 생성
5. PlaySound         → 효과음 재생
6. CameraShake       → 화면 흔들기
7. ReturnToPosition  → 원위치로 복귀
```

> **Parallel** 액션을 사용하면 여러 동작을 동시에 실행할 수 있습니다.

Timeline이 비어있으면 SkillExecutor가 기본 연출을 실행합니다:
공격 애니메이션 → 데미지 적용 → 피격 애니메이션

---

### STEP 7: 이벤트 시스템 활용하기

관련 폴더: `Assets/Scripts/BattleSystem/Core/EventBus/`

이벤트 채널은 ScriptableObject 기반으로, 컴포넌트 간 직접 참조 없이 통신합니다:

```csharp
// 데미지 발생 시 UI 업데이트 예시
public class MyCustomUI : MonoBehaviour
{
    // 인스펙터에서 DamageEventChannel 에셋을 드래그&드롭으로 연결
    [SerializeField] private DamageEventChannel onDamage;

    void OnEnable()
    {
        onDamage.OnEventRaised += HandleDamage;  // 이벤트 구독
    }

    void OnDisable()
    {
        onDamage.OnEventRaised -= HandleDamage;  // 이벤트 해제
    }

    void HandleDamage(DamagePayload payload)
    {
        // payload.Target   — 맞은 캐릭터
        // payload.Damage   — 데미지 양
        // payload.IsCritical — 크리티컬 여부
    }
}
```

사용 가능한 이벤트 채널:

| 이벤트 채널 | 발생 시점 | Payload 내용 |
|------------|----------|-------------|
| BattleStartEventChannel | 배틀 시작 | StageName |
| BattleEndEventChannel | 배틀 종료 | IsVictory |
| TurnChangedEventChannel | 턴 변경 | TurnNumber, CurrentEntity |
| DamageEventChannel | 데미지 발생 | Target, Damage, IsCritical, HitPosition |
| UnitDiedEventChannel | 유닛 사망 | Unit |
| PlaySFXEventChannel | 효과음 재생 | Clip, Volume |
| CameraShakeEventChannel | 화면 흔들기 | Intensity, Duration |
| SpawnEffectEventChannel | 이펙트 생성 | Prefab, Position, Duration |

---

## 자주 겪는 문제와 해결법

| 문제 | 원인 | 해결법 |
|------|------|--------|
| 배틀이 시작 안 됨 | StageDataSO 미설정 | BattleManager 인스펙터에서 StageData 연결 확인 |
| 데미지가 0 | DEF가 너무 높거나 ATK가 0 | UnitDataSO 에셋의 스탯 값 확인 |
| 스킬 사용 불가 | MP 부족 또는 쿨다운 중 | Console 로그에서 EntitySkills 관련 메시지 확인 |
| 애니메이션 없음 | 프리팹에 Animator 컴포넌트 누락 | 프리팹에 Animator + AnimatorController 추가 |
| 이벤트가 안 날아감 | EventChannel 에셋 미연결 | 인스펙터에서 [SerializeField] 이벤트 채널 슬롯 확인 |
| AI가 행동 안 함 | BattleManager 초기화 실패 | Console에서 BattleManager.Awake() 오류 확인 |
| 캐릭터가 안 나옴 | UnitDataSO의 Prefab 미설정 | UnitDataSO 에셋에서 프리팹 연결 확인 |

---

## 핵심 파일 목록

| 역할 | 파일 경로 | 유형 |
|------|----------|------|
| 배틀 전체 관리 | `Assets/Scripts/BattleSystem/Battle/BattleManager.cs` | MonoBehaviour |
| 상태 머신 | `Assets/Scripts/BattleSystem/Battle/BattleStateMachine.cs` | 일반 클래스 |
| 턴 관리 | `Assets/Scripts/BattleSystem/Battle/TurnManager.cs` | 일반 클래스 |
| 데미지 계산 | `Assets/Scripts/BattleSystem/Battle/DamageCalculator.cs` | static 클래스 |
| AI 행동 | `Assets/Scripts/BattleSystem/Battle/AIController.cs` | 일반 클래스 |
| 캐릭터 엔티티 | `Assets/Scripts/BattleSystem/Entity/BattleEntity.cs` | MonoBehaviour (프리팹) |
| 엔티티 생성 | `Assets/Scripts/BattleSystem/Entity/EntityFactory.cs` | 일반 클래스 |
| 스킬 실행 | `Assets/Scripts/BattleSystem/Skill/SkillExecutor.cs` | 일반 클래스 |
| 캐릭터 데이터 | `Assets/Scripts/BattleSystem/Data/UnitDataSO.cs` | ScriptableObject |
| 스킬 데이터 | `Assets/Scripts/BattleSystem/Data/SkillDataSO.cs` | ScriptableObject |
| 스테이지 데이터 | `Assets/Scripts/BattleSystem/Data/StageDataSO.cs` | ScriptableObject |
| 서비스 로케이터 | `Assets/Scripts/BattleSystem/Core/ServiceLocator.cs` | static 클래스 |
| 이벤트 채널 | `Assets/Scripts/BattleSystem/Core/EventBus/` | ScriptableObject |
| 배틀 씬 | `Assets/Scenes/Batttle/BattleTest.unity` | Unity 씬 |

---

## 작동 확인 체크리스트

- [ ] `BattleTest.unity` 씬 열고 Play 버튼 클릭
- [ ] Console 창에 ServiceLocator 등록 관련 오류 없음
- [ ] EntityFactory가 BattleEntity 프리팹을 스폰함
- [ ] 스킬 선택 UI(SkillSelectUI)가 나타남
- [ ] 스킬 선택 시 애니메이션과 데미지 발생
- [ ] 모든 몬스터 사망 시 BattleEndEventChannel 발생
- [ ] Unity Console 창에 오류 없음
