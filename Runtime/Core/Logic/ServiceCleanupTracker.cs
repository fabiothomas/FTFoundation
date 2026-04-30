#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace FTFoundation.Core
{
    internal sealed class ServiceCleanupTracker : MonoBehaviour
    {
        private readonly List<IServiceCleanup> _services = new();

        internal void AddServices(List<object> candidates)
        {
            foreach (var candidate in candidates)
                if (candidate is IServiceCleanup sc) _services.Add(sc);
        }

        private void OnDestroy()
        {
            foreach (var service in _services)
                service.OnCleanup();
        }
    }
}
