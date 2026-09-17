using System;
using System.Collections.Generic;
using FTFoundation.BuildInReferences;
using FTFoundation.Core;

namespace FTFoundation.BuildInServices
{
  [Service(typeof(IEventService), ServiceType.SINGLETON)]
  public class EventService : IEventService
  {
    private readonly Dictionary<Type, Delegate> handlers = new();

    [Inject] private ILoggerService LoggerService { get; set; }

    void Inject()
    {
      LogsDisabled = true;
    }

    public bool LogsDisabled
    {
      get => LoggerService.Disabled;
      set => LoggerService.Disabled = value;
    }

    public IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IEvent
    {
      Type key = typeof(TEvent);
      if (!LogsDisabled) LoggerService.Log($"<i>{key.Name}</i> was subscribed to");

      handlers[key] = handlers.TryGetValue(key, out Delegate existing)
        ? Delegate.Combine(existing, handler)
        : handler;

      return new DelegateDisposable(() => Unsubscribe(handler));
    }

    public void Invoke<TEvent>(TEvent eventInstance) where TEvent : IEvent
    {
      Type key = typeof(TEvent);
      if (!LogsDisabled) LoggerService.Log($"<i>{key.Name}</i> was published");

      if (handlers.TryGetValue(key, out Delegate existing) && existing is Action<TEvent> action)
        action.Invoke(eventInstance);
    }

    private void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : IEvent
    {
      Type key = typeof(TEvent);
      if (!handlers.TryGetValue(key, out Delegate existing)) return;

      if (!LogsDisabled) LoggerService.Log($"<i>{key.Name}</i> was unsubscribed from");

      Delegate combined = Delegate.Remove(existing, handler);
      if (combined == null) handlers.Remove(key);
      else handlers[key] = combined;
    }
  }
}
