using System;
using System.Collections.Generic;
using FTFoundation.BuildInReferences;
using FTFoundation.Core;
using UnityEngine;

namespace FTFoundation.BuildInServices
{
    [Service(typeof(IReferenceService), ServiceType.SINGLETON)]
    public class ReferenceService : IReferenceService
    {
        ILoggerService _ls = null!;
        void Inject(ILoggerService ls)
        {
            _ls = ls;
        }

        private readonly Dictionary<Type, MonoBehaviour> referenceList = new();
        private readonly Dictionary<Type, Delegate> waiters = new();

        public IDisposable RegisterReference(MonoBehaviour ref_)
        {
            if (!ref_)
            {
                _ls.LogWarning("Could not register object because no reference was given");
                return new DelegateDisposable(() => { });
            }

            Type type = ref_.GetType();
            if (referenceList.ContainsKey(type))
            {
                _ls.LogError($"{type.Name} is already assigned and can not be assigned multiple times. Please ensure there is only one {type.Name} active at a time");
                return new DelegateDisposable(() => { });
            }

            referenceList.Add(type, ref_);

            if (waiters.TryGetValue(type, out var pending))
            {
                waiters.Remove(type);
                ((Action<MonoBehaviour>)pending).Invoke(ref_);
            }

            return new DelegateDisposable(() => Unregister(type, ref_));
        }

        private void Unregister(Type type, MonoBehaviour ref_)
        {
            // Only remove if this disposable's instance is still the one on file — a later
            // registration for the same type may have already replaced it.
            if (referenceList.TryGetValue(type, out var current) && current == ref_)
                referenceList.Remove(type);
        }

        public T? GetReference<T>() where T : MonoBehaviour
        {
            Type type = typeof(T);
            if (referenceList.TryGetValue(type, out var ref_) && ref_ is T t) return t;
            _ls.LogWarning($"Object '{type.Name}' could not be found. Make sure to add this object to the scene exactly once");
            return default;
        }

        public void UseReference<T>(Action<T> action, Action fallback) where T : MonoBehaviour
        {
            Type type = typeof(T);
            if (referenceList.TryGetValue(type, out var ref_) && ref_ is T t) action(t);
            else fallback();
        }

        public IDisposable WaitForReference<T>(Action<T> callback) where T : MonoBehaviour
        {
            Type type = typeof(T);
            if (referenceList.TryGetValue(type, out var existing) && existing is T t)
            {
                callback(t);
                return new DelegateDisposable(() => { });
            }

            Action<MonoBehaviour> wrapper = mb => callback((T)mb);

            waiters[type] = waiters.TryGetValue(type, out var current)
                ? Delegate.Combine(current, wrapper)
                : wrapper;

            return new DelegateDisposable(() => RemoveWaiter(type, wrapper));
        }

        private void RemoveWaiter(Type type, Delegate wrapper)
        {
            if (!waiters.TryGetValue(type, out var existing)) return;

            Delegate combined = Delegate.Remove(existing, wrapper);
            if (combined == null) waiters.Remove(type);
            else waiters[type] = combined;
        }
    }
}
