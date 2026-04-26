using System;
using UnityEngine.InputSystem;
using UnityEngine;

namespace FTFoundation.BuildInReferences
{
    public interface IDebugScreenService
    {
        public void Toggle(bool? active = null);
        public void Print(string message);
        public void Clear();
        public IDisposable AddButton(string label, Action onClick, Color? color = null, Key? hotkey = null);
        public void AddValueWatcher<T>(string label, Func<T> valueProvider, Color? color = null);
        public void RemoveValueWatcher(string label);
    }
}