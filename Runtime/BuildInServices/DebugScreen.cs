using FTFoundation.BuildInReferences;
using FTFoundation.Core;
using UnityEngine;

namespace BuildInServices
{
    [InstantiateOnStartup]
    [Service(typeof(IDebugScreen), ServiceType.SINGLETON)]
    public class DebugScreen : IDebugScreen
    {
        [Inject] private IDedicatedObjectService DedicatedObjectService { get; set; } = null!;

        void Inject()
        {

        }


    }
}