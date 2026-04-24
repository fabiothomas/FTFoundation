using FTFoundation.BuildInReferences;
using FTFoundation.Core;
using UnityEngine;

namespace BuildInServices
{
    [Service(typeof(IDedicatedObjectService), ServiceType.TRANSIENT)]
    public class DedicatedObjectService : IDedicatedObjectService
    {
        private GameObject This { get; set; } = null!;
        void Inject(IServiceTargetData targetData)
        {
            This = new GameObject($"{targetData.Name}");
            Object.DontDestroyOnLoad(This);
        }

        public GameObject Get()
        {
            return This;
        }
    }
}