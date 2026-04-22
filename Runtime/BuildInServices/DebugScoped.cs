using FTFoundation.BuildInReferences;
using FTFoundation.Core;

namespace FTFoundation.BuildInServices
{
    [Service(typeof(IDebugScoped), ServiceType.TRANSIENT)]
    public class DebugScoped : IDebugScoped
    {
        [Inject] private ILoggerService Logger { get; set; } = null!;

        void Inject()
        {

        }

        public void Log()
        {
            Logger.Log("Sup Scoped");
        }
    }
}