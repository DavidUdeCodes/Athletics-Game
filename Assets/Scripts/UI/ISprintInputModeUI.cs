using UnityEngine;

public interface ISprintInputModeUI
{
    void Show();
    void Hide();
    void ShowQualityFeedback(TapQuality quality);
    void UpdateMomentum(float momentum);
    void UpdateSpeed(float currentSpeed, float maxSpeed);
}
