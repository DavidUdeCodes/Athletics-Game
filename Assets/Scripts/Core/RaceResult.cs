using UnityEngine;

public class RaceResult
{
    public int Placement { get; set; }
    public string AthleteName { get; set; }
    public string Nationality { get; set; }
    public float FinishTime { get; set; }
    public bool IsPlayer { get; set; }
    public Athlete AthleteReference { get; set; }

    public RaceResult(int placement, string athleteName, string nationality, float finishTime, bool isPlayer, Athlete athleteRef = null)
    {
        Placement = placement;
        AthleteName = athleteName;
        Nationality = nationality;
        FinishTime = finishTime;
        IsPlayer = isPlayer;
        AthleteReference = athleteRef;
    }

    public string GetFormattedTime()
    {
        int minutes = (int)(FinishTime / 60f);
        float seconds = FinishTime % 60f;

        if (minutes > 0)
        {
            return string.Format("{0:D2}:{1:00.00}", minutes, seconds);
        }
        else
        {
            return string.Format("{0:00.00}", seconds);
        }
    }

    public string GetOrdinalPlacement()
    {
        if (Placement % 100 >= 11 && Placement % 100 <= 13)
            return Placement + "th";

        switch (Placement % 10)
        {
            case 1: return Placement + "st";
            case 2: return Placement + "nd";
            case 3: return Placement + "rd";
            default: return Placement + "th";
        }
    }
}
