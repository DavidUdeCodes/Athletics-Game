using UnityEngine;
using System;
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
    [Header("Dependencies")]
    [SerializeField] [Tooltip("Track manager providing track configurations")]
    private TrackManager trackManager;
    
    private RaceConfiguration _currentRaceConfig;
    private Dictionary<Athlete, bool> _athleteFinished = new();
    private Dictionary<Athlete, bool> _athleteAtRest = new();
    private Dictionary<Athlete, int> _athleteFinishOrder = new();
    private int _finishCounter = 0;
    private bool _raceActive = false;
    private bool _raceFinished = false;
    
    public event Action<Athlete, int, float> OnAthleteFinished;
    public event Action<Athlete> OnAthleteAtRest;
    public event Action OnRaceFinished;
    public event Action<RaceConfiguration> OnRaceConfigChanged;
    public event Action<SprintInputMode> OnInputModeChanged;
    
    public RaceConfiguration CurrentRaceConfig => _currentRaceConfig;
    public bool IsRaceActive => _raceActive;
    public bool IsRaceFinished => _raceFinished;
    public float RaceDistanceInMeters => (float)raceDistance;
    public int PlayerLane => playerLane;
    public SprintInputMode CurrentInputMode => currentInputMode;
    
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
    
    public void StartRace()
    {
        _raceActive = true;
        _raceFinished = false;
        _athleteFinished.Clear();
        _athleteAtRest.Clear();
        _athleteFinishOrder.Clear();
        _finishCounter = 0;
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
    
    public void SetRaceDistance(RaceDistance distance)
    {
        SetupRace(distance, playerLane);
    }
    
    public void SetPlayerLane(int lane)
    {
        playerLane = Mathf.Clamp(lane, 1, 8);
        if (_currentRaceConfig != null && _currentRaceConfig.IsValid)
        {
            _currentRaceConfig.PlayerLane = playerLane;
            Debug.Log($"Player lane changed to {lane}");
            OnRaceConfigChanged?.Invoke(_currentRaceConfig);
        }
    }
}
