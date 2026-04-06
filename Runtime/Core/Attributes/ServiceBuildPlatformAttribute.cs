using System;

namespace FTFoundation.Core
{
    /// <summary>
    /// Restricts a service to one or more target platforms.
    /// Services without this attribute are active on all platforms (equivalent to <see cref="BuildTargetPlatform.All"/>).
    ///
    /// Platform groups (<c>Desktop</c>, <c>Mobile</c>, <c>Console</c>) can be used to target an entire
    /// family of platforms without enumerating each one. Specific flags (<c>Windows</c>, <c>Android</c>, etc.)
    /// can be combined with groups or used alone.
    /// </summary>
    /// <example>
    /// // Active on all desktop platforms
    /// [ServiceBuildPlatform(BuildTargetPlatform.Desktop)]
    /// [Service(typeof(ISteamService), ServiceType.SINGLETON)]
    /// public class SteamService : ISteamService { ... }
    ///
    /// // Active only on Windows and macOS
    /// [ServiceBuildPlatform(BuildTargetPlatform.Windows | BuildTargetPlatform.macOS)]
    /// [Service(typeof(IDiscordService), ServiceType.SINGLETON)]
    /// public class DiscordService : IDiscordService { ... }
    /// </example>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ServiceBuildPlatformAttribute : Attribute
    {
        public readonly BuildTargetPlatform Platforms;

        public ServiceBuildPlatformAttribute(BuildTargetPlatform platforms) => Platforms = platforms;
    }
}
