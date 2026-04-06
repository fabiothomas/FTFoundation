using System;

namespace FTFoundation.Core
{
    /// <summary>
    /// This attribute defines that a property will have its content injected.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class InjectAttribute : Attribute
    {
        /// <summary>
        /// When true, a missing or unregistered service injects null instead of throwing an exception.
        /// The consuming property must be able to hold a null reference.
        /// </summary>
        public bool Optional { get; set; }
    }
}