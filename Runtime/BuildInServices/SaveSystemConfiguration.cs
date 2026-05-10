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
            new PrefsSaveable<int>("testInt", 42),
            new PrefsSaveable<float>("testFloat", 3.14f),
            new PrefsSaveable<bool>("testBool", true)
        };
    }
}