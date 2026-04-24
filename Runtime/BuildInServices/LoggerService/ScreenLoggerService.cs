using FTFoundation.Core;
using FTFoundation.BuildInReferences;
using UnityEngine;

namespace FTFoundation.BuildInServices
{

    [ServiceBuildProfile(BuildTargetProfile.Editor | BuildTargetProfile.Development)]
    [Service(typeof(ILoggerService), ServiceType.TRANSIENT)]
    public class ScreenLoggerService : ILoggerService
    {
        [Inject] private IDebugScreen DebugScreen { get; set; } = null!;
        public bool Disabled { get; set; }

        public void Log(string message)
        {
            if (Disabled) return;

            // Implement screen logging logic here
            Debug.Log($"[ScreenLogger] {message}");
        }

        public void LogWarning(string message)
        {
            if (Disabled) return;

            // Implement screen logging logic here
            Debug.LogWarning($"[ScreenLogger] {message}");
        }

        public void LogError(string message)
        {
            if (Disabled) return;

            // Implement screen logging logic here
            Debug.LogError($"[ScreenLogger] {message}");
        }
    }
}