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

> **프리팹이란?** 미리 만들어 놓은 "캐릭터 틀"입니다.
> 게임이 실행되면 `EntityFactory`가 이 프리팹을 복사(Instantiate)해서 전투 씬에 캐릭터를 배치합니다.
> 여러분이 할 일은 **빈 게임오브젝트에 컴포넌트 4개를 붙이고 프리팹으로 저장**하는 것뿐입니다!

스크립트 위치: `Assets/Scripts/BattleSystem/Entity/BattleEntity.cs`

---

#### 3-1. 프리팹 만들기 (처음부터 따라하기)

**① 빈 GameObject 만들기**
1. Unity 상단 메뉴 → **GameObject → Create Empty**
2. 이름을 캐릭터 이름으로 변경 (예: `Warrior`, `Slime` 등)

**② SpriteRenderer 추가하기** (캐릭터 그림 표시용)
1. 방금 만든 GameObject를 클릭
2. Inspector 창 → **Add Component** 버튼 클릭
3. 검색창에 `SpriteRenderer` 입력 → 선택
4. SpriteRenderer의 `Sprite` 항목에 캐릭터 이미지(스프라이트)를 드래그&드롭
5. `Sorting Layer` / `Order in Layer`를 필요에 따라 조정 (캐릭터가 배경 위에 보이도록)

**③ Animator 추가하기** (애니메이션 재생용)
1. Inspector 창 → **Add Component** → `Animator` 검색 → 선택
2. Animator Controller 에셋을 만들어야 합니다 (아래 3-2에서 설명)
3. Animator 컴포넌트의 `Controller` 슬롯에 만든 AnimatorController 에셋을 드래그&드롭

**④ Collider2D 추가하기** (피격 판정용)
1. Inspector 창 → **Add Component** → `BoxCollider2D` 검색 → 선택
   - (캐릭터 모양에 따라 `CircleCollider2D`나 `CapsuleCollider2D`도 가능)
2. Collider의 크기(Size)와 위치(Offset)를 캐릭터 몸에 맞게 조정
3. **Is Trigger** 체크박스는 꺼둔 상태 그대로 두세요

**⑤ BattleEntity 스크립트 추가하기** (핵심!)
1. Inspector 창 → **Add Component** → `BattleEntity` 검색 → 선택
2. 이 스크립트 하나만 추가하면 됩니다!
   - EntityStats, EntitySkills, EntityBuffs 등은 **절대 직접 추가하지 마세요**
   - 게임 실행 시 `BattleEntity.Initialize()`가 전부 자동 생성합니다

**⑥ (선택) HitPoint 자식 오브젝트 만들기** (이펙트 생성 위치)
1. 만든 GameObject를 우클릭 → **Create Empty**
2. 자식 오브젝트의 이름을 정확히 **`HitPoint`** 로 변경 (대소문자 주의!)
3. HitPoint의 위치(Transform Position)를 캐릭터의 몸통 중앙 부근으로 이동
   - 이 위치에 타격 이펙트, 데미지 숫자가 표시됩니다
   - 없으면 Collider의 중심점을 자동으로 사용합니다

**⑦ 프리팹으로 저장하기**
1. Hierarchy 창의 GameObject를 **Project 창의 원하는 폴더로 드래그&드롭**
   - 권장 경로: `Assets/Prefabs/Characters/` 또는 `Assets/Prefabs/Enemies/`
2. 드래그하면 파란색 아이콘의 프리팹 파일(.prefab)이 생성됩니다
3. Hierarchy에 남아있는 원본 GameObject는 삭제해도 됩니다

---

#### 3-2. Animator Controller 만들기 (애니메이션 설정)

> Animator Controller는 "어떤 상황에서 어떤 애니메이션을 재생할지" 정해주는 설정 파일입니다.

**① AnimatorController 에셋 만들기**
1. Project 창에서 우클릭 → **Create → Animator Controller**
2. 이름을 `캐릭터이름_Controller`로 변경 (예: `Warrior_Controller`)

**② 애니메이션 클립 준비하기**
- 각 동작(대기, 공격, 피격, 사망)에 해당하는 스프라이트 시트 또는 애니메이션 클립이 필요합니다
- 아직 애니메이션이 없다면 단일 스프라이트로 임시 클립을 만들어도 됩니다

**③ 상태(State) 추가하기** — 반드시 아래 4개 상태를 만들어야 합니다

`AnimatorController`를 더블클릭하여 Animator 창을 열고:
1. Animator 창에서 우클릭 → **Create State → Empty** → 이름을 `idle`로 변경
2. 같은 방법으로 `attack`, `hit`, `die` 상태를 추가
3. 각 상태를 클릭하고 Inspector에서 **Motion** 항목에 해당 애니메이션 클립을 연결

| 상태 이름 (정확히 소문자로!) | 용도 | 비고 |
|---------------------------|------|------|
| `idle` | 대기 모션 | **기본 상태(주황색)**로 설정 - 우클릭 → Set as Layer Default State |
| `attack` | 공격 모션 | 공격 후 idle로 돌아감 |
| `hit` | 피격 모션 | 피격 후 idle로 돌아감 |
| `die` | 사망 모션 | 재생 후 멈춤 (Loop Time 꺼야 함) |

> **중요!** 상태 이름은 반드시 **소문자**(`idle`, `attack`, `hit`, `die`)로 만드세요.
> `SpriteAnimAdapter`가 이 이름으로 애니메이션을 찾습니다.

**④ Transition(전환) 설정하기**
- 각 상태에서 코드로 직접 전환하므로, **Trigger 파라미터 기반 전환**을 쓰거나
- 단순히 코드에서 `Animator.Play("상태이름")`으로 직접 재생합니다
- 복잡한 Transition 설정은 필요 없습니다

**⑤ idle 상태 루프 설정**
1. idle에 연결된 애니메이션 클립을 선택
2. Inspector에서 **Loop Time** ✅ 체크 (대기 모션은 반복 재생)
3. die 클립은 **Loop Time** ❌ 해제 (사망 모션은 한 번만 재생)

---

#### 3-3. 프리팹 완성 후 데이터 연결하기

프리팹을 만들었으면 **UnitDataSO 에셋에 연결**해야 합니다:
1. STEP 1에서 만든 UnitDataSO 에셋을 클릭
2. Inspector에서 **Prefab** 슬롯에 방금 만든 프리팹을 드래그&드롭
3. Skills 배열에 사용할 SkillDataSO 에셋들을 연결

```
[UnitDataSO 에셋 - Inspector 예시]
┌──────────────────────────────────────┐
│ Unit Name:    전사                     │
│ Portrait:     [전사 초상화 스프라이트]    │
│ Prefab:       [Warrior.prefab] ◀── 여기!│
│                                        │
│ Base Hp:      500                      │
│ Base Mp:      100                      │
│ Base Atk:     80                       │
│ Base Def:     50                       │
│ Base Spd:     30                       │
│ Base Kd:      15    (크리티컬 확률 %)    │
│ Base Kr:      50    (크리티컬 피해 %)    │
│                                        │
│ Skills:       [3개]                     │
│   [0] SlashSkill                       │
│   [1] PowerStrike                      │
│   [2] DefenseUp                        │
│                                        │
│ Team:         Player                   │
└──────────────────────────────────────┘
```

---

#### 3-4. 프리팹이 게임에서 생성되는 과정 (이해용)

> 이 과정은 **전부 자동**이므로 여러분이 코드를 수정할 필요는 없습니다.
> 하지만 문제가 생겼을 때 어디를 확인해야 하는지 알기 위해 흐름을 이해해두세요.

```
[배틀 시작 - 자동 실행 흐름]

1. BattleManager.StartBattle(stageData, playerParty) 호출
       ↓
2. EntityFactory.CreateEntity(unitData, team, position) 실행
       ↓
3. unitData.prefab을 Instantiate (프리팹 복사)
   └→ 이때 SpriteRenderer, Animator, Collider2D가 함께 복사됨
       ↓
4. BattleEntity.Initialize(unitData, team) 자동 호출
   ├→ EntityStats 생성    : HP=500, MP=100, ATK=80 ... (UnitDataSO 값 복사)
   ├→ EntitySkills 생성   : 스킬 3개를 SkillRuntime으로 래핑 (쿨다운 추적용)
   ├→ EntityBuffs 생성    : 빈 버프 목록으로 초기화
   ├→ EntityHitbox 생성   : Collider2D + HitPoint 참조 저장
   ├→ SpriteAnimAdapter 생성 : Animator 컴포넌트 래핑
   └→ EntityAnimator 생성 : 애니메이션 상태머신 초기화
       ↓
5. 캐릭터가 idle 애니메이션을 재생하며 전투 위치에 배치됨
   └→ 적(Enemy)이면 자동으로 좌우 반전 (왼쪽을 바라봄)
```

---

#### 3-5. 프리팹 최종 구조 요약

```
[캐릭터 프리팹 - 최종 구조]
WarriorPrefab (GameObject)
│
├── [컴포넌트] BattleEntity        ← 커스텀 스크립트 (1개만!)
├── [컴포넌트] SpriteRenderer      ← 캐릭터 스프라이트 표시
├── [컴포넌트] Animator            ← Controller에 idle/attack/hit/die 설정
├── [컴포넌트] BoxCollider2D       ← 캐릭터 몸 크기에 맞게 조정
│
└── HitPoint (자식 GameObject)     ← (선택) 빈 오브젝트, 몸통 중앙에 배치
    └── Transform만 있으면 됨
```

**프리팹에 추가하면 안 되는 것들** (BattleEntity.Initialize()가 코드에서 자동 생성):
- ~~EntityStats~~ → `new EntityStats(unitData)` 로 자동 생성
- ~~EntitySkills~~ → `new EntitySkills(skillArray)` 로 자동 생성
- ~~EntityBuffs~~ → `new EntityBuffs(stats)` 로 자동 생성
- ~~EntityHitbox~~ → `new EntityHitbox(collider, hitPoint)` 로 자동 생성
- ~~EntityAnimator~~ → `new EntityAnimator(adapter, spriteRenderer)` 로 자동 생성
- ~~SpriteAnimAdapter~~ → `new SpriteAnimAdapter(animator)` 로 자동 생성

---

#### 3-6. 문제 해결 체크리스트

프리팹 관련 문제가 생기면 아래를 확인하세요:

| 증상 | 확인할 것 |
|------|----------|
| 캐릭터가 안 보임 | SpriteRenderer에 Sprite가 연결되어 있는지 확인 |
| 캐릭터가 투명함 | SpriteRenderer의 Color 알파값이 255인지 확인 |
| 애니메이션이 안 됨 | Animator에 Controller가 연결되어 있는지 확인 |
| idle만 안 됨 | idle 상태가 Default State(주황색)로 설정되어 있는지 확인 |
| 사망 후 다시 움직임 | die 애니메이션 클립의 Loop Time이 꺼져 있는지 확인 |
| 이펙트 위치가 이상함 | HitPoint 자식 오브젝트의 위치(Transform)가 몸통 중앙인지 확인 |
| 피격 판정이 안 됨 | Collider2D가 캐릭터 크기에 맞는지, 너무 작지 않은지 확인 |
| 캐릭터가 안 생성됨 | UnitDataSO의 Prefab 슬롯에 프리팹이 연결되어 있는지 확인 |
| "idle" 상태를 못 찾음 | Animator Controller의 상태 이름이 정확히 소문자(`idle`)인지 확인 |

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
