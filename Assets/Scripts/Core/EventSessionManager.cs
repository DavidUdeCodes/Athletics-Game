using UnityEngine;

public class EventSessionManager : MonoBehaviour
{
    private static EventSessionManager _instance;

    private EventSessionConfig _currentConfig;
    private bool _hasConfig = false;

    public EventSessionConfig CurrentConfig => _currentConfig;
    public bool HasConfig => _hasConfig;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        _currentConfig = new EventSessionConfig();
    }

    public static EventSessionManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject managerGO = new GameObject("EventSessionManager");
                _instance = managerGO.AddComponent<EventSessionManager>();
                DontDestroyOnLoad(managerGO);
            }
            return _instance;
        }
    }

    public void SetEventConfig(RaceDistance distance, LaneSelectionMode laneMode, int fixedLane, SprintInputMode inputMode)
    {
        _currentConfig = new EventSessionConfig(distance, laneMode, fixedLane, inputMode);
        _hasConfig = true;
        Debug.Log($"[EventSessionManager] Config set: {_currentConfig}");
    }

    public void SetEventConfig(EventSessionConfig config)
    {
        if (config != null)
        {
            _currentConfig = config;
            _hasConfig = true;
            Debug.Log($"[EventSessionManager] Config set: {_currentConfig}");
        }
    }

    public void ClearConfig()
    {
        _currentConfig = new EventSessionConfig();
        _hasConfig = false;
        Debug.Log("[EventSessionManager] Config cleared");
    }

    public void Cleanup()
    {
        if (_instance == this)
        {
            _instance = null;
        }
        Destroy(gameObject);
    }
}
