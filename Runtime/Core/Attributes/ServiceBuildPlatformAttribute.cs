using System;

namespace FTFoundation.Core
{
  /// <summary>
  /// <para>
  ///   Restricts a service to one or more target platforms.
  ///   Services without this attribute are active on all platforms (equivalent to <see cref="BuildTargetPlatform.All"/>).
  /// </para>
  /// <para>
  ///   Platform groups (<c>Desktop</c>, <c>Mobile</c>, <c>Console</c>) can be used to target an entire
  ///   family of platforms without enumerating each one. Specific flags (<c>Windows</c>, <c>Android</c>, etc.)
  ///   can be combined with groups or used alone.
  /// </para>
  /// </summary>
  [AttributeUsage(AttributeTargets.Class)]
  public sealed class ServiceBuildPlatformAttribute : Attribute
  {
    public readonly BuildTargetPlatform Platforms;

    public ServiceBuildPlatformAttribute(BuildTargetPlatform platforms) => Platforms = platforms;
  }
}
