using Scripts.Foundation;
using Scripts.Foundation.Attributes;
using Scripts.References.Interfaces;
using UnityEngine;

namespace Scripts.Services.FoundationServices
{

    [Service(typeof(ILoggerService), ServiceType.TRANSIENT)]
    public class LoggerService : ILoggerService
    {
        void Inject(IServiceTargetData targetData)
        {
            switch (targetData.DataType)
            {
                case ServiceTargetDataType.MONOBEHAVIOUR:
                    SetPrefix(targetData.Name, "#ff7fb9ff", "Mo");
                    break;

                case ServiceTargetDataType.SYSTEM:
                    SetPrefix(targetData.Name, "#eba5ffff", "Sy");
                    break;

                case ServiceTargetDataType.SINGLETON:
                    SetPrefix(targetData.Name, "#04c3c9ff", "Si");
                    break;

                case ServiceTargetDataType.SCOPED:
                    SetPrefix(targetData.Name, "#00708cff", "Sc");
                    break;

                default:
                    SetPrefix(targetData.Name, "#edededff", "??");
                    break;
            }
        }

        private string prefix;

        private void SetPrefix(string name, string color, string icon)
        {
            prefix = $"<color={color}>[{icon}]<b>[{name}]</b></color>";
        }

        public bool Disabled { get; set; }

        public void Log(string message)
        {
            if (Disabled) return;

            Debug.Log(FormatMessage(message, "#edededff"));
        }

        public void LogWarning(string message)
        {
            if (Disabled) return;

            Debug.LogWarning(FormatMessage(message, "#cc9b05ff"));
        }

        public void LogError(string message)
        {
            if (Disabled) return;

            Debug.LogError(FormatMessage(message, "#cc5833ff"));
        }

        private string FormatMessage(string message, string color)
        {
            return $"{prefix} <color={color}>{message}</color>";
        }
    }
}