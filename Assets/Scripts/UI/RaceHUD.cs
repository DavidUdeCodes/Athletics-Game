using UnityEngine;
using TMPro;
using System;

public class RaceHUD : MonoBehaviour
{
    [Header("Timer Display")]
    [SerializeField] private TextMeshProUGUI timerDisplay;

    [Header("Speed Display")]
    [SerializeField] private TextMeshProUGUI speedDisplay;

    [Header("Position Display")]
    [SerializeField] private TextMeshProUGUI positionDisplay;
    [SerializeField] [Tooltip("How often (seconds) the race position label is recalculated. 0.1 s is imperceptible to players and avoids per-frame overhead.")]
    private float positionUpdateInterval = 0.1f;

    [Space]
    [Header("References")]
    [SerializeField] private RaceManager raceManager;
    [SerializeField] private RaceTimer raceTimer;

    private CanvasGroup _canvasGroup;
    private Athlete _playerAthlete;
    private bool _playerFinished;
    private string _frozenTimeDisplay;
    private float _positionTimer;
    private bool _raceRunning;

    private Athlete PlayerAthlete
    {
        get
        {
            if (_playerAthlete == null && raceManager != null)
                _playerAthlete = raceManager.PlayerAthlete;
            return _playerAthlete;
        }
    }

    private void Start()
    {
        InitializeReferences();
        InitializeCanvasGroup();
        SubscribeToEvents();
        ShowHUD();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void InitializeReferences()
    {
        if (raceManager == null)
            Debug.LogError($"{gameObject.name}: RaceManager not assigned to RaceHUD in Inspector");

        if (raceTimer == null)
            Debug.LogError($"{gameObject.name}: RaceTimer not assigned to RaceHUD in Inspector");
    }

    private void InitializeCanvasGroup()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
        {
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void SubscribeToEvents()
    {
        if (raceManager != null)
        {
            raceManager.OnRaceStartStateChanged += HandleRaceStateChanged;
            raceManager.OnPlayerFinished += HandlePlayerFinished;
        }

        if (raceTimer != null)
        {
            raceTimer.OnTimerUpdated += HandleTimerUpdated;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (raceManager != null)
        {
            raceManager.OnRaceStartStateChanged -= HandleRaceStateChanged;
            raceManager.OnPlayerFinished -= HandlePlayerFinished;
        }

        if (raceTimer != null)
        {
            raceTimer.OnTimerUpdated -= HandleTimerUpdated;
        }
    }

    private void Update()
    {
        if (raceTimer != null && raceTimer.IsRunning)
        {
            UpdateDisplay();
        }

        if (_raceRunning && !_playerFinished)
        {
            _positionTimer += Time.deltaTime;
            if (_positionTimer >= positionUpdateInterval)
            {
                _positionTimer = 0f;
                UpdateRacePosition();
            }
        }
    }

    private void UpdateDisplay()
    {
        UpdateTimer();
        UpdateSpeed();
    }

    private void UpdateTimer()
    {
        if (timerDisplay == null || raceTimer == null)
            return;

        timerDisplay.text = _playerFinished ? _frozenTimeDisplay : raceTimer.GetFormattedTime();
    }

    private void UpdateSpeed()
    {
        if (speedDisplay == null || PlayerAthlete == null)
            return;

        float speed = PlayerAthlete.CurrentSpeed;
        speedDisplay.text = $"{speed:F2} m/s";
    }

    private void HandleRaceStateChanged(RaceStartState newState)
    {
        if (newState == RaceStartState.Running)
        {
            _raceRunning = true;
            ShowHUD();
        }
    }

    private void HandleTimerUpdated(string formattedTime)
    {
        if (timerDisplay != null && !_playerFinished)
        {
            timerDisplay.text = formattedTime;
        }
    }

    private void HandlePlayerFinished(Athlete athlete)
    {
        _playerFinished = true;
        _frozenTimeDisplay = raceTimer != null ? raceTimer.GetFormattedTime() : string.Empty;

        if (_canvasGroup != null)
        {
            _canvasGroup.interactable = false;
        }
    }

    private void UpdateRacePosition()
    {
        if (positionDisplay == null || raceManager == null || PlayerAthlete == null)
            return;

        float playerDistance = PlayerAthlete.CurrentDistance;
        int aheadCount = 0;

        foreach (Athlete athlete in raceManager.GetAllAthletes())
        {
            if (!athlete.isPlayer && athlete.CurrentDistance > playerDistance)
                aheadCount++;
        }

        int position = aheadCount + 1;
        positionDisplay.text = position + GetOrdinalSuffix(position);
    }

    private static string GetOrdinalSuffix(int n)
    {
        if (n % 100 >= 11 && n % 100 <= 13) return "th";
        return (n % 10) switch
        {
            1 => "st",
            2 => "nd",
            3 => "rd",
            _ => "th"
        };
    }

    public void ShowHUD()
    {
        if (_canvasGroup == null)
            return;

        _canvasGroup.alpha = 1f;
        _canvasGroup.interactable = true;
        _canvasGroup.blocksRaycasts = true;
    }

    public void HideHUD()
    {
        if (_canvasGroup == null)
            return;

        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
    }

    public void ShowTimer(bool show)
    {
        if (timerDisplay != null)
        {
            timerDisplay.gameObject.SetActive(show);
        }
    }

    public void ShowSpeed(bool show)
    {
        if (speedDisplay != null)
        {
            speedDisplay.gameObject.SetActive(show);
        }
    }
}