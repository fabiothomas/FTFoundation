using System;
using FTFoundation.BuildInReferences;
using FTFoundation.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FTFoundation.BuildInServices
{
    [ServiceFallback]
    [Service(typeof(IDebugScreenService), ServiceType.SINGLETON)]
    public class NullDebugScreenService : IDebugScreenService
    {
        public IDisposable AddButton(string label, Action onClick, Color? color = null, Key? hotkey = null)
        {
            return new DelegateDisposable(() => { /* No button to remove */ });
        }

        public IDisposable AddValueWatcher<T>(string label, Func<T> valueProvider, Color? color = null)
        {
            return new DelegateDisposable(() => { /* No value watcher to remove */ });
        }

        public void Clear() { }

        public void Print(string message) { }

        public void Toggle(bool? active = null) { }
    }
}