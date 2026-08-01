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
    private AISprinterController _aiSprinterController;

    private ISprintInputMode _currentInputMode;
    private ISprintInputModeUI _activeUI;

    private float _pendingStartingBonus = 0f;
    private bool _raceSetupInjected = false;

    public System.Action<Athlete, float> OnRaceFinished;
    public System.Action<Athlete> OnAthleteAtRest;

    private float _raceTime = 0f;
    private bool _raceActive = false;
    private bool _raceOfficiallyStarted = false;
    private bool _hasFinishedRace = false;
    private float _finishPeakSpeed = 1f;

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
        _aiSprinterController = GetComponent<AISprinterController>();

        if (isPlayer)
        {
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

        // OnRaceStartStateChanged / OnFalseStart subscriptions and initial positioning
        // are either done here (scene-placed athletes) or already done by InjectRaceSetup
        // (dynamically spawned AI athletes). The _raceSetupInjected flag prevents double
        // subscription in the latter case.
        if (raceManager != null && !_raceSetupInjected)
        {
            raceManager.OnRaceStartStateChanged += HandleRaceStartStateChanged;
            raceManager.OnFalseStart += HandleFalseStartAnimation;
            RepositionForRaceConfig(raceManager.CurrentRaceConfig);
            _raceSetupInjected = true;
        }

        if (isPlayer && raceManager != null)
        {
            raceManager.OnRaceConfigChanged += HandleRaceConfigChanged;
            raceManager.OnInputModeChanged += HandleInputModeChanged;
            SynchronizeWithCurrentRaceState();
        }

        // Initialise AI controller with the race distance now that raceManager is available.
        if (!isPlayer && _aiSprinterController != null && raceManager != null)
        {
            _aiSprinterController.Initialize(raceManager.RaceDistanceInMeters);
        }

        // Bring the animation state in sync for all athletes, including AI that may
        // have been spawned after the sequence already reached OnYourMarks.
        SynchronizeAnimationWithRaceState();

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
        if (!isPlayer) return;

        if (raceManager == null)
        {
            Debug.LogError($"{gameObject.name}: RaceManager not assigned to Athlete in Inspector");
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

    /// <summary>
    /// Syncs the Animator to the current race start state for all athletes.
    /// Handles AI athletes spawned after the sequence already began (e.g. OnYourMarks
    /// animation would otherwise be missed because Start() runs a frame after Instantiate).
    /// </summary>
    private void SynchronizeAnimationWithRaceState()
    {
        if (_animationController == null || raceManager == null) return;

        if (raceManager.CurrentStartState == RaceStartState.OnYourMarks)
            _animationController.SetRaceState(RaceStartState.OnYourMarks);
    }

    /// <summary>
    /// Called by RaceManager immediately after Instantiating an AI athlete prefab so
    /// that the athlete is fully set up before Start() runs. Safe to call before Awake
    /// completes on other objects; all work here operates only on this athlete.
    /// </summary>
    public void InjectRaceSetup(RaceManager manager, RaceConfiguration config, int lane)
    {
        if (_raceSetupInjected) return;
        _raceSetupInjected = true;

        raceManager = manager;
        if (!isPlayer) athleteLane = lane;

        if (raceManager != null)
        {
            raceManager.OnRaceStartStateChanged += HandleRaceStartStateChanged;
            raceManager.OnFalseStart += HandleFalseStartAnimation;
        }

        if (_aiSprinterController != null)
        {
            if (config != null) _aiSprinterController.Initialize(manager.RaceDistanceInMeters);
        }

        RepositionForRaceConfig(config);
    }

    /// <summary>Public setter so RaceManager can inject itself on dynamically spawned athletes.</summary>
    public void SetRaceManager(RaceManager manager)
    {
        raceManager = manager;
    }

    /// <summary>Public setter for the lane index, used when spawning AI athletes at runtime.</summary>
    public void SetAthleteLane(int lane)
    {
        athleteLane = lane;
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

        _raceOfficiallyStarted = true;
    }

    public void ApplyStartingMomentumBonus(float bonus)
    {
        _pendingStartingBonus = bonus;
    }

    private void HandleRaceConfigChanged(RaceConfiguration newConfig)
    {
        if (!isPlayer || !_raceActive) return;
        
        FullyResetRaceState();
        RepositionForRaceConfig(newConfig);
    }

    private void RepositionForRaceConfig(RaceConfiguration raceConfig)
    {
        if (raceConfig == null || !raceConfig.IsValid || _movement == null)
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
        _raceOfficiallyStarted = false;
        _raceTime = 0f;
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
        
        // Capture speed at the finish line so the animation can track deceleration.
        _finishPeakSpeed = _movement != null && _movement.CurrentSpeed > 0.01f
            ? _movement.CurrentSpeed
            : 1f;

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
        
        if (_raceOfficiallyStarted)
        {
            _raceTime += Time.deltaTime;
        }
        
        if (!_hasFinishedRace && raceManager != null && raceManager.IsRaceActive)
        {
            raceManager.CheckForAthleteFinish(this, CurrentDistance);
        }

        UpdateAnimationSprint();
    }

    private void UpdateAnimationSprint()
    {
        if (_animationController == null) return;

        // After crossing the finish line, drive animation directly from actual
        // movement speed so the deceleration is reflected accurately.
        if (_hasFinishedRace && _movement != null)
        {
            float normalized = Mathf.Clamp01(_movement.CurrentSpeed / _finishPeakSpeed);
            _animationController.SetNormalizedSpeed(normalized);
            return;
        }

        if (_momentumController != null)
        {
            _animationController.SetNormalizedSpeed(_momentumController.CurrentMomentum);
        }
        else if (_aiSprinterController != null)
        {
            _animationController.SetNormalizedSpeed(_aiSprinterController.NormalizedSpeed);
        }
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