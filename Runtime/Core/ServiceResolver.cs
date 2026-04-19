#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace FTFoundation.Core
{
    // Responsible for service instance lifecycle: creating, caching, and serving instances
    // according to their ServiceType (TRANSIENT, SINGLETON, SCOPED).
    internal static class ServiceResolver
    {
        private static readonly Dictionary<Type, object> singletons = new();
        private static readonly Dictionary<int, Dictionary<Type, object>> scoped = new();

        // Singleton/scoped stores for non-winner types in multi-service injection
        private static readonly Dictionary<Type, object> multiSingletons = new();
        private static readonly Dictionary<int, Dictionary<Type, object>> multiScoped = new();

        internal static void Clear()
        {
            singletons.Clear();
            scoped.Clear();
            multiSingletons.Clear();
            multiScoped.Clear();
        }

        // Called directly by ServiceProvider for eagerly-instantiated startup singletons.
        internal static void RegisterStartupSingleton(Type iface, object instance)
        {
            singletons.Add(iface, instance);
        }

        // Resolved at runtime via a pre-compiled Expression tree built in ServiceCompiler.
        // Must be internal (not private) so Expression.Call can reference it across classes.
        internal static object? GetService(Type _interface, int sceneIndex, ServiceTargetData target, bool optional)
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
                    return GetMultiService(_interface.GetGenericArguments()[0], sceneIndex, target);
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
                    return CreateService(service, _interface, sceneIndex, ServiceTargetDataType.NONE, target);

                case ServiceType.SINGLETON:
                    if (singletons.TryGetValue(_interface, out object singletonObj)) return singletonObj;

                    sceneIndex = -1;

                    object newSingleton = CreateService(service, _interface, sceneIndex, ServiceTargetDataType.SINGLETON, ServiceTargetData.EmptyServiceTargetData());
                    singletons.Add(_interface, newSingleton);
                    return newSingleton;

                case ServiceType.SCOPED:
                    if (sceneIndex < 0) throw new UnityException($"Service '{_interface.Name}' is a scoped service and cannot be injected into a singleton service");

                    if (scoped.TryGetValue(sceneIndex, out var scopedDict) && scopedDict.TryGetValue(_interface, out object scopedTransientObj)) return scopedTransientObj;

                    object newScoped = CreateService(service, _interface, sceneIndex, ServiceTargetDataType.SCOPED, ServiceTargetData.EmptyServiceTargetData());

                    if (scopedDict != null) scopedDict.Add(_interface, newScoped);
                    else scoped.Add(sceneIndex, new() { { _interface, newScoped } });

                    return newScoped;

                default:
                    throw new UnityException($"Service '{_interface.Name}' is implementing unknown service type '{type}'");
            }
        }

        // Returns a List<T> (cast to object) containing instances of every profile-active implementation
        // of elementType, respecting each implementation's service lifetime.
        private static object GetMultiService(Type elementType, int sceneIndex, ServiceTargetData target)
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
                        instance = CreateService(concreteType, elementType, sceneIndex, ServiceTargetDataType.NONE, target);
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
                            instance = CreateService(concreteType, elementType, sceneIndex, ServiceTargetDataType.SINGLETON, ServiceTargetData.EmptyServiceTargetData());
                            multiSingletons[concreteType] = instance;
                        }
                        break;

                    case ServiceType.SCOPED:
                        if (sceneIndex < 0) throw new UnityException($"Scoped service '{elementType.Name}' cannot be injected into a singleton");

                        if (multiScoped.TryGetValue(sceneIndex, out var multiScopedDict) && multiScopedDict.TryGetValue(concreteType, out var scopedInst))
                        {
                            instance = scopedInst;
                        }
                        else
                        {
                            instance = CreateService(concreteType, elementType, sceneIndex, ServiceTargetDataType.SCOPED, ServiceTargetData.EmptyServiceTargetData());
                            if (multiScopedDict != null) multiScopedDict[concreteType] = instance;
                            else multiScoped[sceneIndex] = new Dictionary<Type, object> { { concreteType, instance } };
                        }
                        break;

                    default:
                        throw new UnityException($"Unknown service type for '{concreteType.Name}'");
                }

                list.Add(instance);
            }

            return list;
        }

        private static object CreateService(Type service, Type serviceInterface, int sceneIndex, ServiceTargetDataType dataType, ServiceTargetData target)
        {
            if (!ServiceCompiler.TryGetFactory(service, out var factory))
            {
                throw new InvalidOperationException($"No factory compiled for '{service.Name}'. Ensure the type is registered as a service.");
            }

            var obj = factory();

            if (target.IsUnknown()) target = new(service.Name, dataType, service, obj);

            ServiceProvider.InjectDependencies(obj, sceneIndex, target);

            return obj;
        }
    }
}
