using FTFoundation.Core;
using FTFoundation.BuildInReferences;
using UnityEngine;

namespace FTFoundation.BuildInServices
{

  [ServiceBuildProfile(BuildTargetProfile.Production | BuildTargetProfile.Staging)]
  [Service(typeof(ILoggerService), ServiceType.TRANSIENT)]
  public class FileLoggerService : ILoggerService
  {
    public bool Disabled { get; set; }

    public void Log(string message)
    {
      if (Disabled) return;

      // Implement file logging logic here
      Debug.Log($"[FileLogger] {message}");
    }

    public void LogWarning(string message)
    {
      if (Disabled) return;

      // Implement file logging logic here
      Debug.LogWarning($"[FileLogger] {message}");
    }

    public void LogError(string message)
    {
      if (Disabled) return;

      // Implement file logging logic here
      Debug.LogError($"[FileLogger] {message}");
    }
  }
}