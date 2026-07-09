using UnityEngine;
using UnityEngine.Splines;

public class Athlete : MonoBehaviour
{
    [Header("Stats")]
    public AthleteStats stats;

    [Header("Identity")]
    public string athleteName = "Athlete";
    public bool isPlayer = true;
    [SerializeField] private int athleteLane = 1;

    [Header("Sprint Input UI")]
    [SerializeField] private RhythmInputUI rhythmInputUI;
    [SerializeField] private ForceControlInputUI forceControlInputUI;

    [Header("Race Manager")]
    [SerializeField] private RaceManager raceManager;

    private AthleteInput _input;
    private AthleteMovement _movement;
    private RhythmInputMode _rhythmController;
    private ForceControlInputMode _forceControlInputMode;
    private SprintController _sprintController;
    private MomentumController _momentumController;
    private AthleteAnimationController _animationController;

    private ISprintInputMode _currentInputMode;
    private ISprintInputModeUI _activeUI;

    private float _pendingStartingBonus = 0f;

    public System.Action<Athlete, float> OnRaceFinished;
    public System.Action<Athlete> OnAthleteAtRest;

    private float _raceTime = 0f;
    private bool _raceActive = false;
    private bool _hasFinishedRace = false;

    public float CurrentDistance => _movement != null ? _movement.DistanceTravelled : 0f;

    public int AthleteLane => isPlayer ? raceManager.PlayerLane : athleteLane;
    
    public float CurrentSpeed => _movement != null ? _movement.CurrentSpeed : 0f;
    
    public float GetCurrentSpeed() => CurrentSpeed;

    private void Awake()
    {
        _input = GetComponent<AthleteInput>();
        _movement = GetComponent<AthleteMovement>();
        _rhythmController = GetComponent<RhythmInputMode>();
        _forceControlInputMode = GetComponent<ForceControlInputMode>();
        _sprintController = GetComponent<SprintController>();
        _momentumController = GetComponent<MomentumController>();
        _animationController = GetComponent<AthleteAnimationController>();
        
        if (_rhythmController == null)
        {
            Debug.LogWarning($"RhythmController not found on {gameObject.name}");
        }

        if (_forceControlInputMode == null)
        {
            _forceControlInputMode = gameObject.AddComponent<ForceControlInputMode>();
        }
        
        if (_momentumController == null)
        {
            _momentumController = gameObject.AddComponent<MomentumController>();
        }
    }

    private void Start()
    {
        if (stats != null && _movement != null)
        {
            _movement.SetStatMultipliers(
                stats.GetTopSpeedMultiplier(),
                stats.GetAccelerationMultiplier()
            );
        }

        if (_sprintController != null && _momentumController != null)
        {
            _sprintController.SetMomentumController(_momentumController);
        }

        if (_rhythmController != null)
        {
            _rhythmController.Initialize(this);
            _rhythmController.OnFalseStartDetected += HandleFalseStart;
        }

        if (_forceControlInputMode != null)
        {
            _forceControlInputMode.Initialize(this);
            _forceControlInputMode.OnFalseStartDetected += HandleFalseStart;
        }

        InitializeInputMode();

        if (rhythmInputUI != null && _rhythmController != null && _sprintController != null)
        {
            rhythmInputUI.SetControllers(_rhythmController, _sprintController, _momentumController);
        }

        if (forceControlInputUI != null && _forceControlInputMode != null && _sprintController != null)
        {
            forceControlInputUI.SetControllers(_forceControlInputMode, _sprintController, _momentumController);
        }

        if (isPlayer && _input != null)
        {
            _input.OnTap += HandleInputQuality;
            _input.OnTap += HandleInputFeedback;
            _input.OnTap += HandleReactionTiming;
        }

        if (_movement != null)
        {
            _movement.OnAthleteAtRest += HandleAthleteAtRest;
        }

        // Single source of truth for race-start-state animation/reset handling.
        // Not gated by isPlayer: RaceManager.NotifyAthletesOfStateChange already
        // calls EnterGetSetState/EnterGoState/EnterRunningState directly on every
        // athlete (player and AI alike), so this handler only needs to cover the
        // states RaceManager doesn't push directly - OnYourMarks, and FalseStart
        // (which bypasses SetRaceStartState entirely and never raises
        // OnRaceStartStateChanged in the first place).
        if (raceManager != null)
        {
            raceManager.OnRaceStartStateChanged += HandleRaceStartStateChanged;
            raceManager.OnFalseStart += HandleFalseStartAnimation;
        }

        if (isPlayer && raceManager != null)
        {
            raceManager.OnRaceConfigChanged += HandleRaceConfigChanged;
            raceManager.OnInputModeChanged += HandleInputModeChanged;
            RepositionForRaceConfig(raceManager.CurrentRaceConfig);

            SynchronizeWithCurrentRaceState();
        }

        if (_animationController != null)
        {
            _animationController.OnFinishDipComplete += HandleFinishDipComplete;
        }

        StartRace();
    }

    private void OnDestroy()
    {
        if (_input != null)
        {
            _input.OnTap -= HandleInputQuality;
            _input.OnTap -= HandleInputFeedback;
            _input.OnTap -= HandleReactionTiming;
        }

        if (_movement != null)
        {
            _movement.OnAthleteAtRest -= HandleAthleteAtRest;
        }

        if (_rhythmController != null)
        {
            _rhythmController.OnFalseStartDetected -= HandleFalseStart;
        }

        if (_forceControlInputMode != null)
        {
            _forceControlInputMode.OnFalseStartDetected -= HandleFalseStart;
        }

        if (raceManager != null)
        {
            raceManager.OnRaceStartStateChanged -= HandleRaceStartStateChanged;
            raceManager.OnFalseStart -= HandleFalseStartAnimation;
        }

        if (isPlayer && raceManager != null)
        {
            raceManager.OnRaceConfigChanged -= HandleRaceConfigChanged;
            raceManager.OnInputModeChanged -= HandleInputModeChanged;
        }

        if (_animationController != null)
        {
            _animationController.OnFinishDipComplete -= HandleFinishDipComplete;
        }
    }

    private void InitializeInputMode()
    {
        if (raceManager == null)
        {
            raceManager = FindAnyObjectByType<RaceManager>();
        }

        SprintInputMode selectedMode = raceManager != null ? raceManager.CurrentInputMode : SprintInputMode.Rhythm;
        DisableAllInputModes();
        HideAllUI();

        _currentInputMode = selectedMode switch
        {
            SprintInputMode.Rhythm => _rhythmController,
            SprintInputMode.ForceControl => _forceControlInputMode,
            _ => _rhythmController
        };

        _activeUI = selectedMode switch
        {
            SprintInputMode.Rhythm => rhythmInputUI,
            SprintInputMode.ForceControl => forceControlInputUI,
            _ => rhythmInputUI
        };

        if (_currentInputMode != null)
        {
            _currentInputMode.Enable();
            _input.SetInputMode(_currentInputMode);
        }

        if (_activeUI != null)
        {
            _activeUI.Show();
        }
    }

    private void DisableAllInputModes()
    {
        if (_rhythmController != null)
            _rhythmController.Disable();
        if (_forceControlInputMode != null)
            _forceControlInputMode.Disable();
    }

    private void HideAllUI()
    {
        if (rhythmInputUI != null)
            rhythmInputUI.Hide();
        if (forceControlInputUI != null)
            forceControlInputUI.Hide();
    }

    private void HandleInputModeChanged(SprintInputMode newMode)
    {
        if (!isPlayer || !_raceActive) return;

        ISprintInputMode newInputMode = newMode switch
        {
            SprintInputMode.Rhythm => _rhythmController,
            SprintInputMode.ForceControl => _forceControlInputMode,
            _ => _rhythmController
        };

        ISprintInputModeUI newUI = newMode switch
        {
            SprintInputMode.Rhythm => rhythmInputUI,
            SprintInputMode.ForceControl => forceControlInputUI,
            _ => rhythmInputUI
        };

        if (_currentInputMode != null)
        {
            _currentInputMode.Disable();
        }

        if (_activeUI != null)
        {
            _activeUI.Hide();
        }

        _currentInputMode = newInputMode;
        _activeUI = newUI;

        if (_currentInputMode != null)
        {
            _currentInputMode.Enable();
            _currentInputMode.Reset();
            _input.SetInputMode(_currentInputMode);
        }

        if (_activeUI != null)
        {
            _activeUI.Show();
        }
    }

    private void HandleInputQuality(TapQuality quality)
    {
        if (_momentumController != null && !_hasFinishedRace)
            _momentumController.ApplyQuality(quality);
    }

    private void HandleInputFeedback(TapQuality quality)
    {
        if (_activeUI != null)
            _activeUI.ShowQualityFeedback(quality);
    }

    private void HandleReactionTiming(TapQuality quality)
    {
        if (isPlayer && raceManager != null && raceManager.CurrentStartState == RaceStartState.Go)
        {
            raceManager.RecordReactionTime(this);
        }
    }

    private void HandleFalseStart()
    {
        if (isPlayer && raceManager != null)
        {
            raceManager.HandleFalseStart(this);
        }
    }

    // Covers only the race-start states RaceManager doesn't already push directly
    // via NotifyAthletesOfStateChange (see EnterGetSetState/EnterGoState/EnterRunningState
    // below, which RaceManager calls on every athlete for GetSet/Go/Running).
    // Applies to every athlete with an animation controller, not just the player -
    // AI athletes need the OnYourMarks pose too.
    private void HandleRaceStartStateChanged(RaceStartState newState)
    {
        if (newState == RaceStartState.OnYourMarks)
        {
            _animationController?.SetRaceState(RaceStartState.OnYourMarks);
        }
    }

    private void SynchronizeWithCurrentRaceState()
    {
        if (!isPlayer || raceManager == null) return;

        RaceStartState currentState = raceManager.CurrentStartState;
        switch (currentState)
        {
            case RaceStartState.OnYourMarks:
                _animationController?.SetRaceState(RaceStartState.OnYourMarks);
                break;
            case RaceStartState.GetSet:
                EnterGetSetState();
                break;
            case RaceStartState.Go:
                EnterGoState();
                break;
            case RaceStartState.Running:
                EnterRunningState();
                break;
        }
    }

    // Called directly by RaceManager.NotifyAthletesOfStateChange for every athlete
    // (player and AI) - do not also call these from an event handler, or gameplay
    // side effects (input, movement, sprint start) will fire twice per transition
    // for the player.
    public void EnterGetSetState()
    {
        if (_currentInputMode != null)
        {
            _currentInputMode.EnterGetSetState();
        }
        
        if (_input != null)
        {
            _input.AllowInput(false);
        }

        _animationController?.SetRaceState(RaceStartState.GetSet);
    }

    public void EnterGoState()
    {
        if (_currentInputMode != null)
        {
            _currentInputMode.ExitGetSetState();
        }
        
        if (_input != null)
        {
            _input.AllowInput(true);
        }

        // No dedicated Animator pose for Go - visually it's the same crouched
        // stance as GetSet (the athlete hasn't moved yet, just reacting), and the
        // window is often a single frame anyway. RaceState stays at GetSet until
        // Running fires.
    }

    public void EnterRunningState()
    {
        if (_currentInputMode != null)
        {
            _currentInputMode.EnterRunningState();
        }
        
        if (_input != null)
        {
            _input.AllowInput(true);
        }

        if (_sprintController != null)
        {
            _sprintController.StartSprinting();
        }

        if (_movement != null)
        {
            _movement.StartMoving();
        }

        if (_animationController != null)
        {
            _animationController.SetRaceState(RaceStartState.Running);
        }

        if (_pendingStartingBonus > 0f && _momentumController != null)
        {
            _momentumController.ApplyStartingBonus(_pendingStartingBonus);
            _pendingStartingBonus = 0f;
        }

        if (raceManager != null)
        {
            raceManager.StartRace();
        }
    }

    public void ApplyStartingMomentumBonus(float bonus)
    {
        _pendingStartingBonus = bonus;
    }

    public void ResetForFalseStart()
    {
        if (_currentInputMode != null)
        {
            _currentInputMode.Reset();
        }

        _animationController?.ResetAnimationState();
    }

    private void HandleRaceConfigChanged(RaceConfiguration newConfig)
    {
        if (!isPlayer || !_raceActive) return;
        
        FullyResetRaceState();
        RepositionForRaceConfig(newConfig);
    }

    private void RepositionForRaceConfig(RaceConfiguration raceConfig)
    {
        if (!raceConfig.IsValid || _movement == null)
            return;

        int lane = isPlayer ? raceManager.PlayerLane : athleteLane;
        RaceInitializer.InitializeAthleteForRace(this, raceConfig, lane);
    }

    private void FullyResetRaceState()
    {
        if (_movement == null)
            return;
            
        _movement.ResetMovementState();
        _hasFinishedRace = false;
    }

    public void StartRace()
    {
        _raceTime = 0f;
        _raceActive = true;
        _hasFinishedRace = false;
        
        if (isPlayer && _input != null)
            _input.SetEnabled(true);
        
        if (raceManager != null)
        {
            raceManager.OnAthleteFinished += HandleAthleteFinished;
        }
        
        Debug.Log($"{athleteName} race initialization complete");
    }

    public void FinishRace()
    {
        if (!_raceActive || _hasFinishedRace) return;
        
        _hasFinishedRace = true;

        _movement.FinishRace();

        if (_animationController != null)
        {
            // OnRaceFinished now fires once the dip animation actually completes
            // (see HandleFinishDipComplete), rather than the instant the trigger is set.
            _animationController.PlayFinishDip();
        }
        else
        {
            OnRaceFinished?.Invoke(this, _raceTime);
        }

        Debug.Log($"{athleteName} finished in {_raceTime:F2}s");
    }

    private void HandleFinishDipComplete()
    {
        if (!_hasFinishedRace) return;
        OnRaceFinished?.Invoke(this, _raceTime);
    }

    private void HandleAthleteAtRest()
    {
        _raceActive = false;
        
        // Finished already exists as a real race-start state and wasn't wired to
        // the Animator before - it's a better fit for the cooldown pose than
        // forcing the speed-driven blend tree toward its low end artificially.
        _animationController?.SetRaceState(RaceStartState.Finished);

        if (raceManager != null)
        {
            raceManager.RegisterAthleteAtRest(this);
        }
        
        OnAthleteAtRest?.Invoke(this);
    }

    private void Update()
    {
        if (!_raceActive) return;
        
        _raceTime += Time.deltaTime;
        
        if (!_hasFinishedRace && raceManager != null && raceManager.IsRaceActive)
        {
            raceManager.CheckForAthleteFinish(this, CurrentDistance);
        }

        UpdateAnimationSprint();
    }

    private void UpdateAnimationSprint()
    {
        if (_animationController == null || _momentumController == null)
            return;

        // Sprint/jog is a continuous blend on speed now, not a discrete phase -
        // no need to watch for a max-speed transition here anymore.
        _animationController.SetNormalizedSpeed(_momentumController.CurrentMomentum);
    }

    private void HandleAthleteFinished(Athlete athlete, int finishOrder, float raceTime)
    {
        if (athlete == this)
        {
            FinishRace();
        }
    }

    // Raised whenever RaceManager's FalseStartSequence resets state - this bypasses
    // SetRaceStartState entirely, so it's the one race-flow event that never comes
    // through OnRaceStartStateChanged and needs its own subscription.
    private void HandleFalseStartAnimation()
    {
        if (_animationController == null) return;

        _animationController.SetRaceState(RaceStartState.FalseStart);
        _animationController.ResetAnimationState();
    }

    // Public pass-throughs for one-off animation calls triggered from outside the
    // race-state flow - e.g. a victory emote after finishing, or holding a flag
    // pose on a menu/results screen.
    public void PlayEmote(EmoteType emote) => _animationController?.PlayEmote(emote);
    public void SetFlagHold(bool isHolding) => _animationController?.SetFlagHold(isHolding);

    public float RaceTime => _raceTime;
    public bool HasFinishedRace => _hasFinishedRace;
    public bool IsAtRest => !_movement.IsMoving && _hasFinishedRace;
}