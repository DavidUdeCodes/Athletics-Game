using UnityEngine;
using System.Collections;
using TMPro;
using System;

public class RaceFinishFlowManager : MonoBehaviour
{
    [Header("Finish Sequence")]
    [SerializeField] [Tooltip("Delay before showing results screen (seconds)")]
    private float delayBeforeResults = 3.0f;

    [Space]
    [Header("Finish Message Display")]
    [SerializeField] private TextMeshProUGUI finishedMessageDisplay;
    [SerializeField] [Tooltip("Duration of finish message animation")]
    private float finishMessageDuration = 1.0f;

    [Space]
    [Header("References")]
    [SerializeField] private RaceManager raceManager;
    [SerializeField] private ResultsScreen resultsScreen;

    private CanvasGroup _messageCanvasGroup;
    private bool _finishSequenceActive = false;

    public event Action OnFinishSequenceStarted;
    public event Action OnFinishSequenceComplete;

    private void Start()
    {
        InitializeReferences();
        InitializeCanvasGroup();
        SubscribeToEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void InitializeReferences()
    {
        if (raceManager == null)
            raceManager = FindAnyObjectByType<RaceManager>();

        if (resultsScreen == null)
            resultsScreen = FindAnyObjectByType<ResultsScreen>();
    }

    private void InitializeCanvasGroup()
    {
        if (finishedMessageDisplay != null)
        {
            _messageCanvasGroup = finishedMessageDisplay.GetComponent<CanvasGroup>();
            if (_messageCanvasGroup == null)
            {
                _messageCanvasGroup = finishedMessageDisplay.gameObject.AddComponent<CanvasGroup>();
            }
            _messageCanvasGroup.alpha = 0f;
        }
    }

    private void SubscribeToEvents()
    {
        if (raceManager != null)
        {
            raceManager.OnPlayerFinished += HandlePlayerFinished;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (raceManager != null)
        {
            raceManager.OnPlayerFinished -= HandlePlayerFinished;
        }
    }

    private void HandlePlayerFinished(Athlete athlete)
    {
        if (!_finishSequenceActive)
        {
            StartCoroutine(PlayFinishSequence());
        }
    }

    private IEnumerator PlayFinishSequence()
    {
        _finishSequenceActive = true;
        OnFinishSequenceStarted?.Invoke();

        yield return StartCoroutine(DisplayFinishedMessage());

        yield return new WaitForSeconds(delayBeforeResults);

        ShowResultsScreen();

        OnFinishSequenceComplete?.Invoke();
    }

    private IEnumerator DisplayFinishedMessage()
    {
        if (finishedMessageDisplay == null || _messageCanvasGroup == null)
            yield break;

        finishedMessageDisplay.text = "FINISHED";

        float elapsed = 0f;
        while (elapsed < finishMessageDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / finishMessageDuration;
            _messageCanvasGroup.alpha = Mathf.Clamp01(t);
            yield return null;
        }

        _messageCanvasGroup.alpha = 1f;

        yield return new WaitForSeconds(delayBeforeResults * 0.3f);

        elapsed = 0f;
        while (elapsed < finishMessageDuration * 0.5f)
        {
            elapsed += Time.deltaTime;
            float t = 1f - (elapsed / (finishMessageDuration * 0.5f));
            
            float scale = Mathf.Lerp(1f, 0.8f, 1f - t);
            finishedMessageDisplay.transform.localScale = new Vector3(scale, scale, 1f);
            
            yield return null;
        }
    }

    private void ShowResultsScreen()
    {
        if (resultsScreen != null)
        {
            resultsScreen.ShowResults(GetRaceResults());
        }
        else
        {
            Debug.LogWarning("ResultsScreen not assigned to RaceFinishFlowManager");
        }
    }

    private RaceResult[] GetRaceResults()
    {
        if (raceManager == null)
            return new RaceResult[0];

        var results = raceManager.GetRaceResults();
        return results.ToArray();
    }

    public float GetDelayBeforeResults()
    {
        return delayBeforeResults;
    }

    public void SetDelayBeforeResults(float delay)
    {
        delayBeforeResults = Mathf.Max(0f, delay);
    }
}
