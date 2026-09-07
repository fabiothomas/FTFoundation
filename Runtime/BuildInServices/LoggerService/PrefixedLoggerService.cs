using FTFoundation.BuildInReferences;
using FTFoundation.Core;

namespace FTFoundation.BuildInServices
{
    // Shared prefix/color formatting for sinks that tag each message with the requesting
    // service's target kind (MonoBehaviour, singleton, scoped, etc.) before writing it out.
    // [Config] properties stay resolvable per-concrete-type (ConfigLoader keys off the concrete
    // service type, not the declaring type), and "protected" is required here, not "private" —
    // reflection-based injection only sees inherited protected/public members, not private ones.
    public abstract class PrefixedLoggerService : ILoggerSink
    {
        [Config] protected string MonoColor { get; set; } = null!;
        [Config] protected string SystemColor { get; set; } = null!;
        [Config] protected string SingletonColor { get; set; } = null!;
        [Config] protected string ScopedColor { get; set; } = null!;
        [Config] protected string FoundationColor { get; set; } = null!;
        [Config] protected string DefaultColor { get; set; } = null!;
        [Config] protected string LogColor { get; set; } = null!;
        [Config] protected string WarningColor { get; set; } = null!;
        [Config] protected string ErrorColor { get; set; } = null!;

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
