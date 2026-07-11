using UnityEngine;
using TMPro;
using System;

public class RaceHUD : MonoBehaviour
{
    [Header("Timer Display")]
    [SerializeField] private TextMeshProUGUI timerDisplay;

    [Header("Speed Display")]
    [SerializeField] private TextMeshProUGUI speedDisplay;

    [Space]
    [Header("References")]
    [SerializeField] private RaceManager raceManager;
    [SerializeField] private RaceTimer raceTimer;

    private CanvasGroup _canvasGroup;
    private Athlete _playerAthlete;

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

        timerDisplay.text = raceTimer.GetFormattedTime();
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
            ShowHUD();
        }
    }

    private void HandleTimerUpdated(string formattedTime)
    {
        if (timerDisplay != null)
        {
            timerDisplay.text = formattedTime;
        }
    }

    private void HandlePlayerFinished(Athlete athlete)
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.interactable = false;
        }
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