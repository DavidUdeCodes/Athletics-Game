using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public enum RaceDistance { Distance100m = 100, Distance200m = 200, Distance400m = 400 }
public enum SprintInputMode { Rhythm, ForceControl }

public class RaceManager : MonoBehaviour
{
    [Header("Race Configuration")]
    [SerializeField] [Tooltip("Distance of the race")]
    private RaceDistance raceDistance = RaceDistance.Distance100m;
    [SerializeField] [Tooltip("Starting lane for player (1-8)")]
    private int playerLane = 1;

    [Space]
    [Header("Sprint Input Mode")]
    [SerializeField] [Tooltip("Current input mode: Rhythm or Force Control")]
    private SprintInputMode currentInputMode = SprintInputMode.Rhythm;
    
    [Space]
    [Header("Start Sequence Timing")]
    [SerializeField] [Tooltip("Minimum delay after On Your Marks (seconds)")]
    private float minOnYourMarksDelay = 1.25f;
    [SerializeField] [Tooltip("Maximum delay after On Your Marks (seconds)")]
    private float maxOnYourMarksDelay = 2.0f;
    [SerializeField] [Tooltip("Minimum delay after Get Set (seconds)")]
    private float minGetSetDelay = 1.2f;
    [SerializeField] [Tooltip("Maximum delay after Get Set (seconds)")]
    private float maxGetSetDelay = 2.3f;
    
    [Space]
    [Header("Reaction Time Thresholds")]
    [SerializeField] [Tooltip("Perfect start reaction time (seconds)")]
    private float perfectStartThreshold = 0.15f;
    [SerializeField] [Tooltip("Great start reaction time (seconds)")]
    private float greatStartThreshold = 0.25f;
    [SerializeField] [Tooltip("Good start reaction time (seconds)")]
    private float goodStartThreshold = 0.35f;
    [SerializeField] [Tooltip("Slow start reaction time (seconds)")]
    private float slowStartThreshold = 0.50f;
    
    [Space]
    [Header("Starting Velocity Bonus")]
    [SerializeField] [Tooltip("Momentum bonus for Perfect Start")]
    private float perfectStartMomentumBonus = 0.2f;
    [SerializeField] [Tooltip("Momentum bonus for Great Start")]
    private float greatStartMomentumBonus = 0.15f;
    [SerializeField] [Tooltip("Momentum bonus for Good Start")]
    private float goodStartMomentumBonus = 0.1f;
    [SerializeField] [Tooltip("Momentum bonus for Slow Start")]
    private float slowStartMomentumBonus = 0.05f;
    
    [Space]
    [Header("Dependencies")]
    [SerializeField] [Tooltip("Track manager providing track configurations")]
    private TrackManager trackManager;
    
    private RaceConfiguration _currentRaceConfig;
    private Dictionary<Athlete, bool> _athleteFinished = new();
    private Dictionary<Athlete, bool> _athleteAtRest = new();
    private Dictionary<Athlete, int> _athleteFinishOrder = new();
    private Dictionary<Athlete, float> _athleteReactionTimes = new();
    private Dictionary<Athlete, ReactionQuality> _athleteReactionQualities = new();
    private int _finishCounter = 0;
    private bool _raceActive = false;
    private bool _raceFinished = false;
    
    private RaceStartState _currentStartState = RaceStartState.Idle;
    private float _startSequenceTimer = 0f;
    private float _reactionTimer = 0f;
    private bool _reactionTimeRecorded = false;
    
    public event Action<Athlete, int, float> OnAthleteFinished;
    public event Action<Athlete> OnAthleteAtRest;
    public event Action OnRaceFinished;
    public event Action<RaceConfiguration> OnRaceConfigChanged;
    public event Action<SprintInputMode> OnInputModeChanged;
    public event Action<RaceStartState> OnRaceStartStateChanged;
    public event Action OnFalseStart;
    
    public RaceConfiguration CurrentRaceConfig => _currentRaceConfig;
    public bool IsRaceActive => _raceActive;
    public bool IsRaceFinished => _raceFinished;
    public float RaceDistanceInMeters => (float)raceDistance;
    public int PlayerLane => playerLane;
    public SprintInputMode CurrentInputMode => currentInputMode;
    public RaceStartState CurrentStartState => _currentStartState;
    
    private void OnValidate()
    {
        if (playerLane < 1) playerLane = 1;
        if (playerLane > 8) playerLane = 8;
    }
    
    private void Start()
    {
        if (trackManager == null)
        {
            trackManager = FindAnyObjectByType<TrackManager>();
            if (trackManager == null)
            {
                Debug.LogError("TrackManager not found in scene");
                return;
            }
        }
        
        SetupRace(raceDistance, playerLane);
        
        BeginRaceStart();
    }
    
    public void SetupRace(RaceDistance distance, int lane)
    {
        raceDistance = distance;
        playerLane = Mathf.Clamp(lane, 1, 8);
        
        TrackConfiguration track = trackManager.GetTrackForRace(distance);
        
        if (!track.IsValid)
        {
            Debug.LogError($"Track configuration invalid for race distance {distance}");
            return;
        }
        
        _currentRaceConfig = new RaceConfiguration(distance, lane, track);
        
        Debug.Log($"Race configured: {distance}m, Lane {lane}");
        OnRaceConfigChanged?.Invoke(_currentRaceConfig);
    }
    
    public void SetInputMode(SprintInputMode mode)
    {
        currentInputMode = mode;
        OnInputModeChanged?.Invoke(mode);
    }
    
    public void BeginRaceStart()
    {
        if (_currentStartState != RaceStartState.Idle)
            return;

        _athleteFinished.Clear();
        _athleteAtRest.Clear();
        _athleteFinishOrder.Clear();
        _athleteReactionTimes.Clear();
        _athleteReactionQualities.Clear();
        _finishCounter = 0;
        
        StartCoroutine(RaceStartSequence());
    }

    private IEnumerator RaceStartSequence()
    {
        SetRaceStartState(RaceStartState.OnYourMarks);
        yield return new WaitForSeconds(UnityEngine.Random.Range(minOnYourMarksDelay, maxOnYourMarksDelay));
        
        SetRaceStartState(RaceStartState.GetSet);
        yield return new WaitForSeconds(UnityEngine.Random.Range(minGetSetDelay, maxGetSetDelay));
        
        SetRaceStartState(RaceStartState.Go);
        
        _reactionTimer = 0f;
        _reactionTimeRecorded = false;
        
        yield return new WaitForSeconds(5f);
    }

    private void SetRaceStartState(RaceStartState newState)
    {
        if (_currentStartState == newState)
            return;

        RaceStartState previousState = _currentStartState;
        _currentStartState = newState;

        OnRaceStartStateChanged?.Invoke(newState);
        RaceStartEvents.RaiseRaceStateChanged(newState);

        NotifyAthletesOfStateChange(previousState, newState);

        if (newState == RaceStartState.Running)
        {
            _raceActive = true;
            _reactionTimeRecorded = false;
        }
    }

    private void NotifyAthletesOfStateChange(RaceStartState previousState, RaceStartState newState)
    {
        Athlete[] allAthletes = FindObjectsByType<Athlete>();
        
        foreach (Athlete athlete in allAthletes)
        {
            if (newState == RaceStartState.GetSet)
            {
                athlete.EnterGetSetState();
            }
            else if (newState == RaceStartState.Go)
            {
                athlete.EnterGoState();
            }
            else if (newState == RaceStartState.Running)
            {
                athlete.EnterRunningState();
            }
        }
    }

    public void HandleFalseStart(Athlete athlete)
    {
        if (_currentStartState != RaceStartState.GetSet)
            return;

        StartCoroutine(FalseStartSequence());
    }

    private IEnumerator FalseStartSequence()
    {
        _currentStartState = RaceStartState.FalseStart;
        OnFalseStart?.Invoke();
        RaceStartEvents.RaiseFalseStart();

        Athlete[] allAthletes = FindObjectsByType<Athlete>();
        foreach (Athlete athlete in allAthletes)
        {
            athlete.ResetForFalseStart();
        }

        yield return new WaitForSeconds(1.0f);

        SetRaceStartState(RaceStartState.Idle);
        
        yield return new WaitForSeconds(0.5f);

        yield return StartCoroutine(RaceStartSequence());
    }

    public void RecordReactionTime(Athlete athlete)
    {
        if (_currentStartState != RaceStartState.Go || _reactionTimeRecorded)
            return;

        if (_reactionTimer < 0.001f)
        {
            _reactionTimer = Time.deltaTime;
        }

        _athleteReactionTimes[athlete] = _reactionTimer;
        
        ReactionQuality quality = DetermineReactionQuality(_reactionTimer);
        _athleteReactionQualities[athlete] = quality;

        _reactionTimeRecorded = true;
        
        RaceStartEvents.RaiseReactionTimed(_reactionTimer);
        RaceStartEvents.RaiseReactionQualityDetermined(quality, _reactionTimer);

        ApplyStartingVelocityBonus(athlete, quality);

        SetRaceStartState(RaceStartState.Running);
    }

    private ReactionQuality DetermineReactionQuality(float reactionTime)
    {
        if (reactionTime <= perfectStartThreshold)
            return ReactionQuality.PerfectStart;
        if (reactionTime <= greatStartThreshold)
            return ReactionQuality.GreatStart;
        if (reactionTime <= goodStartThreshold)
            return ReactionQuality.GoodStart;
        if (reactionTime <= slowStartThreshold)
            return ReactionQuality.SlowStart;
        
        return ReactionQuality.VerySlowStart;
    }

    private void ApplyStartingVelocityBonus(Athlete athlete, ReactionQuality quality)
    {
        float momentumBonus = quality switch
        {
            ReactionQuality.PerfectStart => perfectStartMomentumBonus,
            ReactionQuality.GreatStart => greatStartMomentumBonus,
            ReactionQuality.GoodStart => goodStartMomentumBonus,
            ReactionQuality.SlowStart => slowStartMomentumBonus,
            _ => 0f
        };

        if (momentumBonus > 0f)
        {
            athlete.ApplyStartingMomentumBonus(momentumBonus);
        }
    }

    public void StartRace()
    {
        _raceActive = true;
        _raceFinished = false;
        Debug.Log($"Race started: {_currentRaceConfig.RaceDistance}m");
    }
    
    public void StopRace()
    {
        _raceActive = false;
    }
    
    public void CheckForAthleteFinish(Athlete athlete, float distanceTravelled)
    {
        if (!_raceActive || athlete == null) return;
        
        if (_athleteFinished.ContainsKey(athlete) && _athleteFinished[athlete])
            return;
        
        float finishDistance = _currentRaceConfig.GetFinishDistance();
        
        if (distanceTravelled >= finishDistance)
        {
            _finishCounter++;
            _athleteFinished[athlete] = true;
            _athleteFinishOrder[athlete] = _finishCounter;
            
            OnAthleteFinished?.Invoke(athlete, _finishCounter, athlete.RaceTime);
            
            Debug.Log($"{athlete.athleteName} finished in position {_finishCounter} with time {athlete.RaceTime:F2}s");
        }
    }

    public void RegisterAthleteAtRest(Athlete athlete)
    {
        if (athlete == null) return;
        
        if (_athleteAtRest.ContainsKey(athlete))
            return;
        
        _athleteAtRest[athlete] = true;
        OnAthleteAtRest?.Invoke(athlete);
        
        Debug.Log($"{athlete.athleteName} is now at rest");
        
        CheckIfRaceFinished();
    }

    private void CheckIfRaceFinished()
    {
        if (_raceFinished) return;
        
        Athlete[] allAthletes = FindObjectsByType<Athlete>();
        
        if (allAthletes.Length == 0) return;
        
        foreach (Athlete athlete in allAthletes)
        {
            if (!_athleteAtRest.ContainsKey(athlete))
                return;
        }
        
        _raceFinished = true;
        _currentStartState = RaceStartState.Finished;
        OnRaceFinished?.Invoke();
        Debug.Log("Race finished - all athletes at rest");
    }
    
    public int GetAthleteFinishOrder(Athlete athlete)
    {
        return _athleteFinishOrder.ContainsKey(athlete) ? _athleteFinishOrder[athlete] : -1;
    }
    
    public bool HasAthleteFinished(Athlete athlete)
    {
        return _athleteFinished.ContainsKey(athlete) && _athleteFinished[athlete];
    }

    public bool IsAthleteAtRest(Athlete athlete)
    {
        return _athleteAtRest.ContainsKey(athlete) && _athleteAtRest[athlete];
    }

    public float GetAthleteReactionTime(Athlete athlete)
    {
        return _athleteReactionTimes.ContainsKey(athlete) ? _athleteReactionTimes[athlete] : -1f;
    }

    public ReactionQuality GetAthleteReactionQuality(Athlete athlete)
    {
        return _athleteReactionQualities.ContainsKey(athlete) ? _athleteReactionQualities[athlete] : ReactionQuality.VerySlowStart;
    }
    
    public void SetRaceDistance(RaceDistance distance)
    {
        SetupRace(distance, playerLane);
    }
    
    public void SetPlayerLane(int lane)
    {
        playerLane = Mathf.Clamp(lane, 1, 8);
        if (_currentRaceConfig != null && _currentRaceConfig.IsValid)
        {
            _currentRaceConfig.PlayerLane = lane;
            Debug.Log($"Player lane changed to {lane}");
            OnRaceConfigChanged?.Invoke(_currentRaceConfig);
        }
    }

    private void Update()
    {
        if (_currentStartState == RaceStartState.Go && !_reactionTimeRecorded)
        {
            _reactionTimer += Time.deltaTime;
        }
    }
}
