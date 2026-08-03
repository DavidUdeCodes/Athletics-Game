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
    [SerializeField] [Tooltip("Starting blocks controller for spawning/removing starting blocks")]
    private StartingBlocksController startingBlocksController;
    [SerializeField] private RaceCameraController cameraController;
    [SerializeField] [Tooltip("Records race state for replay playback. Attach ReplayRecorder to this GameObject.")]
    private ReplayRecorder replayRecorder;

    [Space]
    [Header("Player Athlete")]
    [SerializeField] [Tooltip("Prefab for the player athlete. Must have Athlete (isPlayer=true), AthleteMovement, SplineMovement, SprintController, MomentumController, and AthleteAnimationController.")]
    private GameObject playerAthletePrefab;
    [SerializeField] [Tooltip("Rhythm input UI assigned to the player athlete at runtime.")]
    private RhythmInputUI playerRhythmInputUI;
    [SerializeField] [Tooltip("Force control input UI assigned to the player athlete at runtime.")]
    private ForceControlInputUI playerForceControlInputUI;

    [Space]
    [Header("AI Athletes")]
    [SerializeField] [Tooltip("Prefab used for every AI runner. Must have Athlete, AthleteMovement, SplineMovement, AISprinterController, and AthleteAnimationController.")]
    private GameObject aiAthletePrefab;
    [SerializeField] [Range(1, 7)] [Tooltip("Number of AI athletes to spawn (max 7 so the player fits in one of 8 lanes).")]
    private int aiCount = 7;
    [SerializeField] [Tooltip("Generates random names and nationalities for AI athletes each race.")]
    private AthleteIdentityGenerator identityGenerator;

    [Space]
    [Header("AI Difficulty")]
    [SerializeField] [Tooltip("Timing ranges per distance used to generate AI target finish times.")]
    private RaceDifficultyConfig difficultyConfig;
    [SerializeField] [Range(0f, 1f)] [Tooltip("0 = easiest (slowest AI), 1 = hardest (fastest AI).")]
    private float difficulty = 0.5f;

    private readonly List<GameObject> _spawnedAIAthletes = new();
    private GameObject _spawnedPlayerAthlete;

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

        if (replayRecorder == null)
            replayRecorder = GetComponent<ReplayRecorder>();

        replayRecorder?.Initialize(this);

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
        replayRecorder?.StartRecording();
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
        _playerAthlete = null;
        _cachedAllAthletes = null;

        raceTimer?.ResetTimer();

        // Destroy any previously spawned athletes before spawning fresh ones.
        DestroySpawnedAthletes();

        // Spawn player first so their position is known before blocks are placed.
        SpawnPlayerAthlete();

        // Spawn AI after the player is positioned.
        SpawnAIAthletes();

        Athlete[] athletes = GetAllAthletes();

        // All athletes are now initialized and positioned in their lanes — safe to
        // spawn starting blocks using their actual world transforms.
        startingBlocksController?.SpawnBlocksForAthletes(athletes);

        raceStartController.InitiateRaceStart(athletes);
    }

    private void SpawnAIAthletes()
    {
        if (aiAthletePrefab == null)
            return;

        int[] availableLanes = GetAvailableLanes();
        int count = Mathf.Min(aiCount, availableLanes.Length);

        for (int i = 0; i < count; i++)
        {
            int lane = availableLanes[i];

            GameObject go = Instantiate(aiAthletePrefab);

            Athlete aiAthlete = go.GetComponent<Athlete>();
            if (aiAthlete == null)
            {
                Debug.LogError($"[RaceManager] AI prefab '{aiAthletePrefab.name}' is missing an Athlete component.");
                Destroy(go);
                continue;
            }

            // Assign a generated identity (name + nationality) for this runner.
            if (identityGenerator != null)
            {
                AthleteIdentity identity = identityGenerator.Generate();
                aiAthlete.athleteName = identity.Name;
                aiAthlete.nationality = identity.Nationality;
            }

            go.name = $"AI_{aiAthlete.athleteName}";

            AISprinterController aiController = go.GetComponent<AISprinterController>();
            if (aiController != null)
            {
                if (difficultyConfig != null)
                {
                    aiController.SetTargetFinishTime(difficultyConfig.GetTargetTime(raceDistance, difficulty));
                }
                else
                {
                    Debug.LogWarning("[RaceManager] No RaceDifficultyConfig assigned. AI target times will use the inspector default.");
                }
            }

            // InjectRaceSetup subscribes to RaceManager events, sets the lane,
            // and positions the athlete on the spline — synchronously, before Start() runs.
            aiAthlete.InjectRaceSetup(this, _currentRaceConfig, lane);

            _spawnedAIAthletes.Add(go);
        }
    }

    private void SpawnPlayerAthlete()
    {
        if (playerAthletePrefab == null)
        {
            Debug.LogError("[RaceManager] playerAthletePrefab is not assigned. Player cannot be spawned.");
            return;
        }

        GameObject go = Instantiate(playerAthletePrefab);
        go.name = "Player";

        Athlete player = go.GetComponent<Athlete>();
        if (player == null)
        {
            Debug.LogError("[RaceManager] playerAthletePrefab is missing an Athlete component.");
            Destroy(go);
            return;
        }

        // Wire up player-specific UI before Start() runs.
        player.InjectPlayerUI(playerRhythmInputUI, playerForceControlInputUI);

        // InjectRaceSetup positions the athlete and subscribes to events — must
        // happen before Start() so the _raceSetupInjected flag is set correctly.
        player.InjectRaceSetup(this, _currentRaceConfig, playerLane);

        _spawnedPlayerAthlete = go;
        cameraController.FollowAthlete(_spawnedPlayerAthlete.GetComponent<Athlete>().transform);
    }

    private void DestroySpawnedAthletes()
    {
        if (_spawnedPlayerAthlete != null)
        {
            Destroy(_spawnedPlayerAthlete);
            _spawnedPlayerAthlete = null;
        }

        foreach (GameObject go in _spawnedAIAthletes)
        {
            if (go != null)
                Destroy(go);
        }
        _spawnedAIAthletes.Clear();
    }

    /// <summary>Returns all lane indices not currently occupied by the player.</summary>
    private int[] GetAvailableLanes()
    {
        var lanes = new List<int>();
        for (int i = 1; i <= 8; i++)
        {
            if (i != playerLane)
                lanes.Add(i);
        }
        return lanes.ToArray();
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
                // Do NOT stop the master timer here — AI athletes still need it
                // to record their own finish times. The HUD freezes its own display
                // locally. The timer is stopped once all athletes are at rest.
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
        raceTimer?.StopTimer();

        ReplayData replay = replayRecorder?.StopRecording(GetRaceResults());
        if (replay != null && ReplayManager.Instance != null)
            ReplayManager.Instance.SetCompletedReplay(replay);

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
                athlete.nationality,
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
