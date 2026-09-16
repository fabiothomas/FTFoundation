using System;

namespace FTFoundation.Core
{
  /// <summary>
  /// Restricts a service to one or more build target profiles.
  /// Services without this attribute are active in all profiles (equivalent to <see cref="BuildTargetProfile.All"/>).
  /// </summary>
  [AttributeUsage(AttributeTargets.Class)]
  public sealed class ServiceBuildProfileAttribute : Attribute
  {
    public readonly BuildTargetProfile Profiles;

    public ServiceBuildProfileAttribute(BuildTargetProfile profiles) => Profiles = profiles;
  }
}
