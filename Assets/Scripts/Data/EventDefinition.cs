using UnityEngine;

[CreateAssetMenu(fileName = "Event_", menuName = "Athletics/Event Definition")]
public class EventDefinition : ScriptableObject
{
    [SerializeField] private string eventName;
    [SerializeField] private RaceDistance distance;
    [SerializeField] private string eventCategory;
    [SerializeField] private string description;

    public string EventName => eventName;
    public RaceDistance Distance => distance;
    public string EventCategory => eventCategory;
    public string Description => description;

    public override string ToString() => eventName;
}
