using System;

namespace FTFoundation.BuildInReferences
{
    public interface ILifetimeService
    {
        public IDisposable OnUpdate(Action action);
        public IDisposable OnFixedUpdate(Action action);
        public IDisposable OnLateUpdate(Action action);
    }
}