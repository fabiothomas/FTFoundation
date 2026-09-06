using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace FTFoundation.Core
{
    // Responsible for service instance lifecycle: creating, caching, and serving instances
    // according to their ServiceType (TRANSIENT, SINGLETON, SCOPED).
    internal static class ServiceResolver
    {
        private static readonly Dictionary<Type, object> singletons = new();

        // Keyed by Scene.handle, which is unique per *loaded instance* of a scene — unlike
        // Scene.sceneHandle, which is shared by every additively-loaded copy of the same scene asset
        // and would otherwise let duplicate scene instances tear down each other's scoped services.
        private static readonly Dictionary<int, Dictionary<Type, object>> scoped = new();

        // Singleton/scoped stores for non-winner types in multi-service injection
        private static readonly Dictionary<Type, object> multiSingletons = new();
        private static readonly Dictionary<int, Dictionary<Type, object>> multiScoped = new();

        // Maps a scoped instance → the IServiceCleanup transients it owns, populated at creation time.
        private static readonly Dictionary<object, List<IServiceCleanup>> transientDependencies = new();

        // Set by ServiceProvider.Inject before calling InjectDependencies so that newly created
        // transients are appended to the list. Null when not in an active injection call.
        [ThreadStatic] internal static List<object>? CurrentTransientContext;

        // Concrete types currently mid-construction on this thread. A type re-entering this list
        // before its own construction has finished means A (transitively) depends on itself —
        // without this guard that recursion would blow the stack instead of failing cleanly.
        [ThreadStatic] private static List<Type>? resolutionStack;

        internal static void Clear()
        {
            singletons.Clear();
            scoped.Clear();
            multiSingletons.Clear();
            multiScoped.Clear();
            transientDependencies.Clear();
        }

        // Resolved at runtime via a pre-compiled Expression tree built in ServiceCompiler.
        // Must be internal (not private) so Expression.Call can reference it across classes.
        internal static object? GetService(Type _interface, int sceneHandle, ServiceTargetData target, bool optional)
        {
            if (_interface == typeof(IServiceTargetData)) return target;

            // IReadOnlyList<T>, IEnumerable<T>, or List<T> → return all profile-active implementations
            if (_interface.IsGenericType)
            {
                var genericDef = _interface.GetGenericTypeDefinition();
                if (genericDef == typeof(IReadOnlyList<>) ||
                    genericDef == typeof(IEnumerable<>) ||
                    genericDef == typeof(List<>))
                {
                    return GetMultiService(_interface.GetGenericArguments()[0], sceneHandle, target);
                }
            }

            if (!ServiceProvider.serviceCache.TryGetValue(_interface, out Type service))
            {
                if (optional) return null;
                throw new UnityException($"Service '{_interface.Name}' is not a registered service. Please ensure the service is created correctly or mark the dependency as optional if it is not required.");
            }

            ServiceAttribute attribute = (ServiceAttribute)service.GetCustomAttribute(typeof(ServiceAttribute), inherit: true);
            ServiceType type = attribute.Type;

            switch (type)
            {
                case ServiceType.TRANSIENT:
                    object newTransient = CreateService(service, _interface, sceneHandle, ServiceTargetDataType.NONE, target);
                    CurrentTransientContext?.Add(newTransient);
                    return newTransient;

                case ServiceType.SINGLETON:
                    if (singletons.TryGetValue(_interface, out object singletonObj)) return singletonObj;

                    sceneHandle = -1;

                    object newSingleton = CreateService(service, _interface, sceneHandle, ServiceTargetDataType.SINGLETON, ServiceTargetData.EmptyServiceTargetData());
                    singletons.Add(_interface, newSingleton);
                    return newSingleton;

                case ServiceType.SCOPED:
                    if (sceneHandle < 0) throw new UnityException($"Service '{_interface.Name}' is a scoped service and cannot be injected into a singleton service");

                    if (scoped.TryGetValue(sceneHandle, out var scopedDict) && scopedDict.TryGetValue(_interface, out object scopedTransientObj)) return scopedTransientObj;

                    var outerContext = CurrentTransientContext;
                    CurrentTransientContext = new List<object>();

                    object newScoped = CreateService(service, _interface, sceneHandle, ServiceTargetDataType.SCOPED, ServiceTargetData.EmptyServiceTargetData());

                    var ownedTransients = CurrentTransientContext;
                    CurrentTransientContext = outerContext;

                    if (ownedTransients.Count > 0)
                    {
                        var cleanupList = new List<IServiceCleanup>(ownedTransients.Count);
                        foreach (var t in ownedTransients)
                            if (t is IServiceCleanup sc) cleanupList.Add(sc);
                        if (cleanupList.Count > 0)
                            transientDependencies[newScoped] = cleanupList;
                    }

                    if (scopedDict != null) scopedDict.Add(_interface, newScoped);
                    else scoped.Add(sceneHandle, new() { { _interface, newScoped } });

                    return newScoped;

                default:
                    throw new UnityException($"Service '{_interface.Name}' is implementing unknown service type '{type}'");
            }
        }

        // Returns a List<T> (cast to object) containing instances of every profile-active implementation
        // of elementType, respecting each implementation's service lifetime.
        private static object GetMultiService(Type elementType, int sceneHandle, ServiceTargetData target)
        {
            var listType = typeof(List<>).MakeGenericType(elementType);
            var list = (System.Collections.IList)Activator.CreateInstance(listType)!;

            if (!ServiceProvider.multiServiceCache.TryGetValue(elementType, out var serviceTypes))
                return list;

            foreach (var concreteType in serviceTypes)
            {
                ServiceAttribute attr = (ServiceAttribute)concreteType.GetCustomAttribute(typeof(ServiceAttribute), inherit: true);

                object instance;
                switch (attr.Type)
                {
                    case ServiceType.TRANSIENT:
                        instance = CreateService(concreteType, elementType, sceneHandle, ServiceTargetDataType.NONE, target);
                        CurrentTransientContext?.Add(instance);
                        break;

                    case ServiceType.SINGLETON:
                        // Reuse the single-injection winner's cached instance when it is the same concrete type
                        if (ServiceProvider.serviceCache.TryGetValue(elementType, out var winner) && winner == concreteType &&
                            singletons.TryGetValue(elementType, out var winnerInst))
                        {
                            instance = winnerInst;
                        }
                        else if (multiSingletons.TryGetValue(concreteType, out var cachedInst))
                        {
                            instance = cachedInst;
                        }
                        else
                        {
                            instance = CreateService(concreteType, elementType, sceneHandle, ServiceTargetDataType.SINGLETON, ServiceTargetData.EmptyServiceTargetData());
                            multiSingletons[concreteType] = instance;
                        }
                        break;

                    case ServiceType.SCOPED:
                        if (sceneHandle < 0) throw new UnityException($"Scoped service '{elementType.Name}' cannot be injected into a singleton");

                        if (multiScoped.TryGetValue(sceneHandle, out var multiScopedDict) && multiScopedDict.TryGetValue(concreteType, out var scopedInst))
                        {
                            instance = scopedInst;
                        }
                        else
                        {
                            var outerCtx = CurrentTransientContext;
                            CurrentTransientContext = new List<object>();

                            instance = CreateService(concreteType, elementType, sceneHandle, ServiceTargetDataType.SCOPED, ServiceTargetData.EmptyServiceTargetData());

                            var ownedCtx = CurrentTransientContext;
                            CurrentTransientContext = outerCtx;

                            if (ownedCtx.Count > 0)
                            {
                                var cleanupList = new List<IServiceCleanup>(ownedCtx.Count);
                                foreach (var t in ownedCtx)
                                    if (t is IServiceCleanup sc) cleanupList.Add(sc);
                                if (cleanupList.Count > 0)
                                    transientDependencies[instance] = cleanupList;
                            }

                            if (multiScopedDict != null) multiScopedDict[concreteType] = instance;
                            else multiScoped[sceneHandle] = new Dictionary<Type, object> { { concreteType, instance } };
                        }
                        break;

                    default:
                        throw new UnityException($"Unknown service type for '{concreteType.Name}'");
                }

                list.Add(instance);
            }

            return list;
        }

        internal static void CleanupScoped(int sceneHandle)
        {
            if (scoped.TryGetValue(sceneHandle, out var scopedDict))
            {
                foreach (var instance in scopedDict.Values)
                {
                    if (instance is IServiceCleanup sc) sc.OnCleanup();
                    if (transientDependencies.TryGetValue(instance, out var transients))
                    {
                        foreach (var t in transients) t.OnCleanup();
                        transientDependencies.Remove(instance);
                    }
                }
                scoped.Remove(sceneHandle);
            }

            if (multiScoped.TryGetValue(sceneHandle, out var multiScopedDict))
            {
                foreach (var instance in multiScopedDict.Values)
                {
                    if (instance is IServiceCleanup sc) sc.OnCleanup();
                    if (transientDependencies.TryGetValue(instance, out var transients))
                    {
                        foreach (var t in transients) t.OnCleanup();
                        transientDependencies.Remove(instance);
                    }
                }
                multiScoped.Remove(sceneHandle);
            }
        }

        private static object CreateService(Type service, Type serviceInterface, int sceneHandle, ServiceTargetDataType dataType, ServiceTargetData target)
        {
            var stack = resolutionStack ??= new List<Type>();
            if (stack.Contains(service))
            {
                string chain = string.Join(" → ", stack.Select(t => t.Name).Concat(new[] { service.Name }));
                throw new UnityException($"Circular dependency detected while resolving service '{serviceInterface.Name}': {chain}");
            }

            if (!ServiceCompiler.TryGetFactory(service, out var factory))
            {
                throw new InvalidOperationException($"No factory compiled for '{service.Name}'. Ensure the type is registered as a service.");
            }

            stack.Add(service);
            try
            {
                var obj = factory();

                if (target.IsUnknown()) target = new(service.Name, dataType, service, obj);

                ServiceProvider.InjectDependencies(obj, sceneHandle, target);

                return obj;
            }
            finally
            {
                stack.RemoveAt(stack.Count - 1);
            }
        }
    }
}
