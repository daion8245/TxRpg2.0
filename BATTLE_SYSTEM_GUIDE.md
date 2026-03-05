# 유니티 배틀 시스템 적용 가이드 (초보자용)

> TxRpg2.0 배틀 시스템을 처음 접하는 개발자를 위한 단계별 가이드입니다.

---

## 배틀 시스템이 어떻게 동작하나?

```
[게임 시작]
    ↓
BattleManager.StartBattle(stageData, party)  ← 배틀 시작 명령
    ↓
[배틀 루프]
TurnStartPhase    → 턴 시작 (속도순 정렬, 버프 처리)
ActionSelectPhase → 행동 선택 (플레이어 입력 or AI 결정)
ExecutePhase      → 행동 실행 (스킬, 공격, 이펙트)
TurnEndPhase      → 턴 종료 (정리)
    ↓
ResultPhase       → 승리/패배 결과 화면
```

---

## 단계별 적용 가이드

### STEP 1: 데이터 만들기 (ScriptableObject)

> **ScriptableObject** = 게임 데이터를 담는 "데이터 파일"이라고 생각하세요

#### 캐릭터 데이터 (UnitDataSO)
- Unity 에디터에서 우클릭 → Create → UnitData
- 관련 파일: `Assets/Scripts/BattleSystem/Data/UnitDataSO.cs`

| 항목 | 설명 |
|------|------|
| Name | 캐릭터 이름 |
| HP / MP | 체력 / 마나 |
| ATK | 공격력 |
| DEF | 방어력 |
| SPD | 속도 (턴 순서 결정) |
| KD | 크리티컬 확률 (%) |
| KR | 크리티컬 피해 (%) |
| Prefab | 캐릭터 3D/2D 프리팹 |
| Skills | 보유 스킬 목록 |

#### 스킬 데이터 (SkillDataSO)
- 관련 파일: `Assets/Scripts/BattleSystem/Data/SkillDataSO.cs`

| 항목 | 선택지 |
|------|--------|
| Category | Physical / Magical / Healing / Buff / Debuff |
| Element | None / Fire / Ice / Lightning / Dark / Holy |
| TargetType | 단일적 / 전체적 / 단일아군 / 전체아군 / 자신 |
| Timeline | 스킬 연출 순서 정의 (이동→애니→데미지→이펙트) |

#### 스테이지 데이터 (StageDataSO)
- 관련 파일: `Assets/Scripts/BattleSystem/Data/StageDataSO.cs`
- 등장 몬스터, 플레이어 배치 슬롯, 보상(골드/아이템) 설정

---

### STEP 2: 씬 설정하기

**배틀 씬 열기**
```
Assets/Scenes/Batttle/BattleTest.unity
```

씬에 반드시 있어야 하는 오브젝트들:
```
Scene
├── BattleManager     ← 배틀 전체 관리
├── TurnManager       ← 턴 순서 관리
├── ActionQueue       ← 행동 순서 관리
├── SkillExecutor     ← 스킬 실행
├── AIController      ← 적 AI
├── SoundManager      ← 사운드
├── EffectManager     ← 시각 이펙트
├── BattleCamera      ← 카메라
└── BattleUI          ← 전투 UI (HUD, 스킬선택창)
```

---

### STEP 3: 캐릭터 프리팹 만들기

관련 파일: `Assets/Scripts/BattleSystem/Entity/BattleEntity.cs`

빈 GameObject를 만들고 아래 컴포넌트를 추가하세요:

```
[캐릭터 프리팹]
├── BattleEntity      ← 루트 컴포넌트 (필수!)
├── EntityStats       ← 스탯 관리 (HP, MP, ATK, DEF...)
├── EntityAnimator    ← 애니메이션 제어
├── EntitySkills      ← 스킬 목록 + 쿨다운
├── EntityBuffs       ← 버프/디버프 관리
└── EntityHitbox      ← 피격 판정
```

**애니메이션 연결 방법**
- 스프라이트 사용 → `SpriteAnimAdapter` 컴포넌트 추가
- Spine 사용 → `SpineAnimAdapter` 컴포넌트 추가
- 필요한 애니메이션 상태: `Idle`, `Attack`, `Hit`, `Die`
- 관련 폴더: `Assets/Scripts/BattleSystem/Animation/`

---

### STEP 4: 코드로 배틀 시작하기

```csharp
public class GameManager : MonoBehaviour
{
    [SerializeField] private StageDataSO stageData;    // 스테이지 데이터
    [SerializeField] private UnitDataSO[] playerParty; // 플레이어 파티

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
// 관련 파일: Assets/Scripts/BattleSystem/Core/ServiceLocator.cs
var battleManager = ServiceLocator.Get<BattleManager>();
```

---

### STEP 5: 데미지 계산 이해하기

관련 파일: `Assets/Scripts/BattleSystem/Battle/DamageCalculator.cs`

```
물리 공격: 데미지 = (위력/100) × ATK × (1 - DEF/(DEF+100))
마법 공격: 데미지 = (위력/100) × ATK×0.8 × (1 - DEF×0.5/(DEF×0.5+100))
회복:      회복량 = (위력/100) × ATK×0.5

크리티컬:
  발동 확률 = 공격자의 KD %
  피해 배율 = 1.5 + 공격자의 KR/100

최종 데미지: ±10% 랜덤 변동
```

---

### STEP 6: 스킬 연출(Timeline) 만들기

스킬은 "행동 목록"으로 연출을 순서대로 정의합니다:

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

---

### STEP 7: 이벤트 시스템 활용하기

관련 폴더: `Assets/Scripts/BattleSystem/Core/EventBus/`

```csharp
// 데미지 발생 시 UI 업데이트 예시
public class BattleHUD : MonoBehaviour
{
    [SerializeField] private DamageEventChannel onDamage;

    void OnEnable()
    {
        onDamage.OnEventRaised += ShowDamagePopup;
    }

    void ShowDamagePopup(DamageInfo info)
    {
        // 데미지 숫자 팝업 표시
    }
}
```

사용 가능한 이벤트 채널:
- `BattleStartEventChannel` — 배틀 시작
- `BattleEndEventChannel` — 배틀 종료 (승리/패배)
- `TurnChangedEventChannel` — 턴 변경
- `DamageEventChannel` — 데미지 발생
- `UnitDiedEventChannel` — 유닛 사망

---

## 자주 겪는 문제와 해결법

| 문제 | 원인 | 해결법 |
|------|------|--------|
| 배틀이 시작 안 됨 | StageDataSO 미설정 | BattleManager에 StageData 연결 확인 |
| 데미지가 0 | DEF가 너무 높거나 ATK가 0 | UnitDataSO 스탯 값 확인 |
| 스킬 사용 불가 | MP 부족 또는 쿨다운 중 | EntitySkills 컴포넌트 로그 확인 |
| 애니메이션 없음 | AnimAdapter 미연결 | EntityAnimator에 어댑터 컴포넌트 추가 |
| AI가 행동 안 함 | AIController 없음 | 씬에 AIController 오브젝트 확인 |

---

## 핵심 파일 목록

| 역할 | 파일 경로 |
|------|----------|
| 배틀 전체 관리 | `Assets/Scripts/BattleSystem/Battle/BattleManager.cs` |
| 상태 머신 | `Assets/Scripts/BattleSystem/Battle/BattleStateMachine.cs` |
| 턴 관리 | `Assets/Scripts/BattleSystem/Battle/TurnManager.cs` |
| 데미지 계산 | `Assets/Scripts/BattleSystem/Battle/DamageCalculator.cs` |
| 캐릭터 엔티티 | `Assets/Scripts/BattleSystem/Entity/BattleEntity.cs` |
| 스킬 실행 | `Assets/Scripts/BattleSystem/Skill/SkillExecutor.cs` |
| 캐릭터 데이터 | `Assets/Scripts/BattleSystem/Data/UnitDataSO.cs` |
| 스킬 데이터 | `Assets/Scripts/BattleSystem/Data/SkillDataSO.cs` |
| 스테이지 데이터 | `Assets/Scripts/BattleSystem/Data/StageDataSO.cs` |
| 배틀 씬 | `Assets/Scenes/Batttle/BattleTest.unity` |
| 서비스 로케이터 | `Assets/Scripts/BattleSystem/Core/ServiceLocator.cs` |
| 이벤트 버스 | `Assets/Scripts/BattleSystem/Core/EventBus/` |

---

## 작동 확인 체크리스트

- [ ] `BattleTest.unity` 씬 열고 Play 버튼 클릭
- [ ] 플레이어/몬스터가 씬에 스폰됨
- [ ] 스킬 선택 UI가 나타남
- [ ] 스킬 선택 시 애니메이션과 데미지 발생
- [ ] 모든 몬스터 사망 시 승리 화면 표시
- [ ] Unity Console 창에 오류 없음
