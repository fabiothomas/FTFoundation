using FTFoundation.Core;
using FTFoundation.BuildInReferences;

namespace FTFoundation.BuildInServices
{

    [ServiceFallback]
    [Service(typeof(ILoggerService), ServiceType.SINGLETON)]
    public class NullLoggerService : ILoggerService
    {
        public bool Disabled { get; set; }
        public void Log(string message) { }
        public void LogWarning(string message) { }
        public void LogError(string message) { }
    }
}