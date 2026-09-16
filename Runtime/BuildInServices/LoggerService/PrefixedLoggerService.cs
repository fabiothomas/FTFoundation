using FTFoundation.BuildInReferences;
using FTFoundation.Core;

namespace FTFoundation.BuildInServices
{
  public abstract class PrefixedLoggerService : ILoggerSink
  {
    [Config] protected string MonoColor { get; set; }
    [Config] protected string SystemColor { get; set; }
    [Config] protected string SingletonColor { get; set; }
    [Config] protected string ScopedColor { get; set; }
    [Config] protected string FoundationColor { get; set; }
    [Config] protected string DefaultColor { get; set; }
    [Config] protected string LogColor { get; set; }
    [Config] protected string WarningColor { get; set; }
    [Config] protected string ErrorColor { get; set; }

    private string prefix = null!;

    protected void ApplyPrefix(IServiceTargetData targetData)
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

    protected string FormatMessage(string message, string color)
    {
      return $"{prefix} <color={color}>{message}</color>";
    }

    public abstract void Log(string message);
    public abstract void LogWarning(string message);
    public abstract void LogError(string message);
  }
}
