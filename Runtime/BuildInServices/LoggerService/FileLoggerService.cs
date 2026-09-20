using System;
using System.Collections.Generic;
using System.Text;
using FTFoundation.Core;
using FTFoundation.BuildInReferences;
using UnityEngine;

namespace FTFoundation.BuildInServices
{
#if !DEVELOPMENT_BUILD && UNITY_STANDALONE
  [ServiceBuildProfile(BuildTargetProfile.Production | BuildTargetProfile.Staging | BuildTargetProfile.Editor)]
  [ServiceBuildPlatform(BuildTargetPlatform.Desktop)]
  [Service(typeof(ILoggerSink), ServiceType.TRANSIENT)]
  [ServicePriority(1)]
  public class FileLoggerService : ILoggerSink, IServiceCleanup
  {
    private const string LogPath = "logs/session.log";

    [Inject] IFileService FileService { get; set; }
    private readonly List<string> _buffer = new();

    void Inject()
    {
      Application.quitting += OnApplicationQuit;
    }

    public void OnCleanup()
    {
      Application.quitting -= OnApplicationQuit;
      OnApplicationQuit(); // flush whatever's still buffered rather than discard it
    }

    public void Log(string message)
    {
      _buffer.Add(FormatLine("INFO", message));
    }

    public void LogWarning(string message)
    {
      _buffer.Add(FormatLine("WARN", message));
    }

    public void LogError(string message)
    {
      _buffer.Add(FormatLine("ERROR", message));
    }

    private void OnApplicationQuit()
    {
      if (_buffer.Count == 0) return;
      var sb = new StringBuilder();
      foreach (string line in _buffer)
        sb.AppendLine(line);
      FileService.Write(LogPath, sb.ToString());
    }

    private static string FormatLine(string level, string message)
    {
      return $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
    }
  }
#endif
}
