using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawns and manages starting blocks for athletes ahead of a race start. This
/// is the only place responsible for that job - RaceManager just hands it the
/// athletes to spawn for once they exist and are positioned in their lane.
/// Works with any number of supplied athlete references, so it naturally
/// extends to AI competitors or runtime-instantiated athletes without change.
/// </summary>
public class StartingBlocksController : MonoBehaviour
{
    [Header("Starting Blocks")]
    [SerializeField] [Tooltip("Prefab instantiated behind each athlete before the race starts")]
    private GameObject startingBlocksPrefab;
    [SerializeField] [Tooltip("Spawn offset in the athlete's local space (should place the blocks behind the athlete)")]
    private Vector3 localSpawnOffset = new(0f, 0f, -0.6f);
    [SerializeField] [Tooltip("Distance the athlete must move from the blocks, after the race has officially started, before the blocks are removed")]
    private float removalDistance = 3f;

    [Space]
    [Header("Dependencies")]
    [SerializeField] [Tooltip("Race manager used to detect when the race has officially started")]
    private RaceManager raceManager;
    [SerializeField] [Tooltip("Optional parent for spawned race objects, keeps the scene hierarchy organised")]
    private Transform raceObjectsParent;

    private readonly List<StartingBlockInstance> _pendingBlocks = new();

    private void OnEnable()
    {
        if (raceManager != null)
        {
            raceManager.OnRaceStartStateChanged += HandleRaceStartStateChanged;
        }
    }

    private void OnDisable()
    {
        if (raceManager != null)
        {
            raceManager.OnRaceStartStateChanged -= HandleRaceStartStateChanged;
        }
    }

    /// <summary>
    /// Spawns a set of starting blocks for each supplied athlete. Athletes are
    /// expected to already be initialized and positioned in their lane.
    /// </summary>
    public void SpawnBlocksForAthletes(IEnumerable<Athlete> athletes)
    {
        if (athletes == null) return;

        foreach (Athlete athlete in athletes)
        {
            SpawnBlocksForAthlete(athlete);
        }
    }

    /// <summary>
    /// Spawns a single set of starting blocks behind the given athlete, in world
    /// space, oriented to match the athlete's current lane direction. The blocks
    /// are intentionally not parented to the athlete - the athlete leaves them
    /// immediately at the start of the race, so they must stay fixed in place.
    /// </summary>
    public void SpawnBlocksForAthlete(Athlete athlete)
    {
        if (startingBlocksPrefab == null)
        {
            Debug.LogError($"{gameObject.name}: Starting blocks prefab not assigned to StartingBlocksController");
            return;
        }

        if (athlete == null) return;

        Transform athleteTransform = athlete.transform;
        Vector3 spawnPosition = athleteTransform.TransformPoint(localSpawnOffset);
        Quaternion spawnRotation = athleteTransform.rotation;

        GameObject blockInstance = Instantiate(startingBlocksPrefab, spawnPosition, spawnRotation, raceObjectsParent);

        StartingBlockInstance block = blockInstance.GetComponent<StartingBlockInstance>();
        if (block == null)
        {
            block = blockInstance.AddComponent<StartingBlockInstance>();
        }

        block.Initialize(athlete, removalDistance);
        _pendingBlocks.Add(block);
    }

    private void HandleRaceStartStateChanged(RaceStartState newState)
    {
        if (newState != RaceStartState.Running) return;

        foreach (StartingBlockInstance block in _pendingBlocks)
        {
            if (block != null)
            {
                block.BeginMonitoringForRemoval();
            }
        }

        // Each block now owns its own removal decision - the controller has no
        // further per-frame responsibility for them.
        _pendingBlocks.Clear();
    }
}
