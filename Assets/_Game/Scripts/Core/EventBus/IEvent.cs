namespace DreamCafe.Core.EventBus
{
    /// <summary>
    /// Marker interface for all event payloads. Implementations should be readonly structs.
    /// TODO: Add metadata (timestamp, source ID) for enriched analytics replay.
    /// </summary>
    public interface IEvent { }
}
