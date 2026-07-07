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

    private ISprintInputMode _currentInputMode;
    private ISprintInputModeUI _activeUI;

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
        }

        if (_forceControlInputMode != null)
        {
            _forceControlInputMode.Initialize(this);
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
        }

        if (_movement != null)
        {
            _movement.OnAthleteAtRest += HandleAthleteAtRest;
        }

        if (isPlayer && raceManager != null)
        {
            raceManager.OnRaceConfigChanged += HandleRaceConfigChanged;
            raceManager.OnInputModeChanged += HandleInputModeChanged;
            RepositionForRaceConfig(raceManager.CurrentRaceConfig);
        }

        StartRace();
    }

    private void OnDestroy()
    {
        if (_input != null)
        {
            _input.OnTap -= HandleInputQuality;
            _input.OnTap -= HandleInputFeedback;
        }

        if (_movement != null)
        {
            _movement.OnAthleteAtRest -= HandleAthleteAtRest;
        }

        if (isPlayer && raceManager != null)
        {
            raceManager.OnRaceConfigChanged -= HandleRaceConfigChanged;
            raceManager.OnInputModeChanged -= HandleInputModeChanged;
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
        
        _movement.StartMoving();
        
        if (isPlayer && _input != null)
            _input.SetEnabled(true);
        
        if (raceManager != null)
        {
            raceManager.OnAthleteFinished += HandleAthleteFinished;
            raceManager.StartRace();
        }
        
        Debug.Log($"{athleteName} started race");
    }

    public void FinishRace()
    {
        if (!_raceActive || _hasFinishedRace) return;
        
        _hasFinishedRace = true;

        _movement.FinishRace();

        OnRaceFinished?.Invoke(this, _raceTime);

        Debug.Log($"{athleteName} finished in {_raceTime:F2}s");
    }

    private void HandleAthleteAtRest()
    {
        _raceActive = false;
        
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
    }

    private void HandleAthleteFinished(Athlete athlete, int finishOrder, float raceTime)
    {
        if (athlete == this)
        {
            FinishRace();
        }
    }

    public float RaceTime => _raceTime;
    public bool HasFinishedRace => _hasFinishedRace;
    public bool IsAtRest => !_movement.IsMoving && _hasFinishedRace;
}
