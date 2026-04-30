using System;

namespace FTFoundation.Core
{
    /// <summary>
    /// Marks a private property to be populated from the layered appsettings JSON files
    /// before any service dependencies are injected.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class ConfigAttribute : Attribute
    {
        /// <summary>
        /// When true, a missing config value throws an error at startup instead of being silently skipped.
        /// </summary>
        public bool Required { get; }

        public ConfigAttribute(bool required = false)
        {
            Required = required;
        }
    }
}
