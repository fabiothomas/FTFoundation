using System.Collections.Generic;
using Scripts.Foundation.Attributes;
using Scripts.References.Interfaces;
using Scripts.References.Events;
using UnityEngine;
using UnityEngine.Events;

namespace Scripts.Services.FoundationServices
{
    [Service(typeof(IEventService), ServiceType.SINGLETON)]
    public class EventService : IEventService
    {
        ILoggerService _loggerService;

        void Inject(ILoggerService loggerService)
        {
            _loggerService = loggerService;

            LogsDisabled = true;
        }

        public bool LogsDisabled
        {
            get => _loggerService.Disabled;
            set => _loggerService.Disabled = value;
        }

        public Dictionary<EventName, UnityEventBase> eventList = new();

        public void SubscribeTo(EventName eventName, UnityAction action)
        {
            if (!LogsDisabled) _loggerService.Log($"<i>{eventName}</i> was subscribed to");
            TryGetEvent(eventName).AddListener(action);
        }
        public void SubscribeTo<T>(EventName eventName, UnityAction<T> action)
        {
            if (!LogsDisabled) _loggerService.Log($"<i>{eventName}</i> was subscribed to");
            TryGetEvent<T>(eventName).AddListener(action);
        }
        public void SubscribeTo<T0, T1>(EventName eventName, UnityAction<T0, T1> action)
        {
            if (!LogsDisabled) _loggerService.Log($"<i>{eventName}</i> was subscribed to");
            TryGetEvent<T0, T1>(eventName).AddListener(action);
        }
        public void SubscribeTo<T0, T1, T2>(EventName eventName, UnityAction<T0, T1, T2> action)
        {
            if (!LogsDisabled) _loggerService.Log($"<i>{eventName}</i> was subscribed to");
            TryGetEvent<T0, T1, T2>(eventName).AddListener(action);
        }
        public void SubscribeTo<T0, T1, T2, T3>(EventName eventName, UnityAction<T0, T1, T2, T3> action)
        {
            if (!LogsDisabled) _loggerService.Log($"<i>{eventName}</i> was subscribed to");
            TryGetEvent<T0, T1, T2, T3>(eventName).AddListener(action);
        }

        public void UnsubscribeTo(EventName eventName, UnityAction action)
        {
            if (!LogsDisabled) _loggerService.Log($"<i>{eventName}</i> was unsubscribed from");
            TryGetEvent(eventName).RemoveListener(action);
        }
        public void UnsubscribeTo<T>(EventName eventName, UnityAction<T> action)
        {
            if (!LogsDisabled) _loggerService.Log($"<i>{eventName}</i> was unsubscribed from");
            TryGetEvent<T>(eventName).RemoveListener(action);
        }
        public void UnsubscribeTo<T0, T1>(EventName eventName, UnityAction<T0, T1> action)
        {
            if (!LogsDisabled) _loggerService.Log($"<i>{eventName}</i> was unsubscribed from");
            TryGetEvent<T0, T1>(eventName).RemoveListener(action);
        }
        public void UnsubscribeTo<T0, T1, T2>(EventName eventName, UnityAction<T0, T1, T2> action)
        {
            if (!LogsDisabled) _loggerService.Log($"<i>{eventName}</i> was unsubscribed from");
            TryGetEvent<T0, T1, T2>(eventName).RemoveListener(action);
        }
        public void UnsubscribeTo<T0, T1, T2, T3>(EventName eventName, UnityAction<T0, T1, T2, T3> action)
        {
            if (!LogsDisabled) _loggerService.Log($"<i>{eventName}</i> was unsubscribed from");
            TryGetEvent<T0, T1, T2, T3>(eventName).RemoveListener(action);
        }

        public void AttemptInvoke(EventName eventName)
        {
            if (!LogsDisabled) _loggerService.Log($"<i>{eventName}</i> was invoked");
            TryGetEvent(eventName).Invoke();
        }
        public void AttemptInvoke<T>(EventName eventName, T param1)
        {
            if (!LogsDisabled) _loggerService.Log($"<i>{eventName}</i> was invoked");
            TryGetEvent<T>(eventName).Invoke(param1);
        }
        public void AttemptInvoke<T0, T1>(EventName eventName, T0 param1, T1 param2)
        {
            if (!LogsDisabled) _loggerService.Log($"<i>{eventName}</i> was invoked");
            TryGetEvent<T0, T1>(eventName).Invoke(param1, param2);
        }
        public void AttemptInvoke<T0, T1, T2>(EventName eventName, T0 param1, T1 param2, T2 param3)
        {
            if (!LogsDisabled) _loggerService.Log($"<i>{eventName}</i> was invoked");
            TryGetEvent<T0, T1, T2>(eventName).Invoke(param1, param2, param3);
        }
        public void AttemptInvoke<T0, T1, T2, T3>(EventName eventName, T0 param1, T1 param2, T2 param3, T3 param4)
        {
            if (!LogsDisabled) _loggerService.Log($"<i>{eventName}</i> was invoked");
            TryGetEvent<T0, T1, T2, T3>(eventName).Invoke(param1, param2, param3, param4);
        }

        private UnityEvent TryGetEvent(EventName eventName)
        {
            if (!eventList.ContainsKey(eventName)) return (UnityEvent)(eventList[eventName] = new UnityEvent());
            if (eventList[eventName] is UnityEvent e) return e;
            throw new UnityException($"You are trying to access <b>{eventName}</b> as <b>{typeof(UnityEvent)}</b> while it is created as <b>{eventList[eventName].GetType()}</b>");
        }
        private UnityEvent<T> TryGetEvent<T>(EventName eventName)
        {
            if (!eventList.ContainsKey(eventName)) return (UnityEvent<T>)(eventList[eventName] = new UnityEvent<T>());
            if (eventList[eventName] is UnityEvent<T> e) return e;
            throw new UnityException($"You are trying to access <b>{eventName}</b> as <b>{typeof(UnityEvent<T>)}</b> while it is created as <b>{eventList[eventName].GetType()}</b>");
        }
        private UnityEvent<T0, T1> TryGetEvent<T0, T1>(EventName eventName)
        {
            if (!eventList.ContainsKey(eventName)) return (UnityEvent<T0, T1>)(eventList[eventName] = new UnityEvent<T0, T1>());
            if (eventList[eventName] is UnityEvent<T0, T1> e) return e;
            throw new UnityException($"You are trying to access <b>{eventName}</b> as <b>{typeof(UnityEvent<T0, T1>)}</b> while it is created as <b>{eventList[eventName].GetType()}</b>");
        }
        private UnityEvent<T0, T1, T2> TryGetEvent<T0, T1, T2>(EventName eventName)
        {
            if (!eventList.ContainsKey(eventName)) return (UnityEvent<T0, T1, T2>)(eventList[eventName] = new UnityEvent<T0, T1, T2>());
            if (eventList[eventName] is UnityEvent<T0, T1, T2> e) return e;
            throw new UnityException($"You are trying to access <b>{eventName}</b> as <b>{typeof(UnityEvent<T0, T1, T2>)}</b> while it is created as <b>{eventList[eventName].GetType()}</b>");
        }
        private UnityEvent<T0, T1, T2, T3> TryGetEvent<T0, T1, T2, T3>(EventName eventName)
        {
            if (!eventList.ContainsKey(eventName)) return (UnityEvent<T0, T1, T2, T3>)(eventList[eventName] = new UnityEvent<T0, T1, T2, T3>());
            if (eventList[eventName] is UnityEvent<T0, T1, T2, T3> e) return e;
            throw new UnityException($"You are trying to access <b>{eventName}</b> as <b>{typeof(UnityEvent<T0, T1, T2, T3>)}</b> while it is created as <b>{eventList[eventName].GetType()}</b>");
        }
    }
}