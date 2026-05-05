using System.Collections.Generic;

namespace FTFoundation.BuildInReferences
{
    public interface ISaveSystemConfiguration
    {
        IReadOnlyList<ISaveable> Saveables { get; }
    }
}