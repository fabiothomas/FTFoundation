using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Scripts.Foundation.Attributes;
using UnityEngine;

namespace Scripts.Foundation
{
    public static class ServiceProvider
    {
        private static readonly Dictionary<Type, Type> serviceCache = new();

        private static readonly Dictionary<Type, object> singletons = new();
        private static readonly Dictionary<int, Dictionary<Type, object>> scoped = new();

        private static readonly Dictionary<Type, Func<object>> serviceFactories = new();
        private static readonly Dictionary<Type, Action<object, int, ServiceTargetData>> injectionActions = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void Initialize()
        {
            // resetting static values in case of 'domain reloading' being disabled
            serviceCache.Clear();
            singletons.Clear();
            scoped.Clear();
            serviceFactories.Clear();
            injectionActions.Clear();

            List<Type> servicesToInstantiate = new();

            // handle every assembly containing services
            var serviceAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.GetCustomAttribute<ServiceAssemblyAttribute>() != null);

            foreach (Assembly assembly in serviceAssemblies)
            {
                var serviceTypes = assembly.GetTypes();

                foreach (var t in serviceTypes)
                {
                    ServiceAttribute attribute = (ServiceAttribute)t.GetCustomAttribute(typeof(ServiceAttribute), inherit: true);

                    // skip type if it doesn't utalize the ServiceAttribute
                    if (attribute == null) continue;

                    // reject type if type is struct but serviceType is not transient [currently disabled as I'm not allowing structs anyway but leaving this in case struct services become interesting in the future]
                    // if (!t.IsClass && !(attribute.Type == ServiceType.TRANSIENT)) throw new UnityException($"Service '{t.Name}' is a struct and therefore must be registered as a transient service");

                    serviceCache[attribute.Interface] = t;

                    if (t.GetCustomAttributes(typeof(InstantiateOnStartupAttribute), inherit: true).Any())
                    {
                        if (attribute.Type == ServiceType.SINGLETON) servicesToInstantiate.Add(t);
                        else Debug.LogWarning($"InstantiateOnStartupAttribute ia not valid on {t.Name} because it is not a singleton service");
                    }
                    else
                    {
                        PrecompileServiceFactory(attribute.Interface, t);
                    }
                    PrecompileInjectionAction(t);
                }
            }

            // handle every assembly containing injection targets
            var injectionTargetAssemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.GetCustomAttribute<InjectionTargetAssemblyAttribute>() != null);

            foreach (var assembly in injectionTargetAssemblies)
            {
                var injectionTargetTypes = assembly.GetTypes();

                foreach (var t in injectionTargetTypes)
                {
                    if (!t.IsSubclassOf(typeof(MonoBehaviour))) continue;

                    PrecompileInjectionAction(t);
                }
            }

            // handle every singleton service marked for injection on startup
            foreach (var t in servicesToInstantiate)
            {
                ServiceAttribute attribute = (ServiceAttribute)t.GetCustomAttribute(typeof(ServiceAttribute), inherit: true);
                if (attribute == null) continue;

                var obj = Activator.CreateInstance(t);

                ServiceTargetData target = new(t.Name, ServiceTargetDataType.SINGLETON, t, obj);

                InjectDependencies(obj, -1, target);

                singletons.Add(attribute.Interface, obj);
            }
        }

        // a service factory is used to create an instance of a given service object
        private static void PrecompileServiceFactory(Type interfaceType, Type implementationType)
        {
            var newExpression = Expression.New(implementationType);
            var lambda = Expression.Lambda<Func<object>>(newExpression);
            serviceFactories[interfaceType] = lambda.Compile(preferInterpretation: false);
        }

        // an injection action is used to scan for injectable properties and method parameters and perform the injection.
        private static void PrecompileInjectionAction(Type injectionObjectType)
        {
            var injectableProperties = injectionObjectType.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic)
                .Where(p => Attribute.IsDefined(p, typeof(InjectAttribute)))
                .ToList();
            var injectMethod = injectionObjectType.GetMethod("Inject", BindingFlags.Instance | BindingFlags.NonPublic);

            if (injectableProperties.Count != 0 || injectMethod != null)
            {
                injectionActions[injectionObjectType] = CreateInjectionAction(injectionObjectType, injectableProperties, injectMethod);
            }
        }

        private static Action<object, int, ServiceTargetData> CreateInjectionAction(Type injectionObjectType, List<PropertyInfo> injectableProperties, MethodInfo injectMethod)
        {
            var objParameter = Expression.Parameter(typeof(object), "obj");
            var sceneIndexParameter = Expression.Parameter(typeof(int), "sceneIndex");
            var serviceTargetDataParameter = Expression.Parameter(typeof(ServiceTargetData), "target");

            var typedObj = Expression.Convert(objParameter, injectionObjectType);

            var expressions = new List<Expression>();

            foreach (var property in injectableProperties)
            {
                // get the service creation expression
                var serviceCall = Expression.Call(
                    typeof(ServiceProvider),
                    nameof(GetService),
                    null,
                    new Expression[]
                    {
                        Expression.Constant(property.PropertyType),
                        sceneIndexParameter,
                        serviceTargetDataParameter
                    }
                );

                // cast to the property type
                var castedService = Expression.Convert(serviceCall, property.PropertyType);

                // create property assignment
                var propertyAccess = Expression.Property(typedObj, property);
                var assignment = Expression.Assign(propertyAccess, castedService);

                expressions.Add(assignment);
            }

            if (injectMethod != null)
            {
                var parameters = injectMethod.GetParameters();

                // create all injection parameters
                var args = parameters.Select(p =>
                    Expression.Convert(
                        Expression.Call(
                            typeof(ServiceProvider),
                            nameof(GetService),
                            null,
                            new Expression[]
                            {
                                Expression.Constant(p.ParameterType),
                                sceneIndexParameter,
                                serviceTargetDataParameter
                            }
                        ),
                        p.ParameterType
                    )
                ).ToArray();

                // perform injection call
                var methodCall = Expression.Call(typedObj, injectMethod, args);
                expressions.Add(methodCall);
            }

            if (expressions.Count == 0)
            {
                // nothing to inject, return empty action
                return (_, _, _) => { };
            }

            var block = Expression.Block(expressions);
            var parameterExpressions = new ParameterExpression[]
            {
                objParameter,
                sceneIndexParameter,
                serviceTargetDataParameter
            };
            var lambda = Expression.Lambda<Action<object, int, ServiceTargetData>>(block, parameterExpressions);

            return lambda.Compile(preferInterpretation: false);
        }

        /// <summary>
        /// <para> This method can be called to inject services into a MonoBehaviour </para> 
        /// <para> This method should probably be used on the MonoBehaviour itself in the 'Awake()' method passing 'this' as it's parameter </para>
        /// <para> Ensure that the target MonoBehaviour has a 'void Inject()' method </para>
        /// </summary>
        /// <param name="instance">Target MonoBehaviour instance</param>
        public static void Inject(MonoBehaviour instance)
        {
            ServiceTargetData target = new(instance.name, ServiceTargetDataType.MONOBEHAVIOUR, instance.GetType(), instance);
            InjectDependencies(instance, instance.gameObject.scene.buildIndex, target);
        }

        private static void InjectDependencies(object obj, int sceneIndex, ServiceTargetData target)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));

            Type type = obj.GetType();

            if (injectionActions.TryGetValue(type, out var injectionAction))
            {
                injectionAction(obj, sceneIndex, target);
            }
        }

        private static object GetService(Type _interface, int sceneIndex, ServiceTargetData target)
        {
            if (_interface == typeof(IServiceTargetData)) return target;

            if (!serviceCache.TryGetValue(_interface, out Type service))
            {
                throw new UnityException($"Service '{_interface.Name}' is not a registered service. Please ensure the service is created correctly");
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

        private static object CreateService(Type service, Type serviceInterface, int sceneIndex, ServiceTargetDataType dataType, ServiceTargetData target)
        {
            if (!serviceFactories.TryGetValue(serviceInterface, out var factory))
            {
                throw new InvalidOperationException($"No implementation registered for {serviceInterface.Name}");
            }

            var obj = factory();

            if (target.IsUnknown()) target = new(service.Name, dataType, service, obj);

            InjectDependencies(obj, sceneIndex, target);

            return obj;
        }
    }
}