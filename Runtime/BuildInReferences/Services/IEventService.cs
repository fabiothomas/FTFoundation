using System;

namespace FTFoundation.BuildInReferences
{
    /// <summary>
    /// <para>A service used for event management.</para>
    /// <para>This service provides a typed, application-wide pub/sub bus. Each event is its own type
    /// implementing <see cref="IEvent"/> — subscribers and publishers are matched by that type, so
    /// adding a new event means declaring a new struct or class rather than editing a shared registry.</para>
    /// </summary>
    /// <example>
    /// <code>
    /// public readonly struct PlayerDied : IEvent
    /// {
    ///     public readonly int KillerId;
    ///     public PlayerDied(int killerId) => KillerId = killerId;
    /// }
    ///
    /// IDisposable subscription = events.Subscribe&lt;PlayerDied&gt;(e => Respawn(e.KillerId));
    /// events.Publish(new PlayerDied(killerId: 3));
    /// subscription.Dispose(); // unsubscribes
    /// </code>
    /// </example>
    public interface IEventService
    {
        /// <summary>
        /// If true the event service will not send out any logs regarding subscribing, unsubscribing and publishing.
        /// </summary>
        /// <remarks><c>true</c> by default, can be set to false for debugging purposes.</remarks>
        public bool LogsDisabled { get; set; }

        /// <summary>
        /// Subscribes to events of type <typeparamref name="TEvent"/>.
        /// </summary>
        /// <typeparam name="TEvent">The event type to subscribe to.</typeparam>
        /// <param name="handler">Called with the published event instance whenever <typeparamref name="TEvent"/> is published.</param>
        /// <returns>An <see cref="IDisposable"/> that unsubscribes <paramref name="handler"/> when disposed.</returns>
        public IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IEvent;

        /// <summary>
        /// Publishes an event to every current subscriber of its type. A no-op if there are no subscribers.
        /// </summary>
        /// <typeparam name="TEvent">The event type being published.</typeparam>
        /// <param name="eventInstance">The event instance to deliver to subscribers.</param>
        public void Publish<TEvent>(TEvent eventInstance) where TEvent : IEvent;
    }
}
