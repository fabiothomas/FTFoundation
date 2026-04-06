using UnityEngine;

namespace FTFoundation.Core
{
    /// <summary>
    /// Resolves the current <see cref="BuildTargetPlatform"/> at class initialisation using
    /// <see cref="Application.platform"/>. Both the specific platform flag and its parent group flag
    /// are set so that services targeting either level match correctly.
    ///
    /// Examples:
    ///   Windows standalone  →  BuildTargetPlatform.Windows | Desktop
    ///   Android             →  BuildTargetPlatform.Android | Mobile
    ///   PS5                 →  BuildTargetPlatform.PlayStation | Console
    /// </summary>
    internal static class BuildPlatformDetector
    {
        public static readonly BuildTargetPlatform Current;

        static BuildPlatformDetector()
        {
            Current = Application.platform switch
            {
                RuntimePlatform.WindowsPlayer or RuntimePlatform.WindowsEditor
                    => BuildTargetPlatform.Windows | BuildTargetPlatform.Desktop,

                RuntimePlatform.OSXPlayer or RuntimePlatform.OSXEditor
                    => BuildTargetPlatform.macOS | BuildTargetPlatform.Desktop,

                RuntimePlatform.LinuxPlayer or RuntimePlatform.LinuxEditor
                    => BuildTargetPlatform.Linux | BuildTargetPlatform.Desktop,

                RuntimePlatform.Android
                    => BuildTargetPlatform.Android | BuildTargetPlatform.Mobile,

                RuntimePlatform.IPhonePlayer
                    => BuildTargetPlatform.iOS | BuildTargetPlatform.Mobile,

                RuntimePlatform.WebGLPlayer
                    => BuildTargetPlatform.Web,

                RuntimePlatform.GameCoreXboxSeries or RuntimePlatform.GameCoreXboxOne or RuntimePlatform.XboxOne
                    => BuildTargetPlatform.Xbox | BuildTargetPlatform.Console,

                RuntimePlatform.PS4 or RuntimePlatform.PS5
                    => BuildTargetPlatform.PlayStation | BuildTargetPlatform.Console,

                RuntimePlatform.Switch
                    => BuildTargetPlatform.Switch | BuildTargetPlatform.Console,

                _ => BuildTargetPlatform.All
            };
        }
    }
}
