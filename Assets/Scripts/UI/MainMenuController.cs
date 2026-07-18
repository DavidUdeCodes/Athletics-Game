using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private EventSelectorPanel eventSelectorPanel;
    [SerializeField] private SettingsPanel settingsPanel;

    [Header("Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button eventSelectorButton;
    [SerializeField] private Button settingsButton;

    private void Awake()
    {
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayButtonPressed);

        if (eventSelectorButton != null)
            eventSelectorButton.onClick.AddListener(OnEventSelectorButtonPressed);

        if (settingsButton != null)
            settingsButton.onClick.AddListener(OnSettingsButtonPressed);
    }

    private void Start()
    {
        InitializePanels();
    }

    private void InitializePanels()
    {
        if (eventSelectorPanel == null)
            Debug.LogError($"{gameObject.name}: EventSelectorPanel not assigned to MainMenuController in Inspector");

        if (settingsPanel == null)
            Debug.LogError($"{gameObject.name}: SettingsPanel not assigned to MainMenuController in Inspector");

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
        SceneTransitionManager.Instance.LoadScene(SceneNames.TrackScene);
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
