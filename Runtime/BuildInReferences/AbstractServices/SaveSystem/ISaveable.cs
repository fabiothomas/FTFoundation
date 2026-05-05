using System;

namespace FTFoundation.BuildInReferences
{
    public interface ISaveable
    {
        string Id { get; set; }
        bool IsDirty { get; }
        // public void Set<S>(S newValue);
        // public S Get<S>();
        // public IDisposable Bind<S>(Action<S> setter);
        void Save();
        void Restore();
    }
}