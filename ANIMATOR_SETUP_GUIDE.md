# Animator Controller Setup Guide (Refactored)

## Quick Reference: Animator Parameters

The refactored system uses fewer, more focused parameters that map directly to gameplay state.

### Parameters to Create in Animator

| Parameter | Type | Values/Range | Purpose |
|-----------|------|--------------|---------|
| `RaceState` | Integer | 0-6 | Maps to RaceStartState enum |
| `SprintPhase` | Integer | 0-3 | Maps to SprintPhase enum |
| `NormalizedSpeed` | Float | 0.0 to 1.0 | Momentum/speed for stride variation |
| `BlockStart` | Trigger | N/A | One-shot block start animation |
| `FinishDip` | Trigger | N/A | One-shot finish dip animation |
| `Emote` | Trigger | N/A | One-shot emote animation |
| `FlagHold` | Trigger | N/A | One-shot flag hold animation |

## RaceState Parameter Values

Maps directly to `RaceStartState` enum:

```
0 = Idle              (default/rest state)
1 = OnYourMarks       (athlete in starting position)
2 = GetSet            (raised hips ready position)
3 = Go                (block start signal received)
4 = Running           (sprint in progress)
5 = FalseStart        (reset animation state)
6 = Finished          (race ended)
```

### Using RaceState in Animator
```
Transition from State A to State B:
  Condition: RaceState equals 1 (OnYourMarks)
  
Transition from State B to State C:
  Condition: RaceState equals 2 (GetSet)
```

## SprintPhase Parameter Values

Represents the current sprint phase:

```
0 = None             (no sprint active)
1 = Acceleration     (initial drive phase)
2 = TopSpeed         (sustained running)
3 = Jog              (post-finish cooldown)
```

### Using SprintPhase in Animator
```
Transition from Acceleration to TopSpeed:
  Condition: SprintPhase equals 2 (TopSpeed)
  
Transition from TopSpeed to Idle:
  Condition: SprintPhase equals 0 (None)
```

## Animator State Machine Setup

### State Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                      Any State                              │
│                  RaceState = 0 (Idle)                       │
│              ↓ (resets to this on FalseStart)               │
└─────────────────────────────────────────────────────────────┘
                              ↑
                              │
                    ┌─────────┴──────────┐
                    ↓                    ↓
         (RaceState = 1)        (BlockStart trigger)
         ┌──────────────┐       ┌──────────────┐
         │ OnYourMarks  │  ───→ │  BlockStart  │
         └──────────────┘       └──────────────┘
                    ↓                    │
         (RaceState = 2)                │
         ┌──────────────┐               │
         │     Set      │               │
         └──────────────┘               │
                    ↓                    │
         (RaceState = 3)                │
         ┌──────────────┐               │
         │  Go State    │  ←────────────┘
         └──────────────┘
                    ↓
         (RaceState = 4, SprintPhase = 1)
         ┌──────────────┐
         │ Acceleration │
         └──────────────┘
                    ↓
         (SprintPhase = 2)
         ┌──────────────┐
         │  TopSpeed    │
         │ [Blend Tree] │
         └──────────────┘
                    ↓
         (FinishDip trigger)
         ┌──────────────┐
         │ FinishDip    │
         └──────────────┘
                    ↓
         (SprintPhase = 3)
         ┌──────────────┐
         │     Jog      │
         └──────────────┘
                    ↓
         (Animation ends or SprintPhase = 0)
         ┌──────────────┐
         │     Idle     │
         └──────────────┘
```

## Detailed Transition Setup

### Idle → OnYourMarks
```
From State:     Idle
To State:       OnYourMarks
Condition:      RaceState equals 1
Has Exit Time:  No
Transition:     Immediate
```

### OnYourMarks → Set
```
From State:     OnYourMarks
To State:       Set
Condition:      RaceState equals 2
Has Exit Time:  No
Transition:     Immediate
```

### Set → Go
```
From State:     Set
To State:       Go
Condition:      RaceState equals 3
Has Exit Time:  No
Transition:     Immediate
```

### Go → BlockStart
```
From State:     Go (or similar)
To State:       BlockStart
Condition:      BlockStart trigger
Has Exit Time:  No
Transition:     0.05 seconds (immediate)
```

### BlockStart → Acceleration
```
From State:     BlockStart
To State:       Acceleration
Condition:      RaceState equals 4 AND SprintPhase equals 1
Has Exit Time:  No
Transition:     0.1 seconds (smooth blend)
```

### Acceleration → TopSpeed
```
From State:     Acceleration
To State:       TopSpeed
Condition:      SprintPhase equals 2
Has Exit Time:  No
Transition:     0.15-0.3 seconds (smooth blend)
Exit Time:      Optional if animation has natural duration
```

### TopSpeed → FinishDip
```
From State:     TopSpeed
To State:       FinishDip
Condition:      FinishDip trigger
Has Exit Time:  No
Transition:     0.05 seconds (immediate/snappy)
```

### FinishDip → Jog
```
From State:     FinishDip
To State:       Jog
Condition:      SprintPhase equals 3
Has Exit Time:  No
Transition:     0.1 seconds
```

### Jog → Idle
```
From State:     Jog
To State:       Idle
Condition:      SprintPhase equals 0
Has Exit Time:  No
Transition:     0.2 seconds
```

### Any State → Idle (False Start Reset)
```
From State:     Any State
To State:       Idle
Condition:      RaceState equals 0 (Idle)
Has Exit Time:  No
Transition:     0.1 seconds
```

## Animation Clips Required

| State | Animation | Loop | Duration |
|-------|-----------|------|----------|
| OnYourMarks | On Your Marks | No | ~1-2 sec |
| Set | Set Stance | No | ~0.5 sec |
| BlockStart | Block Start Drive | No | ~0.3 sec |
| Acceleration | Sprint Acceleration | Yes | Variable |
| TopSpeed | Sprint Running | Yes | Variable |
| FinishDip | Finish Dip | No | ~0.3 sec |
| Jog | Jog/Cooldown | Yes | Variable |
| Idle | Idle/Stand | Yes | Infinite |

## Optional: Speed Blending in TopSpeed

Create a Blend Tree inside the **TopSpeed** state for smooth stride variation:

### Blend Tree Setup
```
Name: TopSpeed Variations
Parameter: NormalizedSpeed (0 to 1)
Type: 1D Linear

Animation Clips:
  0.0 → Sprint Stride Slow
  0.5 → Sprint Stride Normal
  1.0 → Sprint Stride Fast
```

This allows subtle animation variation based on momentum without state transitions.

**Note:** Do NOT blend Acceleration and TopSpeed directly via a blend tree based on speed. They are separate gameplay phases.

## Implementation Checklist

- [ ] Create Integer parameter: RaceState
- [ ] Create Integer parameter: SprintPhase
- [ ] Create Float parameter: NormalizedSpeed (range 0-1)
- [ ] Create Trigger parameter: BlockStart
- [ ] Create Trigger parameter: FinishDip
- [ ] Create Trigger parameter: Emote
- [ ] Create Trigger parameter: FlagHold
- [ ] Create all animation states (OnYourMarks, Set, BlockStart, Acceleration, TopSpeed, FinishDip, Jog, Idle)
- [ ] Set up all transitions with correct conditions
- [ ] Verify transition durations (should be fast: 0.05-0.15 sec)
- [ ] (Optional) Create Blend Tree inside TopSpeed for stride variation
- [ ] Assign all animation clips to states
- [ ] Test race start sequence in Play mode
- [ ] Test sprint phase transitions
- [ ] Test finish sequence
- [ ] Test false start reset (RaceState = 0)

## Parameter Mapping Reference

### From Gameplay to Animator

```csharp
// In Athlete.cs:

// Race start sequence:
animationController.SetRaceState(RaceStartState.OnYourMarks);  // → RaceState = 1
animationController.SetRaceState(RaceStartState.GetSet);       // → RaceState = 2
animationController.SetRaceState(RaceStartState.Go);           // → RaceState = 3
animationController.PlayBlockStart();                          // → BlockStart trigger
animationController.SetRaceState(RaceStartState.Running);      // → RaceState = 4

// Sprint phases:
animationController.SetSprintPhase(SprintPhase.Acceleration);  // → SprintPhase = 1
animationController.SetSprintPhase(SprintPhase.TopSpeed);      // → SprintPhase = 2

// Finish:
animationController.PlayFinishDip();                           // → FinishDip trigger
animationController.SetSprintPhase(SprintPhase.Jog);           // → SprintPhase = 3

// Speed (every frame during sprint):
animationController.SetNormalizedSpeed(0.75f);                 // → NormalizedSpeed = 0.75

// Reset (false start):
animationController.SetRaceState(RaceStartState.Idle);         // → RaceState = 0
animationController.ResetAnimationState();                     // Resets all to 0
```

## Testing the Setup

### In Unity Play Mode

1. Select Athlete in Hierarchy
2. Open Animator window (Window → Animation → Animator)
3. Start race and observe:
   - [ ] RaceState parameter changes (1 → 2 → 3 → 4)
   - [ ] SprintPhase changes (1 → 2 → 3)
   - [ ] NormalizedSpeed updates (0 → 1 → varies)
   - [ ] Triggers fire once (BlockStart, FinishDip)
   - [ ] States transition smoothly
4. Test false start:
   - [ ] RaceState resets to 0
   - [ ] All parameters reset
   - [ ] Animation returns to Idle

## Common Setup Mistakes

### ❌ Problem: Conditions Never Met
- Check parameter exact names and types
- Verify conditions use correct comparison ("equals", not "greater than")
- Ensure RaceState and SprintPhase values match enum values
- Test with specific values, not ranges

### ❌ Problem: Animation Transitions Don't Happen
- Verify transition has no "Has Exit Time" if using conditions
- Check transition duration isn't too long
- Confirm origin and destination states are correct
- Test manually by setting parameter in Inspector

### ❌ Problem: Stuck in Acceleration
- SprintPhase transition condition checking for value 2?
- SetSprintPhase(SprintPhase.TopSpeed) being called?
- Verify IsAtMaxSpeed threshold in SprintController

### ❌ Problem: Triggers Don't Fire
- Check trigger parameter name exactly
- Verify animation state has transition from source state
- Triggers consumed immediately after frame, so check timing
- Use OnAnimatorMove to debug timing

### ❌ Problem: Wrong Animation Playing
- Verify animation clip assigned to correct state
- Check state's animation field (not blank)
- Confirm state is being entered (check Animator window)
- Verify animation clip loop setting

## Performance Optimization

- Keep transition durations reasonable (0.1-0.3 sec)
- Avoid overly complex blend trees
- Use trigger parameters for one-shots (faster than holding bools)
- Integer parameters cheaper than multiple bools
- Update float parameters only when needed (already done)

## Future Animation Additions

To add new animation trigger:

1. Add Trigger parameter in Animator (e.g., "Emote")
2. Add method in AthleteAnimationController:
   ```csharp
   public void PlayEmote()
   {
       if (animator == null) return;
       animator.SetTrigger(AnimParamIDs.Emote);
   }
   ```
3. Create parameter ID in AnimParamIDs class
4. Add state in Animator and transitions
5. Call from gameplay when needed

## Troubleshooting with Logs

Add debug output to verify parameter changes:
```csharp
public void SetRaceState(RaceStartState state)
{
    if (animator == null) return;
    Debug.Log($"Setting RaceState to {state} (value: {(int)state})");
    animator.SetInteger(AnimParamIDs.RaceState, (int)state);
}
```

Then observe logs while racing to confirm parameters updating correctly.
