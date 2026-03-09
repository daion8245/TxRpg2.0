# TxRpg2.0 배틀 시스템 - 구현 가능 기능 분석

> 현재 `Assets/Scripts/BattleSystem/` 코드베이스 아키텍처를 기반으로
> 구현 가능한 기능과 불가능한 기능을 정리한 문서입니다.

---

## ✅ 구현 가능한 기능

---

### 1. 배틀 이펙트 구현

> `EffectDataSO`, `EffectManager`, `IPoolable`, `SpawnEffectAction` 기반

- **원소별 이펙트 차별화**
  `EffectDataSO.prefab`에 원소(Fire/Ice/Lightning/Dark/Holy)마다 다른 VFX 프리팹을 지정하고,
  `SpawnEffectAction`에서 `SkillDataSO.element` 값을 읽어 해당 이펙트를 선택 소환.
  데이터만 교체하면 코드 수정 없이 원소별 시각화 가능.

- **히트 임팩트 이펙트 (타격감)**
  `ApplyDamageAction` 직후 `SpawnEffectAction`을 타임라인에 배치하면,
  데미지 순간 `EntityHitbox.Center` 좌표에 충격파·스파크 이펙트 소환 가능.
  `EffectDataSO.attachToTarget = true`로 타겟 엔티티에 부착하여 따라다니게 설정 가능.

- **상태이상 지속 이펙트 (DoT 시각화)**
  `EntityBuffs.AddBuff()` 호출 시 `EffectManager.SpawnEffect()`를 함께 호출해
  독·화상·출혈 파티클을 엔티티 위에 붙이고, `TickBuffs()`로 버프 만료 시 함께 제거.

- **치명타 전용 이펙트**
  `DamageCalculator.Calculate()`가 반환하는 `DamageResult.IsCritical == true`일 때
  별도 크리티컬 이펙트 채널을 발행하면, `EffectManager`가 금색 폭발 등 전용 VFX 추가 소환 가능.

- **오브젝트 풀링 기반 이펙트 재사용**
  `BattleEffect`가 이미 `IPoolable` 인터페이스를 구현하므로,
  `EffectManager`에 풀 딕셔너리를 추가해 동일 이펙트 반복 소환 시 생성·파괴 비용 없이 재활용 가능.

---

### 2. 애니메이션 구현

> `EntityAnimator`, `IAnimationAdapter`, `SpriteAnimAdapter`, `AnimStateMachine`, `TweenHelper` 기반

- **커스텀 스킬 애니메이션**
  `SkillActionData.animationName`에 임의의 문자열을 설정하면
  `PlayAnimAction`이 `EntityAnimator.PlayAnimationAsync(name)`을 호출하여
  기본 5개 상태(Idle/Attack/Hit/Die/Move) 외의 커스텀 애니메이션 재생 가능.
  Unity Animator에 해당 State만 추가하면 적용됨.

- **Spine 스켈레탈 애니메이션 연동**
  `SpineAnimAdapter`가 이미 `IAnimationAdapter` 인터페이스로 선언되어 있어,
  Spine SDK의 `SkeletonAnimation.state.SetAnimation()`을 내부에 구현하면
  뼈대 기반 부드러운 애니메이션으로 즉시 전환 가능. 백엔드 교체만으로 완성.

- **히트 플래시 / 피격 색상 연출**
  `EntityAnimator.FlashAsync(Color, float)`가 `TweenHelper.ColorFlashAsync()`를 래핑하므로,
  피격 시 흰색 플래시, 독 데미지 시 녹색 플래시 등 데미지 유형별 색상을
  `ApplyDamageAction`에서 분기하여 호출 가능.

- **입장·등장 애니메이션**
  `BattleIntroPhase`에서 엔티티 생성 후 `PlayAnimationAsync("enter")`를 호출하고,
  `TweenHelper.MoveToAsync()`를 조합해 슬라이드 인 등의 입장 연출 구현 가능.

- **스케일 펀치 (강조 애니메이션)**
  `TweenHelper.ScalePunchAsync(Transform, Vector3, float)`가 이미 구현되어 있어,
  치명타나 버프 적용 시 엔티티 크기를 일시적으로 키우는 강조 연출을
  타임라인 액션(`WaitAction` 앞뒤)에 추가 가능.

- **입장 순서 연출 (스태거 등장)**
  `EntityFactory.CreatePlayerParty()`가 순차적으로 엔티티를 생성하므로,
  각 엔티티 생성 후 `WaitAction`을 삽입해 순서대로 입장하는 연출 가능.

---

### 3. 오브제(무기·장비) 부착 및 별도 애니메이션

> `BattleEntity` 컴포넌트 구조, `EntityAnimator`, `IAnimationAdapter`, `EntityHitbox` 기반

- **무기 오브젝트 자식 GameObject 부착**
  `BattleEntity`는 컴포넌트 조합 구조이므로, 칼·활·지팡이 등 무기 프리팹을 자식 GameObject로 붙이고
  해당 `SpriteRenderer`를 관리하는 `EntityEquipment` 컴포넌트를 추가하면 장비 시각화 가능.
  `UnitDataSO`에 `weaponPrefab` 필드를 추가하면 데이터 기반 교체도 가능.

- **무기 별도 Animator 제어**
  자식 무기 GameObject에 독립 `Animator`를 부착하고,
  `EntityAnimator.PlayAttackAsync()` 호출 시 캐릭터 Animator와 무기 Animator를 동시에 제어하면
  칼 휘두르기, 활시위 당기기 등 무기별 전용 애니메이션 구현 가능.
  `ParallelAction`으로 타임라인에서 동시 재생도 지원됨.

- **Spine 멀티 레이어를 통한 무기 레이어링**
  `SpineAnimAdapter`에 Spine의 스킨/슬롯 전환 기능을 구현하면,
  동일 스켈레톤 내에서 무기 부위만 독립 애니메이션 레이어로 제어 가능.
  무기 교체도 스킨 스왑 한 줄로 처리 가능.

- **무기 끝 이펙트 소환 (WeaponTip 포인트)**
  `EntityHitbox`에 무기 끝 위치를 나타내는 `WeaponTip` Transform 필드를 추가하면,
  `SpawnEffectAction`이 무기 끝에서 정확히 이펙트(칼날 궤적, 발사체 등)를 소환 가능.
  `EffectDataSO.offset`과 조합하면 정밀한 위치 보정도 가능.

- **장비 상태 변화 시각화 (파괴·강화)**
  무기 SpriteRenderer의 스프라이트를 교체하거나 색상 플래시를 주면
  장비 내구도 감소, 강화 성공 연출을 전투 중에도 표현 가능.

---

### 4. 스킬 시스템 확장

> `SkillDataSO`, `TimelineDirector`, `SkillActionData`, `ActionFactory` 기반

- **다단 히트 스킬 (콤보)**
  타임라인에 `ApplyDamageAction`을 `damageMultiplierPercent`를 낮게(예: 30%) 설정하여
  여러 번 배치하면 3히트·5히트 콤보 스킬 구현 가능.
  각 히트 사이에 `WaitAction`으로 타이밍 조절.

- **충전형 스킬 (차징)**
  `WaitAction`과 충전 애니메이션을 타임라인 앞에 배치하고
  이후 `ApplyDamageAction` + `SpawnEffectAction`을 연결하면
  차징 → 폭발 순서의 강력한 스킬 연출 가능.

- **전체/단일/자신 타겟 자동 분기**
  `SkillDataSO.targetType`(SingleEnemy/AllEnemies/SingleAlly/AllAllies/Self)에 따라
  `ActionSelectPhase`와 `AIController.SelectTargets()`가 자동으로 대상을 결정하므로,
  데이터 변경만으로 범위기·단체기·자힐 스킬 전환 가능.

- **원소 속성 상성 시스템**
  `SkillDataSO.element`에 이미 6가지 원소가 정의되어 있으므로,
  `DamageCalculator`에 속성 배율 테이블(예: 불 → 얼음 1.5배)을 추가하면
  속성 상성 데미지 배율 시스템 구현 가능.

- **패시브·자동 발동 스킬**
  `TurnStartPhase`에서 특정 조건(HP 50% 이하 등)을 검사해
  `SkillExecutor.ExecuteSkill()`을 자동 호출하면 패시브 스킬 구현 가능.
  `SkillDataSO`에 `isPassive` 플래그 추가로 관리.

- **병렬 스킬 연출 (ParallelAction 활용)**
  `SkillActionType.Parallel`이 이미 구현되어 있어,
  이동·애니메이션·이펙트·카메라 흔들기를 동시에 실행하는
  화려한 동시다발 연출을 타임라인에서 즉시 구성 가능.

---

### 5. 버프 / 디버프 시스템 확장

> `EntityBuffs`, `BuffData`, `EntityStats` 기반

- **복합 버프 스택**
  `EntityBuffs.AddBuff()`를 여러 번 호출하면 다수의 버프가 독립적으로 쌓이며,
  `EntityStats`의 수정자가 누적 합산되므로,
  `BuffName` 기반 카운트로 최대 스택 제한 시스템 구현 가능.

- **다양한 상태이상 (마비·침묵·도발)**
  `BuffData`에 `isStunned`, `isSilenced`, `isTaunted` 플래그를 추가하고,
  `ActionSelectPhase`에서 해당 플래그를 검사해 행동 제한하면
  마비(행동불능)·침묵(스킬 사용 불가)·도발(특정 대상만 공격) 구현 가능.

- **지속 회복 / 재생 효과 (HoT)**
  `BuffData.HpPerTurn > 0`이 이미 구현되어 있어,
  회복 스킬 타임라인에 버프 적용 액션을 추가하는 것만으로
  매 턴 HP가 회복되는 재생 효과 즉시 사용 가능.

- **방어 자세 (방어력 버프)**
  `ActionType.Defend`가 열거형에 정의되어 있으며,
  `EntityStats.DefModifier`에 큰 수치의 버프를 1턴 적용하면
  방어 자세(받는 데미지 크게 감소) 구현 가능.

- **버프 전이 및 해제 스킬**
  `EntityBuffs.ClearAll()`로 모든 버프를 제거하거나,
  특정 `BuffName`을 가진 버프만 제거하는 메서드를 추가해
  정화(클렌즈)·디스펠 스킬 구현 가능.

---

### 6. 카메라 연출

> `BattleCameraController`, `CameraShaker`, `CameraShakeAction`, `CameraZoomAction` 기반

- **스킬 강도별 카메라 흔들림**
  `CameraShakeAction.shakeIntensity` 값을 스킬별로 다르게 설정해
  약한 평타는 약하게, 필살기는 강하게 흔들리도록
  타임라인 데이터만 수정하면 즉시 적용 가능.

- **타겟 포커스 줌**
  `CameraZoomAction`이 `BattleCameraController.ZoomToAsync(position, size, duration)`을 호출하므로,
  보스 등장이나 필살기 연출 시 대상을 화면 중앙에 확대하는 연출 가능.

- **히트 순간 클로즈업 → 자동 복귀**
  타임라인에 `CameraZoomAction`(줌 인) → `ApplyDamageAction` → `CameraZoomAction`(복귀)을 배치하면
  데미지 순간만 클로즈업되는 드라마틱한 연출 가능.
  `BattleCameraController.ResetAsync()`로 자연스럽게 원위치 복귀.

- **보스 등장 전용 카메라 시퀀스**
  `BattleIntroPhase`에서 카메라를 보스 위치로 빠르게 이동 → 홀드 → 복귀하는
  비동기 시퀀스를 `UniTask`로 구성하면 보스 전투 시작 연출 가능.

- **Perlin 노이즈 기반 자연스러운 흔들림**
  `CameraShaker`가 이미 Perlin 노이즈와 감쇠 곡선으로 구현되어 있어,
  반복 패턴 없는 자연스러운 카메라 진동이 즉시 가능.

---

### 7. UI / HUD 구현

> `SkillSelectUI`, `DamageEventChannel`, `EventChannel` 시스템 기반

- **데미지 팝업 텍스트**
  `DamageEventChannel`이 이미 데미지 이벤트를 발행하므로,
  이 채널을 구독하는 `DamagePopupUI`를 만들어 `EntityHitbox.Top` 위치에 숫자를 팝업.
  `TweenHelper.MoveToAsync()` + `FadeAsync()`로 떠오르며 사라지는 애니메이션 추가 가능.

- **HP / MP 게이지 바**
  `EntityStats.CurrentHp / MaxHp` 비율을 `Image.fillAmount`에 바인딩하는
  `EntityHpBar` 컴포넌트를 추가하면 됨.
  `DamageEventChannel` 구독으로 실시간 갱신.

- **버프·디버프 아이콘 표시**
  `EntityBuffs`의 버프 목록 변경 시 이벤트를 발행하고 UI에서 구독해
  `BuffData.BuffName`에 해당하는 아이콘을 나열하면 됨.
  남은 턴 수도 아이콘 위에 숫자로 표시 가능.

- **턴 순서 표시 UI**
  `TurnManager.GetActionOrder()`가 반환하는 엔티티 리스트를
  `TurnChangedEventChannel` 구독 시 받아 순서대로 초상화를 나열하면
  ATB 스타일의 턴 순서 UI 구현 가능.

- **스킬 쿨다운 / MP 비용 표시**
  `SkillSelectUI`에서 이미 사용 불가 버튼을 비활성화하고 있으므로,
  버튼에 `SkillRuntime.CooldownRemaining` 텍스트와 `SkillDataSO.mpCost`를 표시하는
  레이블만 추가하면 쿨다운·MP 정보 UI 완성.

- **전투 결과 보상 화면**
  `ResultPhase`에서 `BattleEndEventChannel`을 발행하고,
  `StageDataSO.goldReward`, `expReward`를 읽어 애니메이션으로 보상을 표시하는
  결과 화면 UI 구현 가능.

---

### 8. AI 전투 지능 강화

> `AIController`, `EntityStats`, `EntityBuffs`, `EntitySkills` 기반

- **우선순위 기반 스킬 선택**
  현재 랜덤 선택을 조건 점수 시스템으로 대체:
  HP 30% 이하 → 회복기 우선, 버프 없으면 버프기 우선, 다수 적 → 범위기 우선.
  상황에 맞는 스킬을 선택하는 전술 AI 구현 가능.

- **어그로(타겟팅) 로직**
  `BattleEntity`에 `Aggro` 수치 필드를 추가하고,
  데미지를 줄 때마다 증가시켜 `AIController.SelectTargets()`에서
  가장 어그로가 높은 유닛을 우선 공격하는 탱커 역할 구현 가능.

- **페이즈 전환 보스 AI**
  보스 `CurrentHp`를 감시하다가 특정 임계값 이하가 되면
  `AIController`의 스킬 풀을 교체하거나 배율을 증가시켜
  HP 페이즈 전환 보스 행동 패턴 구현 가능.

- **협동 AI (적 간 시너지)**
  `AIController.DecideAction()`에서 아군 상태를 확인해
  아군에 디버프가 있으면 해제기를, 아군 HP가 낮으면 회복기를 우선 선택하는
  팀플레이 AI 구현 가능.

---

### 9. 데이터 드리븐 콘텐츠 제작

> `ScriptableObject` 시스템 (UnitDataSO, SkillDataSO, EffectDataSO, StageDataSO) 기반

- **새 캐릭터 / 적 추가**
  `UnitDataSO`를 새로 생성하고 스탯과 스킬 배열만 채우면
  코드 수정 없이 새 유닛 추가 가능. `EntityFactory`가 자동으로 처리.

- **새 스킬 추가**
  `SkillDataSO`를 생성하고 타임라인 배열에 `SkillActionData`를 나열하면
  완전히 새로운 스킬 구성 가능. 코드 추가 없이 에디터만으로 작업 완료.

- **스테이지(맵) 추가**
  `StageDataSO`에 배경·BGM·적 스폰 정보만 설정하면
  새 전투 스테이지 즉시 추가 가능.

- **레벨 기반 스탯 스케일링**
  `StageDataSO.EnemySpawnData.level` 필드가 이미 존재하므로,
  `EntityFactory`에서 레벨에 따라 스탯에 배율을 곱하는 로직을 추가하면
  레벨 기반 적 강도 조절 시스템 구현 가능.

---

### 10. 전투 규칙 및 메커니즘 확장

> `BattleStateMachine`, `TurnManager`, `ActionQueue`, `DamageCalculator` 기반

- **도주 시스템**
  `ActionType.Flee`가 이미 열거형에 존재하므로,
  `ExecutePhase`에서 Flee 액션을 처리해 성공 확률 계산 후 `BattleResult.Flee`로 전환하는
  로직만 추가하면 도주 구현 완료.

- **선제공격 / 기습 시스템**
  `BattleIntroPhase`에서 플레이어 SPD 합계와 적 SPD 합계를 비교해
  선제공격 여부를 결정하고, 첫 턴 `ActionQueue`에 플레이어 액션을 자동으로 채워 넣으면
  기습 / 선제공격 시스템 구현 가능.

- **속도 조작 전술 (행동 순서 역전)**
  `TurnManager.GetActionOrder()`는 `EffectiveSpd`로 정렬하므로,
  속도 버프/디버프가 즉시 턴 순서에 반영됨.
  이를 활용한 속도 조작 스킬로 전술적 깊이 추가 가능.

- **연속 행동 (더블 어택)**
  특정 조건 충족 시 `ActionQueue`에 동일 엔티티의 액션을 두 번 삽입하면
  같은 턴에 두 번 행동하는 연속 행동 시스템 구현 가능.

- **방어 / 막기 메커니즘**
  `ActionType.Defend` 선택 시 `EntityStats.DefModifier`를 크게 올리고
  다음 턴 `TurnStartPhase`에서 제거하는 방어 자세 사이클 구현 가능.

---

### 11. 사운드 시스템

> `SoundManager`, `PlaySoundAction`, `SoundEvents` 이벤트 채널 기반

- **스킬별 효과음**
  `SkillActionData.soundClip`에 AudioClip을 할당하면
  타임라인 실행 중 `PlaySoundAction`이 `SoundManager`를 통해 자동 재생.
  다단 히트 스킬의 각 히트마다 다른 사운드도 가능.

- **BGM 전투 / 보스 전환**
  `BattleStartEventChannel` 구독 시 `StageDataSO.bgm`을 재생하고,
  보스 HP 임계값 이벤트 발생 시 별도 BGM으로 크로스페이드하는
  `BattleBGMController` 구현 가능.

- **3D 위치 기반 사운드**
  `PlaySoundAction`이 `AudioSource.PlayClipAtPoint(clip, position)`을 사용하므로,
  효과음이 화면 내 위치에서 재생되어 스테레오 공간감 제공.

- **피격·사망 리액션 사운드**
  `UnitDiedEventChannel`, `DamageEventChannel`을 구독해
  사망·피격 전용 사운드를 자동 재생하는 컴포넌트 추가 가능.

---

### 12. 이벤트 / 트리거 시스템

> `EventChannel`, 각종 이벤트 채널 기반

- **전투 중 이벤트 컷씬 트리거**
  `UnitDiedEventChannel`, `TurnChangedEventChannel` 등을 구독해
  특정 조건(보스 사망, N턴 경과) 시 `BattleStateMachine`을 일시 정지하고
  컷씬 시퀀스를 삽입하는 연출 트리거 구현 가능.

- **업적 / 미션 추적**
  모든 전투 이벤트가 채널로 발행되므로, 채널 구독만으로
  "X턴 이내 클리어", "HP 풀로 클리어" 등 미션 달성 여부를 추적하는
  `MissionTracker` 컴포넌트 구현 가능.

- **전투 로그 기록**
  `ActionQueue`에 쌓이는 `BattleAction`(Caster, Targets, Skill, Type) 데이터를
  직렬화해 저장하면 전투 로그 및 리플레이 시스템 구현 기반 마련 가능.

---

## ❌ 현재 시스템으로 구현 불가능한 기능

---

### 불가 1. 실시간(Real-time) 전투

**이유:**
현재 시스템 전체가 **턴 기반(Turn-based)** 설계임.
`BattleStateMachine`은 Phase 단위로 동작하고, `ActionQueue`는 FIFO 순차 처리 구조임.
실시간 입력 처리, 물리 기반 충돌 판정, 프레임 단위 행동 캔슬 등의 구조가 전혀 없음.
구현 시 코어 아키텍처 전면 재설계 필요.

---

### 불가 2. 온라인 멀티플레이어 / 네트워크 동기화

**이유:**
네트워크 레이어(Photon, Mirror, Netcode 등)가 전혀 없음.
`ServiceLocator`, `BattleManager`, `ActionQueue`가 모두 단일 클라이언트 싱글톤 구조임.
원격 플레이어 입력을 `ActionQueue`에 주입하거나
게임 상태를 직렬화·동기화하는 구조가 없어 멀티플레이어 구현 불가.
완전히 새로운 네트워크 동기화 레이어 필요.

---

### 불가 3. 물리 기반 지형 전투 (Terrain Interaction)

**이유:**
전투 공간이 `EntityHitbox` 좌표와 `StageDataSO.playerSlots` 배치 위치 데이터로만 구성됨.
지형 충돌, 높낮이 차이, 장애물 회피, 넉백(지형 밖으로 밀려남) 등
물리 기반 상호작용을 위한 콜라이더 시스템·NavMesh·지형 데이터가 없음.
이동은 `TweenHelper.MoveToAsync()`의 단순 선형 보간만 지원.

---

### 불가 4. 3D 렌더링 및 입체 카메라 연출

**이유:**
모든 시각 요소가 2D 스프라이트 기반(`SpriteRenderer`, `SpriteAnimAdapter`)으로 설계됨.
`BattleCameraController`는 2D 직교(Orthographic) 카메라를 전제로
Ortho Size와 2D 위치만 제어함.
3D 메시, 원근(Perspective) 카메라, 3D 셰이더, 깊이 기반 연출은
렌더링 파이프라인과 카메라 시스템을 전면 교체하지 않는 한 구현 불가.

---

*이 문서는 `Assets/Scripts/BattleSystem/` 코드베이스 전체 분석을 기반으로 작성되었습니다.*
