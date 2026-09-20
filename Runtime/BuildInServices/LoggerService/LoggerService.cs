using System.Collections.Generic;
using FTFoundation.BuildInReferences;
using FTFoundation.Core;
using UnityEngine;

namespace FTFoundation.BuildInServices
{
  [Service(typeof(ILoggerService), ServiceType.TRANSIENT)]
  public class LoggerService : ILoggerService
  {
    [Inject] IReadOnlyList<ILoggerSink> LoggerSinks { get; set; }

    public bool Disabled { get; set; }

    [HideInCallstack]
    public void Log(string message)
    {
      if (Disabled) return;
      foreach (var sink in LoggerSinks) sink.Log(message);
    }

    [HideInCallstack]
    public void LogWarning(string message)
    {
      if (Disabled) return;
      foreach (var sink in LoggerSinks) sink.LogWarning(message);
    }

    [HideInCallstack]
    public void LogError(string message)
    {
      if (Disabled) return;
      foreach (var sink in LoggerSinks) sink.LogError(message);
    }
  }
}
