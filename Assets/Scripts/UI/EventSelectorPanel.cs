using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class EventSelectorPanel : MonoBehaviour, IPanelUI
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Transform eventButtonContainer;
    [SerializeField] private GameObject eventButtonPrefab;
    [SerializeField] private EventManager eventManager;
    [SerializeField] private TMP_Text menuEventSelectText;
    

    private RaceDistance _selectedDistance = RaceDistance.Distance100m;

    [SerializeField] private Button backButton;

    private void OnEnable()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
            
        backButton.onClick.AddListener(() => SetActive(false));
    }

    private void OnDisable()
    {
        backButton.onClick.RemoveListener(() => SetActive(false));
    }

    public void Initialize()
    {
        if (eventManager == null)
        {
            Debug.LogError($"{gameObject.name}: EventManager not assigned to EventSelectorPanel in Inspector");
            return;
        }

        CreateEventButtons();
    }

    private void CreateEventButtons()
    {
        if (eventButtonContainer == null || eventButtonPrefab == null)
        {
            Debug.LogError("EventSelectorPanel: Button container or prefab not assigned");
            return;
        }

        foreach (Transform child in eventButtonContainer)
        {
            Destroy(child.gameObject);
        }

        var allEvents = eventManager.GetAllEvents();

        foreach (var eventDef in allEvents)
        {
            if (eventDef == null) continue;

            GameObject buttonGO = Instantiate(eventButtonPrefab, eventButtonContainer);
            Button button = buttonGO.GetComponent<Button>();
            TextMeshProUGUI buttonText = buttonGO.GetComponentInChildren<TextMeshProUGUI>();

            if (button == null || buttonText == null)
            {
                Debug.LogError($"EventSelectorPanel: Event button prefab missing Button or TextMeshProUGUI component");
                Destroy(buttonGO);
                continue;
            }

            buttonText.text = eventDef.EventName;
            RaceDistance buttonDistance = eventDef.Distance;

            button.onClick.AddListener(() => SelectEvent(buttonDistance));
        }
    }

    private void SelectEvent(RaceDistance distance)
    {
        _selectedDistance = distance;
        Debug.Log($"[EventSelectorPanel] Selected event: {distance}");
        menuEventSelectText.text = distance.ToString().Replace("Distance", "");
    }

    public RaceDistance GetSelectedDistance()
    {
        return _selectedDistance;
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
