using System;
using UnityEngine;

namespace FTFoundation.BuildInReferences
{
    /// <summary>
    /// <para>A global registry holding at most one active <see cref="MonoBehaviour"/> instance per concrete type,
    /// reachable from anywhere in the application, e.g. looking up the Player object from any other script.</para>
    /// <para>This registry is app-wide, not scene-scoped: an instance registered in one scene remains
    /// reachable from another additively-loaded scene until it is unregistered.</para>
    /// </summary>
    public interface IReferenceService
    {
        /// <summary>
        /// Registers a reference so it can be looked up via <see cref="GetReference{T}"/> or <see cref="UseReference{T}"/>.
        /// Call this when the reference is created, likely in its own <c>Awake()</c>, before it's requested by any other object.
        /// Only one instance of a given concrete type may be registered at a time; a second registration is rejected and logged.
        /// </summary>
        /// <param name="ref_">Reference to be registered.</param>
        /// <returns>
        /// An <see cref="IDisposable"/> that unregisters the reference when disposed, dispose it from the reference's
        /// own <c>OnDestroy()</c>. Disposing is always safe, including when registration was rejected.
        /// </returns>
        public IDisposable RegisterReference(MonoBehaviour ref_);

        /// <summary>
        /// Finds the reference defined by the given generic type and returns it, or <c>null</c> if it isn't registered. <br/>
        /// Always null-check the result, or use <c>UseReference()</c> instead if you'd rather branch on presence directly.
        /// </summary>
        /// <remarks>
        /// Never use this method in <c>Awake()</c> or in <c>OnDestroy()</c>, registration order between objects
        /// is not guaranteed. If you need the reference at that point, use <see cref="WaitForReference{T}"/> instead.
        /// </remarks>
        public T? GetReference<T>() where T : MonoBehaviour;

        /// <summary>
        /// Finds the reference defined by the given generic type and executes the given action with it.
        /// </summary>
        /// <param name="action"> the action to perform with the found reference. </param>
        /// <param name="fallback"> the fallback action in case the reference is not found. </param>
        /// <remarks>
        /// Never use this method in <c>Awake()</c> or in <c>OnDestroy()</c>, registration order between objects
        /// is not guaranteed. If you need the reference at that point, use <see cref="WaitForReference{T}"/> instead.
        /// </remarks>
        public void UseReference<T>(Action<T> action, Action fallback) where T : MonoBehaviour;

        /// <summary>
        /// Invokes <paramref name="callback"/> with the reference of type <typeparamref name="T"/> as soon as it
        /// becomes available, immediately if it is already registered, otherwise the moment it is registered.
        /// Safe to call from <c>Awake()</c>, unlike <see cref="GetReference{T}"/> and <see cref="UseReference{T}"/>,
        /// since it does not depend on the other object's <c>Awake()</c> having already run.
        /// </summary>
        /// <remarks>
        /// Fires at most once. If the reference is unregistered and re-registered later, a callback that has
        /// already fired will not fire again, call <see cref="WaitForReference{T}"/> again if you need that.
        /// </remarks>
        /// <param name="callback">Invoked exactly once with the reference, as soon as it is available.</param>
        /// <returns>
        /// An <see cref="IDisposable"/> that cancels the pending wait if disposed before the reference becomes
        /// available. Disposing after the callback has already fired is a safe no-op.
        /// </returns>
        public IDisposable WaitForReference<T>(Action<T> callback) where T : MonoBehaviour;
    }
}
