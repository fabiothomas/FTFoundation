using System.Collections.Generic;
using FTFoundation.BuildInReferences;
using FTFoundation.Core;

namespace FTFoundation.BuildInServices
{
    // The sole ILoggerService implementation: a facade that relays every call to all currently-active
    // ILoggerSink instances. Stays TRANSIENT (like the sinks it wraps) so each injection target still
    // gets its own correctly-prefixed set of sinks — the caller's IServiceTargetData propagates through
    // this instance's own multi-service resolution of IReadOnlyList<ILoggerSink>.
    [Service(typeof(ILoggerService), ServiceType.TRANSIENT)]
    public class LoggerService : ILoggerService
    {
        private IReadOnlyList<ILoggerSink> _sinks = null!;

        public bool Disabled { get; set; }

        void Inject(IReadOnlyList<ILoggerSink> sinks)
        {
            _sinks = sinks;
        }

        public void Log(string message)
        {
            if (Disabled) return;
            foreach (var sink in _sinks) sink.Log(message);
        }

        public void LogWarning(string message)
        {
            if (Disabled) return;
            foreach (var sink in _sinks) sink.LogWarning(message);
        }

        public void LogError(string message)
        {
            if (Disabled) return;
            foreach (var sink in _sinks) sink.LogError(message);
        }
    }
}
