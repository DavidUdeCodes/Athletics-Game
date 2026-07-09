using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class EventManager : MonoBehaviour
{
    [SerializeField] private EventDefinition[] availableEvents;

    private Dictionary<RaceDistance, EventDefinition> _eventsByDistance;

    private void Awake()
    {
        InitializeEventMapping();
    }

    private void InitializeEventMapping()
    {
        _eventsByDistance = new Dictionary<RaceDistance, EventDefinition>();

        if (availableEvents == null || availableEvents.Length == 0)
        {
            Debug.LogError("EventManager: No events configured in availableEvents array");
            return;
        }

        foreach (EventDefinition eventDef in availableEvents)
        {
            if (eventDef != null)
            {
                _eventsByDistance[eventDef.Distance] = eventDef;
            }
        }
    }

    public EventDefinition GetEvent(RaceDistance distance)
    {
        return _eventsByDistance.TryGetValue(distance, out var eventDef) ? eventDef : null;
    }

    public IReadOnlyList<EventDefinition> GetAllEvents()
    {
        return availableEvents ?? System.Array.Empty<EventDefinition>();
    }

    public EventDefinition GetEventByName(string name)
    {
        return availableEvents?.FirstOrDefault(e => e != null && e.EventName == name);
    }
}
