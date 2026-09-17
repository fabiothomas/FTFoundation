using System.Collections.Generic;
using FTFoundation.BuildInReferences;
using FTFoundation.Core;

namespace FTFoundation.BuildInServices
{
  [Service(typeof(ILoggerService), ServiceType.TRANSIENT)]
  public class LoggerService : ILoggerService
  {
    [Inject] IReadOnlyList<ILoggerSink> LoggerSinks { get; set; }

    public bool Disabled { get; set; }

    public void Log(string message)
    {
      if (Disabled) return;
      foreach (var sink in LoggerSinks) sink.Log(message);
    }

    public void LogWarning(string message)
    {
      if (Disabled) return;
      foreach (var sink in LoggerSinks) sink.LogWarning(message);
    }

    public void LogError(string message)
    {
      if (Disabled) return;
      foreach (var sink in LoggerSinks) sink.LogError(message);
    }
  }
}
