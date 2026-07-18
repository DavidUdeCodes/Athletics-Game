using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ResultsScreen : MonoBehaviour
{
    [Header("Results Display")]
    [SerializeField] private Transform resultsTableContainer;
    [SerializeField] private ResultRow resultRowPrefab;

    [Space]
    [Header("Action Buttons")]
    [SerializeField] private Button playAgainButton;
    [SerializeField] private Button replayButton;
    [SerializeField] private Button mainMenuButton;

    [Space]
    [Header("References")]
    [SerializeField] private RaceManager raceManager;
    [SerializeField] private EventSessionManager sessionManager;

    private CanvasGroup _canvasGroup;
    private List<ResultRow> _resultRows = new List<ResultRow>();

    private void Start()
    {
        InitializeReferences();
        InitializeCanvasGroup();
        SetupButtonListeners();
        HideResultsScreen();
    }

    private void OnDestroy()
    {
        RemoveButtonListeners();
    }

    private void InitializeReferences()
    {
        if (raceManager == null)
            Debug.LogError($"{gameObject.name}: RaceManager not assigned to ResultsScreen in Inspector");

        if (sessionManager == null)
            sessionManager = EventSessionManager.Instance;
    }

    private void InitializeCanvasGroup()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
        {
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void SetupButtonListeners()
    {
        if (playAgainButton != null)
            playAgainButton.onClick.AddListener(OnPlayAgainPressed);

        if (replayButton != null)
            replayButton.onClick.AddListener(OnReplayPressed);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(OnMainMenuPressed);
    }

    private void RemoveButtonListeners()
    {
        if (playAgainButton != null)
            playAgainButton.onClick.RemoveListener(OnPlayAgainPressed);

        if (replayButton != null)
            replayButton.onClick.RemoveListener(OnReplayPressed);

        if (mainMenuButton != null)
            mainMenuButton.onClick.RemoveListener(OnMainMenuPressed);
    }

    public void ShowResults(RaceResult[] results)
    {
        ClearResultsTable();
        PopulateResultsTable(results);
        ShowResultsScreen();
    }

    private void ClearResultsTable()
    {
        foreach (var row in _resultRows)
        {
            Destroy(row.gameObject);
        }
        _resultRows.Clear();
    }

    private void PopulateResultsTable(RaceResult[] results)
    {
        if (resultsTableContainer == null || resultRowPrefab == null)
        {
            Debug.LogWarning("ResultsScreen: Results table container or prefab not assigned");
            return;
        }

        foreach (RaceResult result in results)
        {
            ResultRow rowInstance = Instantiate(resultRowPrefab, resultsTableContainer);
            rowInstance.SetResultData(result);
            _resultRows.Add(rowInstance);
        }
    }

    private void ShowResultsScreen()
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
        }
    }

    private void HideResultsScreen()
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }
    }

    private void OnPlayAgainPressed()
    {
        Time.timeScale = 1f;
        SceneTransitionManager.Instance.ReloadCurrentScene();
    }

    private void OnReplayPressed()
    {
        Debug.Log("Replay button pressed - functionality will be implemented later");
    }

    private void OnMainMenuPressed()
    {
        Time.timeScale = 1f;
        SceneTransitionManager.Instance.LoadScene(SceneNames.MainMenu);
    }
}
