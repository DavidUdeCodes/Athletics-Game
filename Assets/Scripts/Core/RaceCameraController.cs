using UnityEngine;
using Unity.Cinemachine;

public class RaceCameraController : MonoBehaviour
{
    [SerializeField] private CinemachineCamera cinemachineCamera;
    public void FollowAthlete(Transform athleteTransform)
    {
        cinemachineCamera.Target.TrackingTarget = athleteTransform;
        cinemachineCamera.Target.LookAtTarget = athleteTransform;
    }
}
