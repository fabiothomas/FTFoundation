using System;

namespace Scripts.Foundation.Attributes
{
    /// <summary>
    /// This attribute defines a service that can be injected
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ServiceAttribute : Attribute
    {
        public readonly Type Interface;
        public readonly ServiceType Type;
        public ServiceAttribute(Type _interface, ServiceType _type)
        {
            Interface = _interface;
            Type = _type;
        }
    }

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