using System;

namespace FTFoundation.BuildInReferences
{
    public delegate void NotifyEventHandler();

    public class Value<T>
    {

        public T Current { get; private set; }
        private event Func<T>? Setters;

        public Value(T initialValue)
        {
            Current = initialValue;
        }

        public void Set(T newValue)
        {
            Current = newValue;
            Setters?.Invoke();
        }

        public IDisposable Bind(Func<T> setValueFunction)
        {
            Setters += setValueFunction;
            return new Unsubscriber(() => Setters -= setValueFunction);
        }

        private class Unsubscriber : IDisposable
        {
            private readonly Action _unsubscribe;

            public Unsubscriber(Action unsubscribe)
            {
                _unsubscribe = unsubscribe;
            }

            public void Dispose()
            {
                _unsubscribe();
            }
        }
    }
}