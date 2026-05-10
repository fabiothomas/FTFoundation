using System;
using UnityEngine.InputSystem;
using UnityEngine;

namespace FTFoundation.BuildInReferences
{
    /// <summary>
    /// <para>A service used for displaying a debug screen.</para>
    /// <para>The debug screen can be used to display log messages, add buttons for debugging purposes, and watch values in real-time.</para>
    /// <para>The debug screen can be toggled on and off, and it will only be included in Editor and Development builds.</para>
    /// </summary>
    public interface IDebugScreenService
    {
        /// <summary>
        /// Toggles the visibility of the debug screen or sets it to a specific state if the 'active' parameter is provided.
        /// </summary>
        /// <param name="active">If provided, sets the debug screen to the specified state. If null, toggles the current state.</param>
        public void Toggle(bool? active = null);

        /// <summary>
        /// Prints a message to the debug screen terminal.
        /// </summary>
        /// <param name="message">The message to print.</param>
        public void Print(string message);

        /// <summary>
        /// Clears all messages from the debug screen terminal.
        /// </summary>
        public void Clear();

        /// <summary>
        /// Adds a button to the debug screen with the specified label, click action, color, and optional hotkey. The button will be removed when the returned IDisposable is disposed.
        /// </summary>
        /// <param name="label">The text to display on the button.</param>
        /// <param name="onClick">The action to perform when the button is clicked.</param>
        /// <param name="color">The color of the button. If null, a default color will be used.</param>
        /// <param name="hotkey">An optional hotkey that can be used to trigger the button's click action.</param>
        /// <returns>An IDisposable that can be disposed to remove the button from the debug screen.</returns>
        public IDisposable AddButton(string label, Action onClick, Color? color = null, Key? hotkey = null);

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="label"></param>
        /// <param name="saveable"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        public IDisposable AddValueWatcher(string label, Saveable<string> saveable, Color? color = null);

        public IDisposable AddValueWatcher(string label, Saveable<int> saveable, Color? color = null);

        public IDisposable AddValueWatcher(string label, Saveable<float> saveable, Color? color = null);

        public IDisposable AddValueWatcher(string label, Saveable<bool> saveable, Color? color = null);
    }
}