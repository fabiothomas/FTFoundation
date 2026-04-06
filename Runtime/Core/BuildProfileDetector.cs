namespace FTFoundation.Core
{
    /// <summary>
    /// Resolves the current <see cref="BuildTargetProfile"/> at class initialisation using compile-time
    /// scripting define symbols. The value is fixed for the lifetime of the process.
    ///
    /// Mapping:
    ///   UNITY_EDITOR      →  BuildTargetProfile.Editor
    ///   DEVELOPMENT_BUILD →  BuildTargetProfile.Development  (Unity built-in define for dev player builds)
    ///   BUILD_STAGING     →  BuildTargetProfile.Staging       (add this define to your staging Build Profile)
    ///   (none of the above) → BuildTargetProfile.Production
    /// </summary>
    internal static class BuildProfileDetector
    {
        public static readonly BuildTargetProfile Current;

        static BuildProfileDetector()
        {
#if UNITY_EDITOR
            Current = BuildTargetProfile.Editor;
#elif DEVELOPMENT_BUILD
            Current = BuildTargetProfile.Development;
#elif BUILD_STAGING
            Current = BuildTargetProfile.Staging;
#else
            Current = BuildTargetProfile.Production;
#endif
        }
    }
}
