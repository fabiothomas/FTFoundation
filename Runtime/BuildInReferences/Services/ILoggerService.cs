namespace FTFoundation.BuildInReferences
{
  /// <summary>
  /// <para>A service used for logging messages, warnings, and errors.</para>
  /// <para>Injecting this gives you a facade that relays every call to all currently-active
  /// <see cref="ILoggerSink"/> implementations (console, on-screen overlay, file, or any of your own),
  /// filtered by the usual build-profile/platform rules. To add a new logging destination, implement
  /// <see cref="ILoggerSink"/> instead of this interface.</para>
  /// </summary>
  public interface ILoggerService
  {
    /// <summary>
    /// <para>Indicates whether logging is disabled. When set to true, all log messages will be ignored for this instance of ILoggerService.</para>
    /// <para>This can be useful for temporarily silencing log output without having to change the logging service(s) or remove log statements from the code.</para>
    /// </summary>
    public bool Disabled { get; set; }

    /// <summary>
    /// <para>Logs a message to every currently-active <see cref="ILoggerSink"/>.</para>
    /// </summary>
    /// <param name="message">The message to log.</param>
    public void Log(string message);

    /// <summary>
    /// <para>Logs a warning message to every currently-active <see cref="ILoggerSink"/>.</para>
    /// </summary>
    /// <param name="message">The warning message to log.</param>
    public void LogWarning(string message);

    /// <summary>
    /// <para>Logs an error message to every currently-active <see cref="ILoggerSink"/>.</para>
    /// </summary>
    /// <param name="message">The error message to log.</param>
    public void LogError(string message);
  }
}