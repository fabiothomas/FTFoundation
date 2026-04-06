namespace FTFoundation.Core
{
    public enum ServiceType
    {
        /// <summary>
        /// A transient service is instantiated each time it is injected. Each injection results in a unique instance
        /// </summary>
        TRANSIENT,
        /// <summary>
        /// A singleton service is instantiated only once. Each injection results in the same instance
        /// </summary>
        SINGLETON,
        /// <summary>
        /// A scoped service is instantiated once per scene. Each injection within the same scene results in the same instance
        /// </summary>
        SCOPED
    }
}