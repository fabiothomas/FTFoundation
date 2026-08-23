using System;
using System.Collections.Generic;

namespace FTFoundation.BuildInReferences
{
    public abstract class Saveable<T> : ISaveable
    {
        protected T Value { get; set; }
        public string Id { get; set; }
        public bool IsDirty { get; protected set; }
        private readonly List<Action<T>> bindings = new();

        public Saveable(string id, T defaultValue)
        {
            Id = id;
            Value = defaultValue;
        }

        public void Set(T newValue)
        {
            if (EqualityComparer<T>.Default.Equals(Value, newValue)) return;
            Value = newValue;
            IsDirty = true;
            InvokeBindings();
        }

        public T Get()
        {
            return Value;
        }

        public IDisposable Bind(Action<T> setter)
        {
            bindings.Add(setter);
            return new DelegateDisposable(() => bindings.Remove(setter));
        }

        protected void InvokeBindings() => bindings.ForEach(binding => binding(Value));

        public abstract void Save();
        public abstract void Restore();
    }
}