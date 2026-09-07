namespace FTFoundation.BuildInReferences
{
    /// <summary>
    /// <para>A single logging destination that <see cref="ILoggerService"/> forwards every message to.</para>
    /// <para>Implement this — rather than <see cref="ILoggerService"/> itself — to add a new logging destination
    /// (a remote log collector, a custom file format, an in-game console, etc.). Any active <see cref="ILoggerSink"/>
    /// is automatically picked up by <c>ILoggerService</c>'s fan-out alongside the built-in console/screen/file sinks,
    /// filtered by the usual <c>[ServiceBuildProfile]</c>/<c>[ServiceBuildPlatform]</c> attributes.</para>
    /// </summary>
    public interface ILoggerSink
    {
        /// <summary>Writes a message to this sink.</summary>
        /// <param name="message">The message to log.</param>
        public void Log(string message);

        /// <summary>Writes a warning message to this sink.</summary>
        /// <param name="message">The warning message to log.</param>
        public void LogWarning(string message);

        /// <summary>Writes an error message to this sink.</summary>
        /// <param name="message">The error message to log.</param>
        public void LogError(string message);
    }
}
