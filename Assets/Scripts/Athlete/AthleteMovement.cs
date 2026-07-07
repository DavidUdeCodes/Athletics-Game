using UnityEngine;
using UnityEngine.Splines;
using System;

[RequireComponent(typeof(SplineMovement))]
public class AthleteMovement : MonoBehaviour
{
    [Header("Sprint Controller")]
    [SerializeField] [Tooltip("Controls speed based on momentum and tap quality")]
    private SprintController sprintController;

    [Header("Post-Finish Deceleration")]
    [SerializeField] [Tooltip("Rate at which athlete decelerates after finishing")]
    private float postFinishDecelerationRate = 2f;

    private SplineMovement _splineMovement;
    private AthleteInput _athleteInput;
    private RhythmInputMode _rhythmController;
    private float _currentSpeed = 0f;
    private bool _active = false;
    private bool _isDecelerating = false;

    public float CurrentSpeed => _currentSpeed;
    public float DistanceTravelled => _splineMovement != null ? _splineMovement.DistanceTravelled : 0f;
    public bool IsMoving => _currentSpeed > 0.01f;
    public bool IsDecelerating => _isDecelerating;

    public event Action OnAthleteAtRest;

    private void Awake()
    {
        _splineMovement = GetComponent<SplineMovement>();
        _athleteInput = GetComponent<AthleteInput>();
        _rhythmController = GetComponent<RhythmInputMode>();
    }

    public void SetStatMultipliers(float topSpeedMult, float accelerationMult)
    {
        if (sprintController != null)
            sprintController.SetStatMultipliers(topSpeedMult, accelerationMult);
    }

    public void SetSpline(SplineContainer spline)
    {
        if (_splineMovement != null)
            _splineMovement.SetSpline(spline);
    }

    public void SetShouldLoop(bool shouldLoop)
    {
        if (_splineMovement != null)
            _splineMovement.SetShouldLoop(shouldLoop);
    }

    public void PositionAtStart()
    {
        if (_splineMovement != null)
            _splineMovement.PositionAtStart();
    }

    public void PositionAtDistance(float distance)
    {
        if (_splineMovement != null)
            _splineMovement.PositionAtDistance(distance);
    }

    public void StartMoving()
    {
        _active = true;
        _currentSpeed = 0f;
        _isDecelerating = false;

        if (_splineMovement != null)
            _splineMovement.StartMovement();

        if (sprintController != null)
            sprintController.StartSprinting();

        if (_rhythmController != null)
            _rhythmController.PrepareForFirstInput();
    }

    public void FinishRace()
    {
        if (!_active) return;

        _isDecelerating = true;

        if (_athleteInput != null)
            _athleteInput.SetEnabled(false);

        ISprintInputMode currentMode = _athleteInput?.GetCurrentMode();
        if (currentMode is RhythmInputMode rhythmMode)
        {
            rhythmMode.StopRhythm();
        }

        if (sprintController != null)
            sprintController.StopSprinting();
    }

    public void ResetMovementState()
    {
        if (_splineMovement != null)
            _splineMovement.ResetState();
        
        _currentSpeed = 0f;
        _active = false;
        _isDecelerating = false;
    }

    public void StopMoving()
    {
        _active = false;
        _isDecelerating = false;

        if (_splineMovement != null)
            _splineMovement.StopMovement();

        if (sprintController != null)
            sprintController.StopSprinting();

        ISprintInputMode currentMode = _athleteInput?.GetCurrentMode();
        if (currentMode is RhythmInputMode rhythmMode)
        {
            rhythmMode.StopRhythm();
        }
    }

    private void Update()
    {
        if (!_active) return;

        if (_isDecelerating)
        {
            HandlePostFinishDeceleration();
        }
        else
        {
            if (sprintController != null)
            {
                _currentSpeed = sprintController.CurrentSpeed;

                if (_rhythmController != null && _rhythmController.enabled)
                    _rhythmController.UpdateRhythmSpeed(_currentSpeed);
            }
        }

        if (_splineMovement != null)
            _splineMovement.MoveAlongSpline(_currentSpeed);
    }

    private void HandlePostFinishDeceleration()
    {
        _currentSpeed = Mathf.Max(0f, _currentSpeed - postFinishDecelerationRate * Time.deltaTime);

        if (_currentSpeed <= 0.01f)
        {
            _currentSpeed = 0f;
            _active = false;
            _isDecelerating = false;
            
            if (_splineMovement != null)
                _splineMovement.StopMovement();

            OnAthleteAtRest?.Invoke();
        }
    }
}

