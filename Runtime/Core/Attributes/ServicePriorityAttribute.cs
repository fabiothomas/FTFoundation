using System;

namespace FTFoundation.Core
{
    /// <summary>
    /// Sets the resolution priority for a service when multiple implementations of the same interface
    /// are active in the current build profile. Higher values win. Default priority is 0.
    /// </summary>
    /// <remarks>
    /// When two or more services for the same interface share the highest priority, a warning is logged
    /// and the first one found (by assembly scan order) is used for single-service injection.
    /// All tied implementations are still included when injecting <see cref="System.Collections.Generic.IReadOnlyList{T}"/>.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ServicePriorityAttribute : Attribute
    {
        public readonly int Priority;

        public ServicePriorityAttribute(int priority) => Priority = priority;
    }
}
