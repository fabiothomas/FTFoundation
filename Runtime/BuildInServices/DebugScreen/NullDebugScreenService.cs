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

        public IDisposable AddValueWatcher(string label, Saveable<string> saveable, Color? color = null)
        {
            return new DelegateDisposable(() => { /* No value watcher to remove */ });
        }

        public IDisposable AddValueWatcher(string label, Saveable<int> saveable, Color? color = null)
        {
            return new DelegateDisposable(() => { /* No value watcher to remove */ });
        }

        public IDisposable AddValueWatcher(string label, Saveable<float> saveable, Color? color = null)
        {
            return new DelegateDisposable(() => { /* No value watcher to remove */ });
        }

        public IDisposable AddValueWatcher(string label, Saveable<bool> saveable, Color? color = null)
        {
            return new DelegateDisposable(() => { /* No value watcher to remove */ });
        }

        public void Clear() { }

        public void Print(string message) { }

        public void Toggle(bool? active = null) { }
    }
}