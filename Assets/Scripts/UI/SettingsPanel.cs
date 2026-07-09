using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsPanel : MonoBehaviour, IPanelUI
{
    [SerializeField] private CanvasGroup canvasGroup;
    
    [Header("Lane Settings")]
    [SerializeField] private TMP_Dropdown lanePreferenceDropdown;
    [SerializeField] private TMP_Dropdown fixedLaneDropdown;
    [SerializeField] private GameObject fixedLaneContainer;
    
    [Header("Input Mode Settings")]
    [SerializeField] private TMP_Dropdown inputModeDropdown;

    private LaneSelectionMode _laneMode = LaneSelectionMode.Random;
    private int _fixedLane = 1;
    private SprintInputMode _inputMode = SprintInputMode.Rhythm;

    private void OnEnable()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
    }

    public void Initialize()
    {
        InitializeLaneSettings();
        InitializeInputModeSettings();
    }

    private void InitializeLaneSettings()
    {
        if (lanePreferenceDropdown != null)
        {
            lanePreferenceDropdown.ClearOptions();
            lanePreferenceDropdown.AddOptions(new System.Collections.Generic.List<string> { "Random", "Fixed" });
            lanePreferenceDropdown.value = 0;
            lanePreferenceDropdown.onValueChanged.AddListener(OnLanePreferenceChanged);
        }

        if (fixedLaneDropdown != null)
        {
            fixedLaneDropdown.ClearOptions();
            var laneOptions = new System.Collections.Generic.List<string>();
            for (int i = 1; i <= 8; i++)
            {
                laneOptions.Add($"Lane {i}");
            }
            fixedLaneDropdown.AddOptions(laneOptions);
            fixedLaneDropdown.value = 0;
            fixedLaneDropdown.onValueChanged.AddListener(OnFixedLaneChanged);
        }

        if (fixedLaneContainer != null)
        {
            fixedLaneContainer.SetActive(false);
        }
    }

    private void InitializeInputModeSettings()
    {
        if (inputModeDropdown != null)
        {
            inputModeDropdown.ClearOptions();
            inputModeDropdown.AddOptions(new System.Collections.Generic.List<string> { "Rhythm", "Force Control" });
            inputModeDropdown.value = 0;
            inputModeDropdown.onValueChanged.AddListener(OnInputModeChanged);
        }
    }

    private void OnLanePreferenceChanged(int value)
    {
        _laneMode = value == 0 ? LaneSelectionMode.Random : LaneSelectionMode.Fixed;
        
        if (fixedLaneContainer != null)
        {
            fixedLaneContainer.SetActive(_laneMode == LaneSelectionMode.Fixed);
        }

        Debug.Log($"[SettingsPanel] Lane preference changed to: {_laneMode}");
    }

    private void OnFixedLaneChanged(int value)
    {
        _fixedLane = value + 1;
        Debug.Log($"[SettingsPanel] Fixed lane changed to: {_fixedLane}");
    }

    private void OnInputModeChanged(int value)
    {
        _inputMode = value == 0 ? SprintInputMode.Rhythm : SprintInputMode.ForceControl;
        Debug.Log($"[SettingsPanel] Input mode changed to: {_inputMode}");
    }

    public LaneSelectionMode GetLaneMode()
    {
        return _laneMode;
    }

    public int GetFixedLane()
    {
        return _fixedLane;
    }

    public SprintInputMode GetInputMode()
    {
        return _inputMode;
    }

    public void SetActive(bool active)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = active ? 1f : 0f;
            canvasGroup.blocksRaycasts = active;
            canvasGroup.interactable = active;
        }
        gameObject.SetActive(active);
    }
}
