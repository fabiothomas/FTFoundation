using System;

namespace FTFoundation.Core
{
    /// <summary>
    /// Represents the build target profile for conditional service registration.
    /// Use with <see cref="ServiceBuildProfileAttribute"/> to restrict a service to specific builds.
    /// </summary>
    [Flags]
    public enum BuildTargetProfile
    {
        /// <summary>Running in the Unity Editor (play mode or editor tooling).</summary>
        Editor = 1 << 0,

        /// <summary>
        /// A development player build. Corresponds to Unity's built-in DEVELOPMENT_BUILD scripting define.
        /// </summary>
        Development = 1 << 1,

        /// <summary>
        /// A staging/QA player build. Requires the user-defined scripting symbol BUILD_STAGING to be set
        /// in the Build Profile's scripting defines.
        /// </summary>
        Staging = 1 << 2,

        /// <summary>A production/release player build (default when no other define matches).</summary>
        Production = 1 << 3,

        /// <summary>
        /// Active in every profile. Services without a <see cref="ServiceBuildProfileAttribute"/> default to this value.
        /// </summary>
        All = ~0
    }
}
