using UnityEngine;

public class SprintInputModeUIManager : MonoBehaviour
{
    [SerializeField] private RhythmInputUI rhythmUI;
    [SerializeField] private ForceControlInputUI forceControlUI;
    [SerializeField] private RaceManager raceManager;

    private ISprintInputModeUI _currentUI;

    private void Start()
    {
        if (raceManager == null)
        {
            raceManager = FindAnyObjectByType<RaceManager>();
        }

        if (raceManager != null)
        {
            raceManager.OnInputModeChanged += HandleInputModeChanged;
        }

        InitializeUIForMode(raceManager != null ? raceManager.CurrentInputMode : SprintInputMode.Rhythm);
    }

    private void OnDestroy()
    {
        if (raceManager != null)
        {
            raceManager.OnInputModeChanged -= HandleInputModeChanged;
        }
    }

    private void HandleInputModeChanged(SprintInputMode newMode)
    {
        InitializeUIForMode(newMode);
    }

    private void InitializeUIForMode(SprintInputMode mode)
    {
        HideAllUI();

        _currentUI = mode switch
        {
            SprintInputMode.Rhythm => rhythmUI,
            SprintInputMode.ForceControl => forceControlUI,
            _ => rhythmUI
        };

        if (_currentUI != null)
        {
            _currentUI.Show();
        }
    }

    private void HideAllUI()
    {
        if (rhythmUI != null)
            rhythmUI.Hide();
        if (forceControlUI != null)
            forceControlUI.Hide();
    }

    public ISprintInputModeUI GetCurrentUI() => _currentUI;
}
