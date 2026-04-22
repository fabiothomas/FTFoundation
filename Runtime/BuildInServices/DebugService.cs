using FTFoundation.BuildInReferences;
using FTFoundation.Core;
using UnityEngine;

namespace FTFoundation.BuildInServices
{
    [Service(typeof(IDebugService), ServiceType.SINGLETON)]
    public class DebugService : IDebugService
    {
        [Inject] IDebugScoped DebugScoped { get; set; } = null!;
        [Inject] private ILoggerService Logger { get; set; } = null!;
        void Inject()
        {

        }

        public void Log()
        {
            DebugScoped.Log();
            Logger.Log("Sup");
        }
    }
}