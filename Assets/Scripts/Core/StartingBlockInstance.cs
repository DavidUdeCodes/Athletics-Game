using UnityEngine;

/// <summary>
/// Attached to a single spawned starting-blocks instance. Knows which athlete it
/// belongs to and is solely responsible for deciding when it should remove
/// itself - once the race has officially started AND its athlete has moved
/// beyond the configured removal distance. Never removed on a timer, so a slow
/// reaction or a delayed start never causes the blocks to disappear while the
/// athlete is still using them.
/// </summary>
public class StartingBlockInstance : MonoBehaviour
{
    private Athlete _athlete;
    private float _removalDistance;
    private Vector3 _spawnPosition;
    private bool _isMonitoring;

    /// <summary>
    /// Sets the athlete these blocks belong to and how far they must travel
    /// before the blocks are removed. Called once, immediately after spawning.
    /// </summary>
    public void Initialize(Athlete athlete, float removalDistance)
    {
        _athlete = athlete;
        _removalDistance = removalDistance;
        _spawnPosition = transform.position;
    }

    /// <summary>
    /// Starts distance monitoring. Called by the spawning controller only after
    /// the race has officially started - before that, this instance stays idle
    /// regardless of how long the athlete lingers at the line.
    /// </summary>
    public void BeginMonitoringForRemoval()
    {
        _isMonitoring = true;
    }

    private void Update()
    {
        if (!_isMonitoring) return;

        if (_athlete == null)
        {
            Destroy(gameObject);
            return;
        }

        float distanceFromBlocks = Vector3.Distance(_spawnPosition, _athlete.transform.position);
        if (distanceFromBlocks >= _removalDistance)
        {
            Destroy(gameObject);
        }
    }
}
