using FTFoundation.Core;
using FTFoundation.BuildInReferences;

namespace FTFoundation.BuildInServices
{

    [ServiceBuildProfile(BuildTargetProfile.Editor | BuildTargetProfile.Development)]
    [Service(typeof(ILoggerSink), ServiceType.TRANSIENT)]
    [ServicePriority(0)]
    public class ScreenLoggerService : PrefixedLoggerService
    {
        [Inject] private IDebugScreenService DebugScreen { get; set; } = null!;

        void Inject(IServiceTargetData targetData) => ApplyPrefix(targetData);

        public override void Log(string message)
        {
            DebugScreen.Print(FormatMessage(message, LogColor));
        }

        public override void LogWarning(string message)
        {
            DebugScreen.Print(FormatMessage(message, WarningColor));
        }

        public override void LogError(string message)
        {
            DebugScreen.Print(FormatMessage(message, ErrorColor));
        }
    }
}
