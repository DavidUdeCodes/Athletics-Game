using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResultRow : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI placementText;
    [SerializeField] private Image nationalityImage;
    [SerializeField] private TextMeshProUGUI athleteNameText;
    [SerializeField] private TextMeshProUGUI finishTimeText;

    public void SetResultData(RaceResult result)
    {
        if (result == null) return;

        if (placementText != null)
        {
            placementText.text = result.GetOrdinalPlacement();
        }

        if (athleteNameText != null)
        {
            athleteNameText.text = result.AthleteName;
        }

        if (finishTimeText != null)
        {
            finishTimeText.text = result.GetFormattedTime();
        }

        if (nationalityImage != null)
        {
            nationalityImage.color = GetNationalityColor(result.Nationality);
        }
    }

    private Color GetNationalityColor(string nationality)
    {
        return Color.gray;
    }
}
