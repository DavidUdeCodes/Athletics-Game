using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

public class SplineMovement : MonoBehaviour
{
    [Header("Spline")]
    private SplineContainer _splineContainer;
    private float _splineLength = 0f;
    private bool _shouldLoop = false;

    private float _distanceTravelled = 0f;
    private float _raceStartOffset = 0f;
    private bool _isMoving = false;

    public float DistanceTravelled => _distanceTravelled - _raceStartOffset;
    public float NormalizedProgress => _splineLength > 0f ? (_distanceTravelled / _splineLength) % 1f : 0f;
    public bool ShouldLoop => _shouldLoop;

    public void SetSpline(SplineContainer spline)
    {
        _splineContainer = spline;
        _splineLength = spline != null ? spline.CalculateLength() : 0f;
    }

    public void SetShouldLoop(bool shouldLoop)
    {
        _shouldLoop = shouldLoop;
    }

    public void PositionAtDistance(float distance)
    {
        _raceStartOffset = distance;
        _distanceTravelled = distance;
        UpdatePosition();
    }

    public void PositionAtStart()
    {
        _raceStartOffset = 0f;
        _distanceTravelled = 0f;
        UpdatePosition();
    }

    public void SetDistanceTravelled(float distance)
    {
        if (_shouldLoop)
            _distanceTravelled = distance;
        else
            _distanceTravelled = Mathf.Clamp(distance, _raceStartOffset, _splineLength);
    }

    public void ResetState()
    {
        _distanceTravelled = 0f;
        _raceStartOffset = 0f;
        _splineLength = _splineContainer != null ? _splineContainer.CalculateLength() : 0f;
        _isMoving = false;
        UpdatePosition();
    }

    public void StartMovement()
    {
        _isMoving = true;
        UpdatePosition();
    }

    public void StopMovement()
    {
        _isMoving = false;
    }

    public void MoveAlongSpline(float speedMetersPerSecond)
    {
        if (!_isMoving || _splineContainer == null || _splineLength <= 0f)
            return;

        _distanceTravelled += speedMetersPerSecond * Time.deltaTime;

        if (!_shouldLoop)
        {
            _distanceTravelled = Mathf.Clamp(_distanceTravelled, _raceStartOffset, _splineLength);
        }

        UpdatePosition();
    }

    public bool HasFinished(float finishDistance)
    {
        return DistanceTravelled >= finishDistance;
    }

    private void UpdatePosition()
    {
        if (_splineContainer == null || _splineLength <= 0f)
            return;

        float t = _distanceTravelled / _splineLength;
        
        if (_shouldLoop)
        {
            t = t % 1f;
        }
        else
        {
            t = Mathf.Clamp01(t);
        }

        _splineContainer.Spline.Evaluate(
            t,
            out float3 position,
            out float3 tangent,
            out float3 up
        );

        Vector3 worldPosition = _splineContainer.transform.TransformPoint((Vector3)position);
        transform.position = worldPosition;

        if (math.lengthsq(tangent) > 0.001f)
        {
            Vector3 worldForward = _splineContainer.transform.TransformDirection((Vector3)tangent);
            Vector3 worldUp = _splineContainer.transform.TransformDirection((Vector3)up);

            if (worldForward.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(worldForward, worldUp);
            }
        }
    }
}