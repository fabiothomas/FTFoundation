namespace FTFoundation.BuildInReferences
{
    /// <summary>
    /// <para>A service used for logging messages, warnings, and errors.</para>
    /// <para>This service provides a way to log messages in different ways depending on the active logging service(s), which can be useful for debugging and monitoring the application.</para>
    /// </summary>
    public interface ILoggerService
    {
        /// <summary>
        /// <para>Indicates whether logging is disabled. When set to true, all log messages will be ignored for this instance of ILoggerService.</para>
        /// <para>This can be useful for temporarily silencing log output without having to change the logging service(s) or remove log statements from the code.</para>
        /// </summary>
        public bool Disabled { get; set; }

        /// <summary>
        /// <para>Logs a message using this ILoggerService instance. The actual output of the log message will depend on the active logging service(s).</para>
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void Log(string message);

        /// <summary>
        /// <para>Logs a warning message using this ILoggerService instance. The actual output of the log message will depend on the active logging service(s).</para>
        /// </summary>
        /// <param name="message">The warning message to log.</param>
        public void LogWarning(string message);

        /// <summary>
        /// <para>Logs an error message using this ILoggerService instance. The actual output of the log message will depend on the active logging service(s).</para>
        /// </summary>
        /// <param name="message">The error message to log.</param>
        public void LogError(string message);
    }
}