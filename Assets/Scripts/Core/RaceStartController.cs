using UnityEngine;
using System;
using System.Collections;

public class RaceStartController : MonoBehaviour
{
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
    [Header("False Start Restart")]
    [SerializeField] [Tooltip("Delay before reloading scene on false start (seconds)")]
    private float falseStartRestartDelay = 2.0f;
    
    [Space]
    [Header("Dependencies")]
    [SerializeField] [Tooltip("Race manager for coordination and notifications")]
    private RaceManager raceManager;
    [SerializeField] [Tooltip("Screen effects controller for visual feedback")]
    private ScreenEffectsController screenEffects;
    
    private Athlete[] _allAthletes;
    private RaceStartState _currentStartState = RaceStartState.Idle;
    private float _reactionTimer = 0f;
    private bool _reactionTimeRecorded = false;
    private Coroutine _raceStartSequenceCoroutine;
    
    public event Action<RaceStartState> OnStartStateChanged;
    public event Action OnFalseStart;
    public event Action OnRaceOfficiallyStarted;
    
    public RaceStartState CurrentStartState => _currentStartState;
    
    private void Start()
    {
        if (raceManager == null)
        {
            Debug.LogError($"{gameObject.name}: RaceManager not assigned to RaceStartController in Inspector");
        }
        
        if (screenEffects == null)
        {
            screenEffects = FindAnyObjectByType<ScreenEffectsController>();
        }
    }
    
    private void Update()
    {
        if (_currentStartState == RaceStartState.Go && !_reactionTimeRecorded)
        {
            _reactionTimer += Time.deltaTime;
        }
    }
    
    public void InitiateRaceStart(Athlete[] athletes)
    {
        if (_currentStartState != RaceStartState.Idle)
            return;
        
        _allAthletes = athletes;
        _reactionTimer = 0f;
        _reactionTimeRecorded = false;
        
        _raceStartSequenceCoroutine = StartCoroutine(RaceStartSequence());
    }
    
    private IEnumerator RaceStartSequence()
    {
        SetStartState(RaceStartState.OnYourMarks);
        yield return new WaitForSeconds(UnityEngine.Random.Range(minOnYourMarksDelay, maxOnYourMarksDelay));
        
        SetStartState(RaceStartState.GetSet);
        yield return new WaitForSeconds(UnityEngine.Random.Range(minGetSetDelay, maxGetSetDelay));
        
        SetStartState(RaceStartState.Go);
        
        _reactionTimer = 0f;
        _reactionTimeRecorded = false;
        
        yield return new WaitForSeconds(5f);
    }
    
    private void SetStartState(RaceStartState newState)
    {
        if (_currentStartState == newState)
            return;
        
        _currentStartState = newState;
        OnStartStateChanged?.Invoke(newState);
        RaceStartEvents.RaiseRaceStateChanged(newState);
        
        NotifyAthletesOfStateChange(newState);
        
        if (newState == RaceStartState.Go)
        {
            if (screenEffects != null)
            {
                screenEffects.PlayGoFlash();
            }
        }
        
        if (newState == RaceStartState.Running)
        {
            _reactionTimeRecorded = false;
            OnRaceOfficiallyStarted?.Invoke();
        }
    }
    
    private void NotifyAthletesOfStateChange(RaceStartState newState)
    {
        if (_allAthletes == null) return;
        
        foreach (Athlete athlete in _allAthletes)
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
        
        if (_raceStartSequenceCoroutine != null)
        {
            StopCoroutine(_raceStartSequenceCoroutine);
            _raceStartSequenceCoroutine = null;
        }
        
        StartCoroutine(FalseStartSequence());
    }
    
    private IEnumerator FalseStartSequence()
    {
        _currentStartState = RaceStartState.FalseStart;
        OnFalseStart?.Invoke();
        RaceStartEvents.RaiseFalseStart();
        
        if (screenEffects != null)
        {
            screenEffects.PlayFalseStartFlash();
        }
        
        SceneTransitionManager.Instance.ReloadCurrentScene(falseStartRestartDelay);
        
        yield break;
    }
    
    public void RecordReactionTime(Athlete athlete)
    {
        if (_currentStartState != RaceStartState.Go || _reactionTimeRecorded)
            return;
        
        if (_reactionTimer < 0.001f)
        {
            _reactionTimer = Time.deltaTime;
        }
        
        ReactionQuality quality = DetermineReactionQuality(_reactionTimer);
        
        _reactionTimeRecorded = true;
        
        RaceStartEvents.RaiseReactionTimed(_reactionTimer);
        RaceStartEvents.RaiseReactionQualityDetermined(quality, _reactionTimer);
        
        ApplyStartingVelocityBonus(athlete, quality);
        
        SetStartState(RaceStartState.Running);
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
    
    public float GetReactionTime(Athlete athlete)
    {
        return _reactionTimeRecorded ? _reactionTimer : -1f;
    }
    
    public bool HasReactionTimeBeenRecorded()
    {
        return _reactionTimeRecorded;
    }
}
