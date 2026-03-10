# Battle System Event Scripts Reference

> Technical reference for the TxRpg2.0 battle system's event channels, skill timeline actions, and battle phase lifecycle.
> For setup instructions, see [BATTLE_SYSTEM_GUIDE.md](../BATTLE_SYSTEM_GUIDE.md).

---

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Battle Phases](#battle-phases)
3. [Event Channels](#event-channels)
4. [Skill Timeline Actions](#skill-timeline-actions)
5. [SkillActionData Fields](#skillactiondata-fields)
6. [Skill Timeline Examples](#skill-timeline-examples)
7. [Extending the System](#extending-the-system)

---

## Architecture Overview

The battle system uses three interconnected scripting mechanisms:

```
┌─────────────────────────────────────────────────────┐
│                  BattleStateMachine                  │
│  (controls battle flow through sequential phases)   │
│                                                     │
│  BattleIntro → TurnStart → ActionSelect → Execute   │
│                    ↑          → TurnEnd ──┘          │
│                    │              │                   │
│                    └──────────────┘                   │
│                         or                           │
│                    ResultPhase (battle over)          │
├─────────────────────────────────────────────────────┤
│                  Event Channels                      │
│  (ScriptableObject-based pub/sub messaging)          │
│  Battle, Camera, Sound, Effect events                │
├─────────────────────────────────────────────────────┤
│                  Skill Timelines                     │
│  (sequential/parallel action sequences per skill)    │
│  Defined in SkillDataSO.timeline[]                  │
└─────────────────────────────────────────────────────┘
```

**Key source paths:**

| Component | Path |
|-----------|------|
| Battle phases | `Assets/Scripts/BattleSystem/Battle/Phases/` |
| Event channels | `Assets/Scripts/BattleSystem/Core/EventBus/Events/` |
| Event base class | `Assets/Scripts/BattleSystem/Core/EventBus/EventChannel_T.cs` |
| Skill actions | `Assets/Scripts/BattleSystem/Skill/Actions/` |
| Skill executor | `Assets/Scripts/BattleSystem/Skill/SkillExecutor.cs` |
| Timeline director | `Assets/Scripts/BattleSystem/Skill/TimelineDirector.cs` |
| Action factory | `Assets/Scripts/BattleSystem/Skill/ActionFactory.cs` |
| Enums | `Assets/Scripts/BattleSystem/Data/BattleEnums.cs` |
| Skill data | `Assets/Scripts/BattleSystem/Data/SkillDataSO.cs` |

---

## Battle Phases

The `BattleStateMachine` drives the battle through six sequential phases. Each phase implements the `IState` interface with `Enter()`, `Execute()`, and `Exit()` methods using async UniTask.

### Phase Lifecycle

```
BattleIntroPhase
  │  • SpawnEntities() — creates all BattleEntity instances via EntityFactory
  │  • RaiseBattleStart() — fires BattleStartEventChannel
  │  • 1000ms intro delay
  ▼
TurnStartPhase  ◄─────────────────────────────────┐
  │  • AdvanceTurn() — increments turn counter      │
  │  • TickBuffs() — decrement buff durations       │
  │  • TickCooldowns() — decrement skill cooldowns  │
  │  • GetActionOrder() — sort entities by SPD      │
  │  • RaiseTurnChanged() — fires TurnChanged event │
  │  • 300ms delay                                  │
  ▼                                                 │
ActionSelectPhase                                   │
  │  • Clears ActionQueue                           │
  │  • For each entity in speed order:              │
  │    - Player: await SkillSelectUI input          │
  │    - Enemy: AIController.DecideAction()         │
  │  • Enqueues BattleAction per entity             │
  ▼                                                 │
ExecutePhase                                        │
  │  • Dequeues actions one by one                  │
  │  • Skips dead casters, filters dead targets     │
  │  • Consumes MP for skills                       │
  │  • SkillExecutor.ExecuteSkill() plays timeline  │
  │  • Processes deaths (PlayDeathAsync)            │
  │  • CheckBattleEnd() after each action           │
  │  • 200ms delay between actions                  │
  ▼                                                 │
TurnEndPhase                                        │
  │  • Applies DoT/HoT from buffs (HpPerTurn)      │
  │  • Processes deaths from DoT                    │
  │  • CheckBattleEnd()                             │
  │  • 300ms delay                                  │
  ▼                                                 │
  ├── (battle not over) ────────────────────────────┘
  │
  ▼
ResultPhase
   • Logs result (Victory/Defeat/Flee)
   • Logs rewards (Gold, EXP) on victory
   • RaiseBattleEnd() — fires BattleEndEventChannel
   • 2000ms delay for result UI
```

### Phase Source Files

| Phase | File | Key Methods Called |
|-------|------|--------------------|
| `BattleIntroPhase` | `Battle/Phases/BattleIntroPhase.cs` | `SpawnEntities()`, `RaiseBattleStart()` |
| `TurnStartPhase` | `Battle/Phases/TurnStartPhase.cs` | `AdvanceTurn()`, `TickBuffs()`, `TickCooldowns()`, `GetActionOrder()`, `RaiseTurnChanged()` |
| `ActionSelectPhase` | `Battle/Phases/ActionSelectPhase.cs` | `WaitForPlayerInput()`, `AIController.DecideAction()` |
| `ExecutePhase` | `Battle/Phases/ExecutePhase.cs` | `SkillExecutor.ExecuteSkill()`, `TakeDamage()`, `PlayDeathAsync()`, `CheckBattleEnd()` |
| `TurnEndPhase` | `Battle/Phases/TurnEndPhase.cs` | Buff HpPerTurn processing, `PlayDeathAsync()`, `CheckBattleEnd()` |
| `ResultPhase` | `Battle/Phases/ResultPhase.cs` | `RaiseBattleEnd()` |

---

## Event Channels

Event channels are `ScriptableObject`-based pub/sub messaging. Each channel is a concrete subclass of `EventChannel<T>` that carries a typed payload struct.

### How Event Channels Work

```csharp
// EventChannel<T> base class (Core/EventBus/EventChannel_T.cs)
public abstract class EventChannel<T> : ScriptableObject
{
    public void Register(Action<T> listener);   // subscribe
    public void Unregister(Action<T> listener); // unsubscribe
    public void Raise(T payload);               // broadcast to all listeners
}
```

**Usage pattern:**

```csharp
// Subscribing to an event
[SerializeField] private DamageEventChannel onDamage;

void OnEnable()  => onDamage.Register(HandleDamage);
void OnDisable() => onDamage.Unregister(HandleDamage);

void HandleDamage(DamagePayload payload)
{
    // React to damage event
}
```

### Battle Events

Source: `Core/EventBus/Events/BattleEvents.cs`

| Channel Class | Payload Struct | When Raised | Fields |
|---------------|---------------|-------------|--------|
| `BattleStartEventChannel` | `BattleStartPayload` | `BattleIntroPhase.Enter()` — after entities are spawned | `string StageName` |
| `BattleEndEventChannel` | `BattleEndPayload` | `ResultPhase.Enter()` — battle conclusion | `bool IsVictory` |
| `TurnChangedEventChannel` | `TurnChangedPayload` | `TurnStartPhase.Enter()` — each new turn | `int TurnNumber`, `GameObject CurrentEntity` |
| `UnitDiedEventChannel` | `UnitDiedPayload` | When a unit's HP reaches 0 | `GameObject Unit` |
| `DamageEventChannel` | `DamagePayload` | When damage is dealt to a target | `GameObject Target`, `int Damage`, `bool IsCritical`, `Vector2 HitPosition` |

### Camera Events

Source: `Core/EventBus/Events/CameraEvents.cs`

| Channel Class | Payload Struct | Purpose | Fields |
|---------------|---------------|---------|--------|
| `CameraShakeEventChannel` | `CameraShakePayload` | Screen shake effect | `float Intensity`, `float Duration` |
| `CameraZoomEventChannel` | `CameraZoomPayload` | Camera zoom to position | `Vector3 TargetPosition`, `float OrthoSize`, `float Duration` |
| `CameraPanEventChannel` | `CameraPanPayload` | Camera pan to position | `Vector3 TargetPosition`, `float Duration` |

### Sound Events

Source: `Core/EventBus/Events/SoundEvents.cs`

| Channel Class | Payload Struct | Purpose | Fields |
|---------------|---------------|---------|--------|
| `PlaySFXEventChannel` | `PlaySFXPayload` | Play one-shot sound effect | `AudioClip Clip`, `float Volume` |
| `PlayBGMEventChannel` | `PlayBGMPayload` | Play background music | `AudioClip Clip`, `float Volume`, `bool Loop` |
| `StopBGMEventChannel` | `StopBGMPayload` | Stop background music | `float FadeDuration` |

### Effect Events

Source: `Core/EventBus/Events/EffectEvents.cs`

| Channel Class | Payload Struct | Purpose | Fields |
|---------------|---------------|---------|--------|
| `SpawnEffectEventChannel` | `SpawnEffectPayload` | Spawn a visual effect prefab | `GameObject Prefab`, `Vector3 Position`, `Quaternion Rotation`, `float Duration`, `Transform Parent` |

### Creating Event Channel Assets

In the Unity Editor: Project window > Right-click > Create > TxRpg > Events > select the desired channel type.

---

## Skill Timeline Actions

Skills are scripted through the `SkillDataSO.timeline[]` array — an ordered sequence of `SkillActionData` entries. The `TimelineDirector` executes each entry sequentially by passing it through `ActionFactory.Create()` which returns an `ITimelineAction` implementation.

### Execution Flow

```
SkillExecutor.ExecuteSkill(caster, targets, skillData)
  │
  ├─ Creates SkillContext { Caster, Targets, SkillData }
  │
  ├─ Has timeline? ──Yes──▶ TimelineDirector.PlayTimeline(timeline, context)
  │                           │
  │                           ├─ For each SkillActionData in timeline[]:
  │                           │    ActionFactory.Create(data) → ITimelineAction
  │                           │    await action.Execute(context, ct)
  │                           │
  │                           └─ (cancellation checked between each action)
  │
  └─ No timeline ──▶ DefaultSkillSequence
                       │  PlayAttackAsync()
                       │  DamageCalculator.Calculate()
                       │  TakeDamage() or Heal()
                       └─ PlayHitAsync()
```

### ITimelineAction Interface

```csharp
// Skill/Actions/ITimelineAction.cs
public interface ITimelineAction
{
    UniTask Execute(SkillContext context, CancellationToken ct = default);
}
```

### SkillContext

```csharp
// Skill/SkillContext.cs
public class SkillContext
{
    public BattleEntity Caster { get; }        // the entity using the skill
    public List<BattleEntity> Targets { get; } // resolved target entities
    public SkillDataSO SkillData { get; }      // the skill being executed
}
```

### Action Type Reference

| SkillActionType | Implementation | Description | Blocking? |
|-----------------|---------------|-------------|-----------|
| `PlayAnimation` | `PlayAnimAction` | Plays a named animation on the caster. Falls back to `PlayAttackAsync()` if `animationName` is empty. | Yes — waits for animation to complete |
| `MoveToTarget` | `MoveToAction` | Moves the caster toward the first target's hitbox center, stopping 1.5 units in front. Speed controlled by `moveSpeed`. | Yes — waits until destination reached |
| `ReturnToPosition` | `MoveToAction` (isReturn=true) | Moves the caster back to their original formation position. | Yes — waits until destination reached |
| `ApplyDamage` | `ApplyDamageAction` | Calculates and applies damage to all alive targets using `DamageCalculator`. Applies `damageMultiplierPercent` scaling. Negative damage triggers healing. | No — completes immediately |
| `SpawnEffect` | `SpawnEffectAction` | Spawns a visual effect prefab at each target's hitbox center (+ offset). Uses `EffectManager` if available, otherwise raw `Instantiate`. | Yes — waits for `effect.duration` |
| `PlaySound` | `PlaySoundAction` | Plays a one-shot sound effect via `SoundManager`, or `AudioSource.PlayClipAtPoint` as fallback. | No — completes immediately (fire-and-forget) |
| `CameraShake` | `CameraShakeAction` | Triggers screen shake via `BattleCameraController`. | Yes — waits for `duration` |
| `CameraZoom` | `CameraZoomAction` | Zooms the camera to the first target's hitbox center (or caster if no targets) with specified orthographic size. | Yes — waits for zoom animation |
| `Flash` | `FlashAction` | Flashes each target's sprite with the specified color. | Yes — waits for flash duration per target |
| `Wait` | `WaitAction` | Pauses execution for the specified `duration` in seconds. | Yes — waits for duration |
| `Parallel` | `ParallelAction` | Executes all `parallelActions[]` concurrently using `UniTask.WhenAll`. | Yes — waits for the longest sub-action |

---

## SkillActionData Fields

`SkillActionData` is a flat data class where only the fields relevant to the chosen `actionType` are used. Defined in `Data/SkillDataSO.cs`.

| Field | Type | Default | Used By |
|-------|------|---------|---------|
| `actionType` | `SkillActionType` | — | All (determines which action class is created) |
| `animationName` | `string` | `null` | `PlayAnimation` — animation state name; empty = default attack |
| `moveSpeed` | `float` | `10f` | `MoveToTarget`, `ReturnToPosition` — units per second |
| `duration` | `float` | `0.5f` | `Wait`, `CameraShake`, `CameraZoom`, `Flash`, `SpawnEffect` — seconds |
| `damageMultiplierPercent` | `int` | `100` | `ApplyDamage` — percentage multiplier on calculated damage |
| `effect` | `EffectDataSO` | `null` | `SpawnEffect` — references the visual effect asset |
| `soundClip` | `AudioClip` | `null` | `PlaySound` — the audio clip to play |
| `soundVolume` | `float` | `1f` | `PlaySound` — volume level (0.0–1.0) |
| `shakeIntensity` | `float` | `0.3f` | `CameraShake` — shake magnitude |
| `zoomSize` | `float` | `3f` | `CameraZoom` — target orthographic camera size |
| `flashColor` | `Color` | `White` | `Flash` — color to flash the target sprite |
| `parallelActions` | `SkillActionData[]` | `null` | `Parallel` — nested array of actions to run concurrently |

---

## Skill Timeline Examples

### Basic Melee Attack (Slash)

A physical skill that moves to the target, strikes, and returns:

```
Index  ActionType         Key Fields
─────  ─────────────────  ───────────────────────────────
  0    MoveToTarget       moveSpeed: 15
  1    PlayAnimation      animationName: "attack"
  2    Parallel
       ├─ ApplyDamage     damageMultiplierPercent: 100
       ├─ SpawnEffect     effect: SlashVFX
       ├─ PlaySound       soundClip: SwordHit.wav, soundVolume: 0.8
       └─ CameraShake     shakeIntensity: 0.2, duration: 0.15
  3    Wait               duration: 0.3
  4    ReturnToPosition   moveSpeed: 12
```

### Ranged Magic Attack (Fireball)

A magical skill that plays an animation in place and spawns an effect on targets:

```
Index  ActionType         Key Fields
─────  ─────────────────  ───────────────────────────────
  0    PlayAnimation      animationName: "attack"
  1    PlaySound          soundClip: FireCast.wav
  2    Wait               duration: 0.3
  3    Parallel
       ├─ SpawnEffect     effect: FireballVFX
       ├─ ApplyDamage     damageMultiplierPercent: 120
       ├─ CameraShake     shakeIntensity: 0.4, duration: 0.3
       └─ Flash           flashColor: (1, 0.3, 0, 1), duration: 0.2
  4    PlaySound          soundClip: FireExplosion.wav
```

### Multi-Hit Skill (Triple Strike)

A skill that applies damage multiple times with pauses:

```
Index  ActionType         Key Fields
─────  ─────────────────  ───────────────────────────────
  0    MoveToTarget       moveSpeed: 20
  1    PlayAnimation      animationName: "attack"
  2    ApplyDamage        damageMultiplierPercent: 40
  3    CameraShake        shakeIntensity: 0.15, duration: 0.1
  4    Wait               duration: 0.15
  5    ApplyDamage        damageMultiplierPercent: 40
  6    CameraShake        shakeIntensity: 0.15, duration: 0.1
  7    Wait               duration: 0.15
  8    ApplyDamage        damageMultiplierPercent: 60
  9    CameraShake        shakeIntensity: 0.3, duration: 0.2
 10    Flash              flashColor: White, duration: 0.15
 11    Wait               duration: 0.2
 12    ReturnToPosition   moveSpeed: 12
```

### Heal Skill

A support skill that heals allies (negative damage = healing):

```
Index  ActionType         Key Fields
─────  ─────────────────  ───────────────────────────────
  0    PlayAnimation      animationName: "attack"
  1    PlaySound          soundClip: HealCast.wav
  2    SpawnEffect        effect: HealVFX
  3    ApplyDamage        damageMultiplierPercent: 100
```

> Note: `SkillCategory.Healing` skills produce negative `FinalDamage` from `DamageCalculator`,
> which `ApplyDamageAction` converts to `Heal()` calls automatically.

---

## Extending the System

### Adding a New Timeline Action

1. Define a new enum value in `SkillActionType` (`Data/BattleEnums.cs`):
   ```csharp
   public enum SkillActionType
   {
       // ... existing values ...
       ApplyBuff   // new
   }
   ```

2. Create a new action class implementing `ITimelineAction` (`Skill/Actions/ApplyBuffAction.cs`):
   ```csharp
   public class ApplyBuffAction : ITimelineAction
   {
       public ApplyBuffAction(SkillActionData data) { /* read fields */ }
       public async UniTask Execute(SkillContext context, CancellationToken ct)
       {
           // implementation
       }
   }
   ```

3. Add relevant data fields to `SkillActionData` in `Data/SkillDataSO.cs`:
   ```csharp
   [Header("Buff")]
   public BuffDataSO buffData;
   ```

4. Register in `ActionFactory.Create()` (`Skill/ActionFactory.cs`):
   ```csharp
   SkillActionType.ApplyBuff => new ApplyBuffAction(data),
   ```

### Adding a New Event Channel

1. Define payload struct and channel class in `Core/EventBus/Events/`:
   ```csharp
   [System.Serializable]
   public struct MyCustomPayload
   {
       public string Message;
   }

   [CreateAssetMenu(menuName = "TxRpg/Events/My Custom Channel")]
   public class MyCustomEventChannel : EventChannel<MyCustomPayload> { }
   ```

2. Create the channel asset: Project window > Right-click > Create > TxRpg > Events > My Custom Channel.

3. Wire it up via `[SerializeField]` in any MonoBehaviour that needs to raise or listen to the event.
