using UnityEngine.Events;
using Scripts.References.Events;

namespace Scripts.References.Interfaces
{
    /// <summary>
    /// <para>A service used for event management.</para>
    /// <para>This service provides access to Unity Events on a global scale troughout the entire application.</para>
    /// </summary>
    public interface IEventService
    {
        /// <summary>
        /// If true the event service will not send out any logs regarding event subscribing, unsubscribing and invoking.
        /// </summary>
        /// <remarks><c>true</c> by default, can be set to false for debugging purposes.</remarks>
        public bool LogsDisabled { get; set; }

        /// <summary>
        /// Subscribes to an event with no parameters.
        /// </summary>
        /// <param name="eventName">The event to be subscribed to (ensure the event is registered in <c>Scripts.References.Events</c>).</param>
        /// <param name="action">Action to be called when the event is invoked.</param>
        public void SubscribeTo(EventName eventName, UnityAction action);
        /// <summary>
        /// Subscribes to an event with one parameter.
        /// </summary>
        /// <typeparam name="T">The type of the event's parameter.</typeparam>
        /// <param name="eventName">The event to be subscribed to (ensure the event is registered in <c>Scripts.References.Events</c>).</param>
        /// <param name="action">Action to be called when the event is invoked.</param>
        public void SubscribeTo<T>(EventName eventName, UnityAction<T> action);
        /// <summary>
        /// Subscribes to an event with two parameters.
        /// </summary>
        /// <typeparam name="T0">The type of the event's first parameter.</typeparam>
        /// <typeparam name="T1">The type of the event's second parameter.</typeparam>
        /// <param name="eventName">The event to be subscribed to (ensure the event is registered in <c>Scripts.References.Events</c>).</param>
        /// <param name="action">Action to be called when the event is invoked.</param>
        public void SubscribeTo<T0, T1>(EventName eventName, UnityAction<T0, T1> action);
        /// <summary>
        /// Subscribes to an event with three parameters.
        /// </summary>
        /// <typeparam name="T0">The type of the event's first parameter.</typeparam>
        /// <typeparam name="T1">The type of the event's second parameter.</typeparam>
        /// <typeparam name="T2">The type of the event's third parameter.</typeparam>
        /// <param name="eventName">The event to be subscribed to (ensure the event is registered in <c>Scripts.References.Events</c>).</param>
        /// <param name="action">Action to be called when the event is invoked.</param>
        public void SubscribeTo<T0, T1, T2>(EventName eventName, UnityAction<T0, T1, T2> action);
        /// <summary>
        /// Subscribes to an event with four parameters.
        /// </summary>
        /// <typeparam name="T0">The type of the event's first parameter.</typeparam>
        /// <typeparam name="T1">The type of the event's second parameter.</typeparam>
        /// <typeparam name="T2">The type of the event's third parameter.</typeparam>
        /// <typeparam name="T3">The type of the event's fourth parameter.</typeparam>
        /// <param name="eventName">The event to be subscribed to (ensure the event is registered in <c>Scripts.References.Events</c>).</param>
        /// <param name="action">Action to be called when the event is invoked.</param>
        public void SubscribeTo<T0, T1, T2, T3>(EventName eventName, UnityAction<T0, T1, T2, T3> action);

        /// <summary>
        /// Unsubscribes from an event with no parameters.
        /// </summary>
        /// <param name="eventName">The event to be unsubscribed from (ensure the event is registered in <c>Scripts.References.Events</c>).</param>
        /// <param name="action">Action to be called when the event is invoked.</param>
        public void UnsubscribeTo(EventName eventName, UnityAction action);
        /// <summary>
        /// Unsubscribes from an event with one parameter.
        /// </summary>
        /// <typeparam name="T">The type of the event's parameter.</typeparam>
        /// <param name="eventName">The event to be unsubscribed from (ensure the event is registered in <c>Scripts.References.Events</c>).</param>
        /// <param name="action">Action to be called when the event is invoked.</param>
        public void UnsubscribeTo<T>(EventName eventName, UnityAction<T> action);
        /// <summary>
        /// Unsubscribes from an event with two parameters.
        /// </summary>
        /// <typeparam name="T0">The type of the event's first parameter.</typeparam>
        /// <typeparam name="T1">The type of the event's second parameter.</typeparam>
        /// <param name="eventName">The event to be unsubscribed from (ensure the event is registered in <c>Scripts.References.Events</c>).</param>
        /// <param name="action">Action to be called when the event is invoked.</param>
        public void UnsubscribeTo<T0, T1>(EventName eventName, UnityAction<T0, T1> action);
        /// <summary>
        /// Unsubscribes from an event with three parameters.
        /// </summary>
        /// <typeparam name="T0">The type of the event's first parameter.</typeparam>
        /// <typeparam name="T1">The type of the event's second parameter.</typeparam>
        /// <typeparam name="T2">The type of the event's third parameter.</typeparam>
        /// <param name="eventName">The event to be unsubscribed from (ensure the event is registered in <c>Scripts.References.Events</c>).</param>
        /// <param name="action">Action to be called when the event is invoked.</param>
        public void UnsubscribeTo<T0, T1, T2>(EventName eventName, UnityAction<T0, T1, T2> action);
        /// <summary>
        /// Unsubscribes from an event with four parameters.
        /// </summary>
        /// <typeparam name="T0">The type of the event's first parameter.</typeparam>
        /// <typeparam name="T1">The type of the event's second parameter.</typeparam>
        /// <typeparam name="T2">The type of the event's third parameter.</typeparam>
        /// <typeparam name="T3">The type of the event's fourth parameter.</typeparam>
        /// <param name="eventName">The event to be unsubscribed from (ensure the event is registered in <c>Scripts.References.Events</c>).</param>
        /// <param name="action">Action to be called when the event is invoked.</param>
        public void UnsubscribeTo<T0, T1, T2, T3>(EventName eventName, UnityAction<T0, T1, T2, T3> action);

        /// <summary>
        /// Attempts to invoke an event with no parameters. Fails if the event is first subscribed to or invoked with parameters.
        /// </summary>
        /// <param name="eventName">The event to be invoked (ensure the event is registered in <c>Scripts.References.Events</c>).</param>
        public void AttemptInvoke(EventName eventName);
        /// <summary>
        /// Attempts to invoke an event with one parameter. Fails if the event is first subscribed to or invoked with a different set of parameters.
        /// </summary>
        /// <typeparam name="T">The type of the event's parameter.</typeparam>
        /// <param name="eventName">The event to be invoked (ensure the event is registered in <c>Scripts.References.Events</c>).</param>
        /// <param name="param1">The event's parameter.</param>
        public void AttemptInvoke<T>(EventName eventName, T param1);
        /// <summary>
        /// Attempts to invoke an event with two parameters. Fails if the event is first subscribed to or invoked with a different set of parameters.
        /// </summary>
        /// <typeparam name="T0">The type of the event's first parameter.</typeparam>
        /// <typeparam name="T1">The type of the event's second parameter.</typeparam>
        /// <param name="eventName">The event to be invoked (ensure the event is registered in <c>Scripts.References.Events</c>).</param>
        /// <param name="param1">The event's first parameter.</param>
        /// <param name="param2">The event's second parameter.</param>
        public void AttemptInvoke<T0, T1>(EventName eventName, T0 param1, T1 param2);
        /// <summary>
        /// Attempts to invoke an event with three parameters. Fails if the event is first subscribed to or invoked with a different set of parameters.
        /// </summary>
        /// <typeparam name="T0">The type of the event's first parameter.</typeparam>
        /// <typeparam name="T1">The type of the event's second parameter.</typeparam>
        /// <typeparam name="T2">The type of the event's third parameter.</typeparam>
        /// <param name="eventName">The event to be invoked (ensure the event is registered in <c>Scripts.References.Events</c>).</param>
        /// <param name="param1">The event's first parameter.</param>
        /// <param name="param2">The event's second parameter.</param>
        /// <param name="param3">The event's third parameter.</param>
        public void AttemptInvoke<T0, T1, T2>(EventName eventName, T0 param1, T1 param2, T2 param3);
        /// <summary>
        /// Attempts to invoke an event with four parameters. Fails if the event is first subscribed to or invoked with a different set of parameters.
        /// </summary>
        /// <typeparam name="T0">The type of the event's first parameter.</typeparam>
        /// <typeparam name="T1">The type of the event's second parameter.</typeparam>
        /// <typeparam name="T2">The type of the event's third parameter.</typeparam>
        /// <typeparam name="T3">The type of the event's fourth parameter.</typeparam>
        /// <param name="eventName">The event to be invoked (ensure the event is registered in <c>Scripts.References.Events</c>).</param>
        /// <param name="param1">The event's first parameter.</param>
        /// <param name="param2">The event's second parameter.</param>
        /// <param name="param3">The event's third parameter.</param>
        /// <param name="param4">The event's fourth parameter.</param>
        public void AttemptInvoke<T0, T1, T2, T3>(EventName eventName, T0 param1, T1 param2, T2 param3, T3 param4);
    }
}