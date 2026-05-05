using System.Collections.Generic;
using FTFoundation.BuildInReferences;
using FTFoundation.Core;

namespace FTFoundation.BuildInServices
{
    [InstantiateOnStartup]
    [Service(typeof(ISaveSystemConfiguration), ServiceType.SINGLETON)]
    public class DefaultSaveSystemConfiguration : ISaveSystemConfiguration
    {
        public IReadOnlyList<ISaveable> Saveables { get; } = new List<ISaveable>
        {
            new PrefsSaveable<string>("test", "hello world"),
        };
    }
}