using System;

namespace FTFoundation.Core
{
    /// <summary>
    /// Represents the target platform for conditional service registration.
    /// Use with <see cref="ServiceBuildPlatformAttribute"/> to restrict a service to specific platforms.
    ///
    /// Group flags (Desktop, Mobile, Console) are automatically set alongside the specific platform flag
    /// by <see cref="BuildPlatformDetector"/>, so targeting a group like <c>Desktop</c> will match all
    /// standalone platforms without needing to enumerate them individually.
    /// </summary>
    [Flags]
    public enum BuildTargetPlatform
    {
        // ── Platform groups ───────────────────────────────────────────────────────────────────

        /// <summary>Windows, macOS, and Linux standalone players.</summary>
        Desktop     = 1 << 0,

        /// <summary>Android and iOS players.</summary>
        Mobile      = 1 << 1,

        /// <summary>Nintendo Switch, PlayStation, and Xbox players.</summary>
        Console     = 1 << 2,

        /// <summary>WebGL player.</summary>
        Web         = 1 << 3,

        // ── Specific platforms ────────────────────────────────────────────────────────────────

        Windows     = 1 << 4,
        macOS       = 1 << 5,
        Linux       = 1 << 6,
        Android     = 1 << 7,
        iOS         = 1 << 8,
        Switch      = 1 << 9,
        PlayStation = 1 << 10,
        Xbox        = 1 << 11,

        // ── Convenience ───────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Active on every platform. Services without a <see cref="ServiceBuildPlatformAttribute"/>
        /// default to this value.
        /// </summary>
        All = ~0
    }
}
