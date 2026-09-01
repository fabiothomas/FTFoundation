namespace FTFoundation.BuildInReferences
{
    /// <summary>
    /// Marker interface for messages published through <see cref="IEventService"/>.
    /// Implement this on a struct or class to define a new event; the event's own type is its identity,
    /// so no central registry (e.g. an enum) needs to be extended to add one.
    /// </summary>
    public interface IEvent
    {
    }
}
