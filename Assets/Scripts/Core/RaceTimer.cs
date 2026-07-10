using UnityEngine;
using System;

public class RaceTimer : MonoBehaviour
{
    private float _elapsedTime = 0f;
    private bool _isRunning = false;
    private bool _hasStarted = false;
    private float _officialFinishTime = -1f;

    public event Action<string> OnTimerUpdated;
    public event Action<float> OnTimerFinished;

    public float ElapsedTime => _elapsedTime;
    public bool IsRunning => _isRunning;
    public bool HasStarted => _hasStarted;
    public float OfficialFinishTime => _officialFinishTime;

    private void Start()
    {
        _elapsedTime = 0f;
        _isRunning = false;
        _hasStarted = false;
        _officialFinishTime = -1f;
    }

    private void Update()
    {
        if (!_isRunning) return;

        _elapsedTime += Time.deltaTime;
        OnTimerUpdated?.Invoke(FormatTime(_elapsedTime));
    }

    public void StartTimer()
    {
        if (_hasStarted) return;

        _hasStarted = true;
        _isRunning = true;
        _elapsedTime = 0f;
        _officialFinishTime = -1f;
    }

    public void StopTimer()
    {
        if (!_isRunning) return;

        _isRunning = false;
        _officialFinishTime = _elapsedTime;
        OnTimerFinished?.Invoke(_officialFinishTime);
    }

    public void PauseTimer()
    {
        _isRunning = false;
    }

    public void ResumeTimer()
    {
        if (!_hasStarted) return;

        _isRunning = true;
    }

    public void ResetTimer()
    {
        _elapsedTime = 0f;
        _isRunning = false;
        _hasStarted = false;
        _officialFinishTime = -1f;
    }

    public string GetFormattedTime()
    {
        return FormatTime(_elapsedTime);
    }

    public string GetFormattedOfficialTime()
    {
        if (_officialFinishTime < 0f)
            return FormatTime(_elapsedTime);

        return FormatTime(_officialFinishTime);
    }

    private string FormatTime(float timeInSeconds)
    {
        if (timeInSeconds < 0f)
            timeInSeconds = 0f;

        int minutes = (int)(timeInSeconds / 60f);
        float seconds = timeInSeconds % 60f;

        if (minutes > 0)
        {
            return string.Format("{0:D2}:{1:00.00}", minutes, seconds);
        }
        else
        {
            return string.Format("{0:00.00}", seconds);
        }
    }
}
