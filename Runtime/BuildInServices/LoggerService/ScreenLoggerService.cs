using FTFoundation.Core;
using FTFoundation.BuildInReferences;
using UnityEngine;

namespace FTFoundation.BuildInServices
{

    [ServiceBuildProfile(BuildTargetProfile.Editor | BuildTargetProfile.Development)]
    [Service(typeof(ILoggerService), ServiceType.TRANSIENT)]
    public class ScreenLoggerService : ILoggerService
    {
        [Inject] private IDebugScreenService DebugScreen { get; set; } = null!;
        public bool Disabled { get; set; }

        private string prefix = null!;

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

                case ServiceTargetDataType.FT_FOUNDATION:
                    SetPrefix(targetData.Name, "#00ff00ff", "FT");
                    break;

                default:
                    SetPrefix(targetData.Name, "#edededff", "??");
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

        public void Log(string message)
        {
            if (Disabled) return;

            DebugScreen.Print(FormatMessage(message, "#edededff"));
        }

        public void LogWarning(string message)
        {
            if (Disabled) return;

            DebugScreen.Print(FormatMessage(message, "#cc9b05ff"));
        }

        public void LogError(string message)
        {
            if (Disabled) return;

            DebugScreen.Print(FormatMessage(message, "#cc5833ff"));
        }
    }
}