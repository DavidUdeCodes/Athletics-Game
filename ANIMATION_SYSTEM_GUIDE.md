# Athlete Animation System Architecture Guide (Refactored)

## Overview

The animation system is designed as a **pure renderer** that visualizes gameplay state:
- **Gameplay systems** (RaceManager, Athlete, SprintController, etc.) are the source of truth
- **AthleteAnimationController** receives state updates and passes them to the Animator
- **Animator** responds to parameters via state machines and transitions
- **Animation controller maintains no duplicate game logic or state**

This architecture ensures gameplay logic remains independent of animation, making the system maintainable, testable, and scalable.

## Design Philosophy: Renderer Pattern

The animation controller follows a **renderer pattern**:
- Gameplay systems determine what state the athlete is in
- Animation controller receives state notifications
- Animation controller maps state to Animator parameters
- Animator visualizes that state

**No duplicate state machines.** **No animation-specific logic.**

Example data flow:
```
Gameplay: SprintController.IsAtMaxSpeed = true
  ↓
Athlete.UpdateAnimationSprint() detects transition
  ↓
Athlete: _currentSprintPhase = SprintPhase.TopSpeed
  ↓
Athlete calls: animationController.SetSprintPhase(SprintPhase.TopSpeed)
  ↓
AnimationController sets: animator.SetInteger(SprintPhase, 2)
  ↓
Animator responds: transitions from Acceleration to TopSpeed state
```

## Core Components

### 1. AthleteAnimationController
Location: `Assets/Scripts/Athlete/AthleteAnimationController.cs`

**Responsibilities:**
- Own the Animator reference
- Map gameplay state to Animator parameters (pure translation)
- Manage all animator parameter IDs (hashed for performance)
- Provide clean public API that gameplay calls

**What it does NOT do:**
- Store gameplay state
- Make gameplay decisions
- Determine sprint phases
- Control animation flow

### 2. SprintPhase Enum
Represents the current sprint phase in the animation system:
```csharp
public enum SprintPhase
{
    None,           // No sprint active
    Acceleration,   // Initial drive phase
    TopSpeed,       // Sustained running phase
    Jog             // Post-finish deceleration
}
```

Note: `None` state allows proper reset and idle state handling.

### 3. Athlete Class Integration
Location: `Assets/Scripts/Athlete/Athlete.cs`

Athlete is now the authority for sprint phase tracking:
```csharp
private SprintPhase _currentSprintPhase = SprintPhase.None;
```

This is the source of truth. When Athlete detects a phase change (e.g., reached max speed), it:
1. Updates its local `_currentSprintPhase`
2. Calls `animationController.SetSprintPhase(newPhase)`
3. Animation controller updates the Animator

## Animator Parameters (Refactored)

All parameters are now organized to represent persistent gameplay state or continuous values:

### Persistent State Parameters (Integers)

#### RaceState (integer, 0-6)
Maps directly to the existing `RaceStartState` enum:
```csharp
0 = Idle
1 = OnYourMarks
2 = GetSet
3 = Go
4 = Running
5 = FalseStart
6 = Finished
```

Updated via: `SetRaceState(RaceStartState state)`

#### SprintPhase (integer, 0-3)
Represents current sprint phase:
```csharp
0 = None
1 = Acceleration
2 = TopSpeed
3 = Jog
```

Updated via: `SetSprintPhase(SprintPhase phase)`

### Continuous Value Parameters

#### NormalizedSpeed (float, 0-1)
Current momentum/speed as a 0-1 value.
- Updated every frame during sprint
- Used for stride variation within TopSpeed state
- Derived from `MomentumController.CurrentMomentum`

Updated via: `SetNormalizedSpeed(float speed)`

### One-Shot Action Triggers

#### BlockStart
Plays block start animation when Go signal fires.
Triggered via: `PlayBlockStart()`

#### FinishDip
Plays finish line dip animation.
Triggered via: `PlayFinishDip()`

#### Emote
Plays emote/celebration animation.
Triggered via: `PlayEmote()`

#### FlagHold
Plays flag hold animation (for flag bearer, etc.).
Triggered via: `PlayFlagHold()`

## Public API

### State Parameters

```csharp
public void SetRaceState(RaceStartState state)
```
Sets the RaceState integer parameter. Called whenever race state changes.

```csharp
public void SetSprintPhase(SprintPhase phase)
```
Sets the SprintPhase integer parameter. Called when sprint phase changes (Acceleration → TopSpeed, etc.).

```csharp
public void SetNormalizedSpeed(float normalizedSpeed)
```
Sets the NormalizedSpeed float parameter (0-1, clamped). Called every frame during sprint.

### One-Shot Actions

```csharp
public void PlayBlockStart()
```
Triggers block start animation (one-shot).

```csharp
public void PlayFinishDip()
```
Triggers finish dip animation (one-shot).

```csharp
public void PlayEmote()
```
Triggers emote/celebration animation (one-shot).

```csharp
public void PlayFlagHold()
```
Triggers flag hold animation (one-shot).

### Utility

```csharp
public void ResetAnimationState()
```
Resets all animation parameters to neutral state. Used during false starts.
- Sets RaceState to Idle
- Sets SprintPhase to None
- Sets NormalizedSpeed to 0

## Animation Flow Sequence

### Race Start Sequence
```
Idle (RaceState = Idle)
  ↓ (OnYourMarks event)
  ↓ SetRaceState(OnYourMarks)
On Your Marks Animation
  ↓ (after delay)
  ↓ SetRaceState(GetSet)
Set Animation
  ↓ (after delay)
  ↓ SetRaceState(Go)
  ↓ PlayBlockStart() [one-shot trigger]
Go State + Block Start Animation
  ↓ (input received)
  ↓ SetRaceState(Running)
  ↓ SetSprintPhase(Acceleration)
Running State + Acceleration Animation
  ↓ (momentum updates every frame)
  ↓ SetNormalizedSpeed(currentMomentum)
Stride variation within Acceleration
  ↓ (max speed reached)
  ↓ SetSprintPhase(TopSpeed)
Transition to TopSpeed Animation
  ↓ (continuing sprint)
  ↓ SetNormalizedSpeed(currentMomentum) [every frame]
TopSpeed with Stride Variation
  ↓ (crosses finish line)
  ↓ PlayFinishDip() [one-shot trigger]
Finish Dip Animation
  ↓ (deceleration phase)
  ↓ SetSprintPhase(Jog)
Jog Animation
  ↓ (speed reaches zero)
Idle State
```

## Integration Points in Athlete.cs

### EnterGetSetState()
```csharp
_animationController.SetRaceState(RaceStartState.GetSet);
```

### EnterGoState()
```csharp
_animationController.SetRaceState(RaceStartState.Go);
_animationController.PlayBlockStart();  // One-shot trigger
```

### EnterRunningState()
```csharp
_animationController.SetRaceState(RaceStartState.Running);
_animationController.SetSprintPhase(SprintPhase.Acceleration);
```

### FinishRace()
```csharp
_animationController.PlayFinishDip();  // One-shot trigger
```

### HandleAthleteAtRest()
```csharp
_animationController.SetSprintPhase(SprintPhase.Jog);
```

### Update Loop (UpdateAnimationSprint)
```csharp
// Each frame during sprint:
if (_currentSprintPhase == SprintPhase.Acceleration)
{
    if (_sprintController.IsAtMaxSpeed)
    {
        _currentSprintPhase = SprintPhase.TopSpeed;
        _animationController.SetSprintPhase(SprintPhase.TopSpeed);
    }
}

float normalizedSpeed = _momentumController.CurrentMomentum;
_animationController.SetNormalizedSpeed(normalizedSpeed);
```

### False Start (HandleAnimationFalseStart)
```csharp
_animationController.SetRaceState(RaceStartState.FalseStart);
_animationController.ResetAnimationState();
_currentSprintPhase = SprintPhase.None;
```

## Animator Setup (TrackChar.controller)

### Expected Parameters

```
RaceState (int)
SprintPhase (int)
NormalizedSpeed (float)
BlockStart (Trigger)
FinishDip (Trigger)
Emote (Trigger)
FlagHold (Trigger)
```

### State Machine Structure

```
Base Layer:
  - Idle (entry state)
  - OnYourMarks
  - Set
  - BlockStart
  - Acceleration
  - TopSpeed
  - FinishDip
  - Jog

Transitions:
  - Any → Idle (for FalseStart reset)
  - Idle → OnYourMarks (when RaceState = 1)
  - OnYourMarks → Set (when RaceState = 2)
  - Set → BlockStart (when RaceState = 3)
  - BlockStart → Acceleration (when RaceState = 4 and SprintPhase = 1)
  - Acceleration → TopSpeed (when SprintPhase = 2)
  - TopSpeed → FinishDip (when PlayFinishDip trigger)
  - FinishDip → Jog (when SprintPhase = 3)
  - Jog → Idle (when animation ends or Jog duration complete)
```

### Speed Blending (Optional)

Inside **TopSpeed state**, optionally add a Blend Tree:
- Parameter: NormalizedSpeed (0-1)
- Blends between stride variations:
  - 0.0 = Stride Slow
  - 0.5 = Stride Normal
  - 1.0 = Stride Fast

This provides smooth animation variation without phase transitions.

## Key Differences from Previous Version

### Before (Old Implementation)
- `PlayRaceStartState()` used multiple triggers
- `IsAccelerating` and `IsTopSpeed` bools (mutually exclusive)
- `EnterSprint()` and `TransitionToTopSpeed()` methods
- `CurrentSprintPhase` property stored duplicate state
- Animation controller maintained gameplay state

### After (Refactored Implementation)
- `SetRaceState()` uses single integer parameter
- Single `SprintPhase` integer parameter (no conflicting bools)
- `SetSprintPhase()` method (state set directly by Athlete)
- No duplicate state in animation controller
- Animation controller purely receives and renders state

## Best Practices

### Do ✓
- Call animation methods from Athlete.cs
- Track sprint phase in Athlete class
- Use integer parameters for mutually exclusive states
- Use triggers only for one-shot animations
- Use floats for continuous values
- Subscribe to existing gameplay events

### Don't ✗
- Access `animator` directly from gameplay code
- Store gameplay state in animation controller
- Use multiple bools for mutually exclusive states
- Use triggers for persistent state
- Make gameplay decisions in animation controller
- Duplicate game logic in animation system

## Scaling to Other Events

The refactored architecture easily supports new athletics events:

```csharp
// For Long Jump:
public enum LongJumpPhase
{
    None,
    RunUp,
    Takeoff,
    Flight,
    Landing
}

// In animation controller:
public void SetLongJumpPhase(LongJumpPhase phase)
{
    animator.SetInteger(AnimParamIDs.JumpPhase, (int)phase);
}
```

Each event maintains the same pattern:
- Gameplay determines phase
- Animation controller receives phase updates
- Animator visualizes phase

## Performance Notes

- All parameter IDs hashed at class load (zero runtime cost)
- Integer parameters cheaper than multiple bools
- Float parameters updated only when needed
- One Animator per athlete (Unity standard)
- No string hashing in runtime loops
- Minimal memory overhead (no state duplication)

## Architecture Diagram

```
RaceManager (Race Flow)
    ↓
Athlete (Game State Authority)
    ├─ _currentSprintPhase (local tracking)
    ├─ _currentRaceState (implied from RaceManager)
    ↓
AthleteAnimationController (Parameter Renderer)
    ├─ SetRaceState()
    ├─ SetSprintPhase()
    ├─ SetNormalizedSpeed()
    ├─ PlayBlockStart()
    ├─ PlayFinishDip()
    ├─ PlayEmote()
    ├─ PlayFlagHold()
    ↓
Animator (Parameter Responder)
    ├─ Reads: RaceState, SprintPhase, NormalizedSpeed
    ├─ Responds to: BlockStart, FinishDip, Emote, FlagHold
    ↓
Character Model (Visual Output)
```

**Key principle:** Gameplay systems control state. Animation system visualizes state. No feedback loop.

## Troubleshooting

### Animation not changing states
1. Verify SetRaceState() or SetSprintPhase() is being called
2. Check animator has correct parameter names
3. Confirm animator transitions have correct conditions
4. Verify animator state machine setup matches expectations

### Unexpected state combinations
1. This is impossible now - only one RaceState and one SprintPhase at a time
2. If unexpected behavior, check Athlete._currentSprintPhase logic

### Performance issues
1. Ensure SetNormalizedSpeed() is only called during sprint (it already is)
2. Avoid calling setters unnecessarily (existing code only calls when changing)

## Code Examples

### Setting Race State
```csharp
// In Athlete.EnterRunningState()
_animationController.SetRaceState(RaceStartState.Running);
```

### Setting Sprint Phase
```csharp
// In Athlete.UpdateAnimationSprint()
if (_sprintController.IsAtMaxSpeed)
{
    _currentSprintPhase = SprintPhase.TopSpeed;
    _animationController.SetSprintPhase(SprintPhase.TopSpeed);
}
```

### Updating Continuous Value
```csharp
// In Athlete.UpdateAnimationSprint()
float speed = _momentumController.CurrentMomentum;
_animationController.SetNormalizedSpeed(speed);
```

### Playing One-Shot
```csharp
// In Athlete.EnterGoState()
_animationController.PlayBlockStart();
```

### Resetting State
```csharp
// In Athlete.ResetForFalseStart()
_animationController.ResetAnimationState();
_currentSprintPhase = SprintPhase.None;
```
