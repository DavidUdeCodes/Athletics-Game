using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private EventSelectorPanel eventSelectorPanel;
    [SerializeField] private SettingsPanel settingsPanel;
    [SerializeField] private string raceSceneName = "TrackScene";

    private void Start()
    {
        InitializePanels();
    }

    private void InitializePanels()
    {
        if (eventSelectorPanel == null)
        {
            eventSelectorPanel = FindAnyObjectByType<EventSelectorPanel>();
        }

        if (settingsPanel == null)
        {
            settingsPanel = FindAnyObjectByType<SettingsPanel>();
        }

        if (eventSelectorPanel != null)
        {
            eventSelectorPanel.Initialize();
            eventSelectorPanel.SetActive(false);
        }

        if (settingsPanel != null)
        {
            settingsPanel.Initialize();
            settingsPanel.SetActive(false);
        }
    }

    public void OnPlayButtonPressed()
    {
        if (eventSelectorPanel == null || settingsPanel == null)
        {
            Debug.LogError("MainMenuController: Panels not properly initialized");
            return;
        }

        RaceDistance selectedDistance = eventSelectorPanel.GetSelectedDistance();
        LaneSelectionMode laneMode = settingsPanel.GetLaneMode();
        int fixedLane = settingsPanel.GetFixedLane();
        SprintInputMode inputMode = settingsPanel.GetInputMode();

        EventSessionConfig config = new EventSessionConfig(selectedDistance, laneMode, fixedLane, inputMode);
        EventSessionManager.Instance.SetEventConfig(config);

        Debug.Log($"[MainMenuController] Starting race with config: {config}");
        SceneManager.LoadScene(raceSceneName);
    }

    public void OnEventSelectorButtonPressed()
    {
        ShowPanel(eventSelectorPanel, settingsPanel);
    }

    public void OnSettingsButtonPressed()
    {
        ShowPanel(settingsPanel, eventSelectorPanel);
    }

    private void ShowPanel(MonoBehaviour panelToShow, MonoBehaviour panelToHide)
    {
        if (panelToHide is IPanelUI panelUI)
            panelUI.SetActive(false);

        if (panelToShow is IPanelUI showPanelUI)
            showPanelUI.SetActive(true);
    }
}
