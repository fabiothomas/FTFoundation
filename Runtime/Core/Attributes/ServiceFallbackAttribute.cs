using System;

namespace FTFoundation.Core
{
    /// <summary>
    /// Marks a service as a fallback implementation.
    /// A fallback service is only registered when no other non-fallback implementation of the same
    /// interface passes the current build profile filter. Use this to implement the null-object pattern
    /// or a safe default that is guaranteed to exist in every build.
    /// </summary>
    /// <example>
    /// [ServiceFallback]
    /// [Service(typeof(ILoggerService), ServiceType.TRANSIENT)]
    /// public class NullLoggerService : ILoggerService
    /// {
    ///     public void Log(string message) { }
    ///     public void LogWarning(string message) { }
    ///     public void LogError(string message) { }
    /// }
    /// </example>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ServiceFallbackAttribute : Attribute { }
}
