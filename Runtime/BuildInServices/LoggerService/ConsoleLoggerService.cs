using FTFoundation.Core;
using FTFoundation.BuildInReferences;
using UnityEngine;

namespace FTFoundation.BuildInServices
{

  [ServiceBuildProfile(BuildTargetProfile.Editor)]
  [ServiceBuildPlatform(BuildTargetPlatform.Desktop)]
  [ServicePriority(10)]
  [Service(typeof(ILoggerService), ServiceType.TRANSIENT)]
  public class ConsoleLoggerService : ILoggerService
  {
    public bool Disabled { get; set; }

    [Config] private string MonoColor { get; set; } = null!;
    [Config] private string SystemColor { get; set; } = null!;
    [Config] private string SingletonColor { get; set; } = null!;
    [Config] private string ScopedColor { get; set; } = null!;
    [Config] private string FoundationColor { get; set; } = null!;
    [Config] private string DefaultColor { get; set; } = null!;
    [Config] private string LogColor { get; set; } = null!;
    [Config] private string WarningColor { get; set; } = null!;
    [Config] private string ErrorColor { get; set; } = null!;

    private string prefix = null!;

    void Inject(IServiceTargetData targetData)
    {
      switch (targetData.DataType)
      {
        case ServiceTargetDataType.MONOBEHAVIOUR:
          SetPrefix(targetData.Name, MonoColor, "Mo");
          break;

        case ServiceTargetDataType.SYSTEM:
          SetPrefix(targetData.Name, SystemColor, "Sy");
          break;

        case ServiceTargetDataType.SINGLETON:
          SetPrefix(targetData.Name, SingletonColor, "Si");
          break;

        case ServiceTargetDataType.SCOPED:
          SetPrefix(targetData.Name, ScopedColor, "Sc");
          break;

        case ServiceTargetDataType.FT_FOUNDATION:
          SetPrefix(targetData.Name, FoundationColor, "FT");
          break;

        default:
          SetPrefix(targetData.Name, DefaultColor, "??");
          break;
      }
    }

    private void SetPrefix(string name, string color, string icon)
    {
      prefix = $"<color={color}>[{icon}]<b>[{name}]</b></color>";
    }

    private string FormatMessage(string message, string color)
    {
      return $"{prefix} <color={color}>{message}</color>";
    }

    [HideInCallstack]
    public void Log(string message)
    {
      if (Disabled) return;

      Debug.Log(FormatMessage(message, LogColor));
    }

    [HideInCallstack]
    public void LogWarning(string message)
    {
      if (Disabled) return;

      Debug.LogWarning(FormatMessage(message, WarningColor));
    }

    [HideInCallstack]
    public void LogError(string message)
    {
      if (Disabled) return;

      Debug.LogError(FormatMessage(message, ErrorColor));
    }
  }
}