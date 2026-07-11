using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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
    [Header("Dependencies")]
    [SerializeField] [Tooltip("Track manager providing track configurations")]
    private TrackManager trackManager;
    [SerializeField] [Tooltip("Race timer for tracking official times")]
    private RaceTimer raceTimer;
    [SerializeField] [Tooltip("Race start controller for managing start sequence")]
    private RaceStartController raceStartController;
    
    private RaceConfiguration _currentRaceConfig;
    private Dictionary<Athlete, bool> _athleteFinished = new();
    private Dictionary<Athlete, bool> _athleteAtRest = new();
    private Dictionary<Athlete, int> _athleteFinishOrder = new();
    private Dictionary<Athlete, float> _athleteFinishTimes = new();
    private int _finishCounter = 0;
    private bool _raceActive = false;
    private bool _raceFinished = false;
    private bool _playerHasFinished = false;
    private Athlete[] _cachedAllAthletes;
    private Athlete _playerAthlete;

    
    public event Action<Athlete, int, float> OnAthleteFinished;
    public event Action<Athlete> OnPlayerFinished;
    public event Action<Athlete> OnAthleteAtRest;
    public event Action OnRaceFinished;
    public event Action<RaceConfiguration> OnRaceConfigChanged;
    public event Action<SprintInputMode> OnInputModeChanged;
    public event Action<RaceStartState> OnRaceStartStateChanged;
    public event Action OnFalseStart;
    
    public RaceConfiguration CurrentRaceConfig => _currentRaceConfig;
    public bool IsRaceActive => _raceActive;
    public bool IsRaceFinished => _raceFinished;
    public bool HasPlayerFinished => _playerHasFinished;
    public float RaceDistanceInMeters => (float)raceDistance;
    public int PlayerLane => playerLane;
    public SprintInputMode CurrentInputMode => currentInputMode;
    public RaceStartState CurrentStartState => raceStartController != null ? raceStartController.CurrentStartState : RaceStartState.Idle;
    
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
        
        if (raceTimer == null)
        {
            raceTimer = FindAnyObjectByType<RaceTimer>();
            if (raceTimer == null)
            {
                GameObject timerGO = new GameObject("RaceTimer");
                raceTimer = timerGO.AddComponent<RaceTimer>();
            }
        }
        
        if (raceStartController == null)
        {
            raceStartController = FindAnyObjectByType<RaceStartController>();
            if (raceStartController == null)
            {
                Debug.LogError("RaceStartController not found in scene");
                return;
            }
        }
        
        SubscribeToRaceStartControllerEvents();
        
        if (EventSessionManager.Instance.HasConfig)
        {
            EventSessionConfig sessionConfig = EventSessionManager.Instance.CurrentConfig;
            raceDistance = sessionConfig.SelectedDistance;
            currentInputMode = sessionConfig.InputMode;
            playerLane = sessionConfig.GetPlayerLane(playerLane);
            Debug.Log($"[RaceManager] Using EventSessionConfig: {sessionConfig}");
        }
        
        SetupRace(raceDistance, playerLane);
        
        BeginRaceStart();
    }
    public Athlete PlayerAthlete
    {
        get
        {
            if (_playerAthlete == null)
            {
                _playerAthlete = GetAllAthletes().FirstOrDefault(a => a.isPlayer);
            }
            return _playerAthlete;
        }
    }
    private void SubscribeToRaceStartControllerEvents()
    {
        if (raceStartController == null) return;
        
        raceStartController.OnStartStateChanged += HandleRaceStartControllerStateChanged;
        raceStartController.OnFalseStart += HandleRaceStartControllerFalseStart;
        raceStartController.OnRaceOfficiallyStarted += HandleRaceStartControllerOfficiallyStarted;
    }
    
    private void HandleRaceStartControllerStateChanged(RaceStartState newState)
    {
        OnRaceStartStateChanged?.Invoke(newState);
    }
    
    private void HandleRaceStartControllerFalseStart()
    {
        OnFalseStart?.Invoke();
    }
    
    private void HandleRaceStartControllerOfficiallyStarted()
    {
        _raceActive = true;
        StartRace();
    }
    
    public Athlete[] GetAllAthletes()
    {
        if (_cachedAllAthletes == null || _cachedAllAthletes.Length == 0)
        {
            _cachedAllAthletes = FindObjectsByType<Athlete>();
        }
        return _cachedAllAthletes;
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
        if (raceStartController == null)
        return;
    
        _athleteFinished.Clear();
        _athleteAtRest.Clear();
        _athleteFinishOrder.Clear();
        _athleteFinishTimes.Clear();
        _finishCounter = 0;
        _playerHasFinished = false;
        _playerAthlete = null;          // <-- add this
        _cachedAllAthletes = null;      // <-- also this, since GetAllAthletes() caches too and has the same stale-reference risk
        
        raceTimer?.ResetTimer();
        
        Athlete[] athletes = GetAllAthletes();
        raceStartController.InitiateRaceStart(athletes);
    }
    
    public void HandleFalseStart(Athlete athlete)
    {
        if (raceStartController != null)
        {
            raceStartController.HandleFalseStart(athlete);
        }
    }
    
    public void RecordReactionTime(Athlete athlete)
    {
        if (raceStartController != null)
        {
            raceStartController.RecordReactionTime(athlete);
        }
    }

    public void StartRace()
    {
        _raceActive = true;
        _raceFinished = false;
        raceTimer?.StartTimer();
        if (_currentRaceConfig != null)
        {
            Debug.Log($"Race started: {_currentRaceConfig.RaceDistance}m");
        }
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
            
            float officialFinishTime = raceTimer != null ? raceTimer.ElapsedTime : athlete.RaceTime;
            _athleteFinishTimes[athlete] = officialFinishTime;
            
            OnAthleteFinished?.Invoke(athlete, _finishCounter, officialFinishTime);
            
            if (athlete.isPlayer)
            {
                _playerHasFinished = true;
                raceTimer?.StopTimer();
                OnPlayerFinished?.Invoke(athlete);
            }
            
            Debug.Log($"{athlete.athleteName} finished in position {_finishCounter} with time {officialFinishTime:F2}s");
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
        
        Athlete[] allAthletes = GetAllAthletes();
        
        if (allAthletes.Length == 0) return;
        
        foreach (Athlete athlete in allAthletes)
        {
            if (!_athleteAtRest.ContainsKey(athlete))
                return;
        }
        
        _raceFinished = true;
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

    public float GetAthleteFinishTime(Athlete athlete)
    {
        return _athleteFinishTimes.ContainsKey(athlete) ? _athleteFinishTimes[athlete] : -1f;
    }

   public float GetPlayerFinishTime()
    {
        return GetAthleteFinishTime(PlayerAthlete);
    }

    public List<RaceResult> GetRaceResults()
    {
        var results = new List<RaceResult>();

        foreach (var athlete in _athleteFinished.Keys)
        {
            if (!_athleteFinished[athlete])
                continue;

            int placement = _athleteFinishOrder[athlete];
            float finishTime = GetAthleteFinishTime(athlete);
            var result = new RaceResult(
                placement,
                athlete.athleteName,
                "Unknown",
                finishTime,
                athlete.isPlayer,
                athlete
            );
            results.Add(result);
        }

        results.Sort((a, b) => a.Placement.CompareTo(b.Placement));
        return results;
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
}
