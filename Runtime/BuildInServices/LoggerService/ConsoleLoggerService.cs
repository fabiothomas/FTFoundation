using FTFoundation.Core;
using FTFoundation.BuildInReferences;
using UnityEngine;

namespace FTFoundation.BuildInServices
{
#if UNITY_EDITOR && UNITY_STANDALONE
  [ServiceBuildProfile(BuildTargetProfile.Editor)]
  [ServiceBuildPlatform(BuildTargetPlatform.Desktop)]
  [Service(typeof(ILoggerSink), ServiceType.TRANSIENT)]
  [ServicePriority(2)]
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
#endif
}
