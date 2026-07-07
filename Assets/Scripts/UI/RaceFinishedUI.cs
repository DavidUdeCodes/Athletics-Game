using UnityEngine;
using TMPro;
using System;

public class RaceFinishedUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private RaceManager raceManager;

    public event Action OnRaceResultsDisplayed;

    private void Start()
    {
        if (raceManager == null)
        {
            raceManager = FindAnyObjectByType<RaceManager>();
        }

        if (raceManager != null)
        {
            raceManager.OnRaceFinished += HandleRaceFinished;
        }

        if (resultText != null)
        {
            resultText.text = "";
        }
    }

    private void OnDestroy()
    {
        if (raceManager != null)
        {
            raceManager.OnRaceFinished -= HandleRaceFinished;
        }
    }

    private void HandleRaceFinished()
    {
        DisplayRaceResults();
    }

    private void DisplayRaceResults()
    {
        if (resultText == null)
            return;

        string message = "Race Finished!";

        Athlete playerAthlete = FindAnyObjectByType<Athlete>();
        if (playerAthlete != null && playerAthlete.isPlayer)
        {
            int finishOrder = raceManager.GetAthleteFinishOrder(playerAthlete);
            float raceTime = playerAthlete.RaceTime;

            if (finishOrder > 0)
            {
                message = $"Finished in {GetOrdinalSuffix(finishOrder)} Place\nTime: {raceTime:F2}s";
            }
        }

        resultText.text = message;
        OnRaceResultsDisplayed?.Invoke();
    }

    private string GetOrdinalSuffix(int number)
    {
        if (number % 100 >= 11 && number % 100 <= 13)
            return number + "th";

        switch (number % 10)
        {
            case 1: return number + "st";
            case 2: return number + "nd";
            case 3: return number + "rd";
            default: return number + "th";
        }
    }
}
