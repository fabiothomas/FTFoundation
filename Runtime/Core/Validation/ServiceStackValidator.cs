using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FTFoundation.Core.Validation
{
    internal static class ServiceStackValidator
    {
        public static void Validate(IReadOnlyDictionary<Type, Type> serviceCache, BuildTargetProfile currentProfile)
        {
            if (serviceCache.Count == 0)
            {
                Debug.LogWarning($"[ServiceProvider] No services registered for profile '{currentProfile}'. Ensure at least one [ServiceAssembly] assembly contains [Service] decorated types.");
                return;
            }

#if UNITY_EDITOR
            var lines = serviceCache
                .OrderBy(kvp => kvp.Key.Name)
                .Select(kvp => $"  {kvp.Key.Name} → {kvp.Value.Name}");
            Debug.Log($"[ServiceProvider] {serviceCache.Count} service(s) active in profile '{currentProfile}':\n{string.Join("\n", lines)}");
#endif
        }
    }
}
