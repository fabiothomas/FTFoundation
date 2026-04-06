using System;

namespace FTFoundation.Core
{
    /// <summary>
    /// Restricts a service to one or more build target profiles.
    /// Services without this attribute are active in all profiles (equivalent to <see cref="BuildTargetProfile.All"/>).
    /// </summary>
    /// <example>
    /// // Only active in Development builds and in the Editor
    /// [ServiceBuildProfile(BuildTargetProfile.Development | BuildTargetProfile.Editor)]
    /// [Service(typeof(ILoggerService), ServiceType.TRANSIENT)]
    /// public class ScreenLogger : ILoggerService { ... }
    /// </example>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ServiceBuildProfileAttribute : Attribute
    {
        public readonly BuildTargetProfile Profiles;

        public ServiceBuildProfileAttribute(BuildTargetProfile profiles) => Profiles = profiles;
    }
}
