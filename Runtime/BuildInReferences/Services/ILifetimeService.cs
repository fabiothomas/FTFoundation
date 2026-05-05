using System;

namespace FTFoundation.BuildInReferences
{
    /// <summary>
    /// <para>A service used for managing the lifetime of actions that need to be updated every frame.</para>
    /// <para>This service provides a way to register actions that will be called on Update, FixedUpdate, and LateUpdate.</para>
    /// <para>The registered actions will be automatically unregistered when the returned IDisposable is disposed, which is useful for cleaning up resources and preventing memory leaks.</para>
    /// </summary>
    public interface ILifetimeService
    {
        /// <summary>
        /// Registers an action to be called on Update, and returns an IDisposable that can be disposed to unregister the action.
        /// </summary>
        /// <param name="action">The action to be called on Update.</param>
        /// <returns>An IDisposable that can be disposed to unregister the action.</returns>
        public IDisposable OnUpdate(Action action);

        /// <summary>
        /// Registers an action to be called on FixedUpdate, and returns an IDisposable that can be disposed to unregister the action.
        /// </summary>
        /// <param name="action">The action to be called on FixedUpdate.</param>
        /// <returns>An IDisposable that can be disposed to unregister the action.</returns>
        public IDisposable OnFixedUpdate(Action action);

        /// <summary>
        /// Registers an action to be called on LateUpdate, and returns an IDisposable that can be disposed to unregister the action.
        /// </summary>
        /// <param name="action">The action to be called on LateUpdate.</param>
        /// <returns>An IDisposable that can be disposed to unregister the action.</returns>
        public IDisposable OnLateUpdate(Action action);
    }
}