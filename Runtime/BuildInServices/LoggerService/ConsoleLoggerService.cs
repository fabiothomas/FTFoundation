using FTFoundation.Core;
using FTFoundation.BuildInReferences;
using UnityEngine;

namespace FTFoundation.BuildInServices
{

  [ServiceBuildProfile(BuildTargetProfile.Editor)]
  [ServiceBuildPlatform(BuildTargetPlatform.Desktop)]
  [Service(typeof(ILoggerSink), ServiceType.TRANSIENT)]
  public class ConsoleLoggerService : PrefixedLoggerService
  {
    void Inject(IServiceTargetData targetData) => ApplyPrefix(targetData);

    [HideInCallstack]
    public override void Log(string message)
    {
      Debug.Log(FormatMessage(message, LogColor));
    }

    [HideInCallstack]
    public override void LogWarning(string message)
    {
      Debug.LogWarning(FormatMessage(message, WarningColor));
    }

    [HideInCallstack]
    public override void LogError(string message)
    {
      Debug.LogError(FormatMessage(message, ErrorColor));
    }
  }
}
