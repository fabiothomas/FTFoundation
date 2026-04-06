using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using FTFoundation.Core.Validation;
using UnityEngine;

namespace FTFoundation.Core
{
  public static class ServiceProvider
  {
    // Single-winner service resolution: interface → winning concrete type
    private static readonly Dictionary<Type, Type> serviceCache = new();

    // All profile-matched concrete types per interface, ordered by priority (for IReadOnlyList<T> injection)
    private static readonly Dictionary<Type, List<Type>> multiServiceCache = new();

    private static readonly Dictionary<Type, object> singletons = new();
    private static readonly Dictionary<int, Dictionary<Type, object>> scoped = new();

    // Singleton/scoped stores for non-winner types in multi-service injection
    private static readonly Dictionary<Type, object> multiSingletons = new();
    private static readonly Dictionary<int, Dictionary<Type, object>> multiScoped = new();

    // Keyed by concrete implementation type (not interface)
    private static readonly Dictionary<Type, Func<object>> serviceFactories = new();
    private static readonly Dictionary<Type, Action<object, int, ServiceTargetData>> injectionActions = new();

    private struct ServiceCandidate
    {
      public Type ImplementationType;
      public ServiceAttribute ServiceAttribute;
      public BuildTargetProfile Profiles;
      public BuildTargetPlatform Platforms;
      public int Priority;
      public bool IsFallback;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    private static void InitializeServiceProvider()
    {
      // resetting static values in case of 'domain reloading' being disabled
      serviceCache.Clear();
      multiServiceCache.Clear();
      singletons.Clear();
      scoped.Clear();
      multiSingletons.Clear();
      multiScoped.Clear();
      serviceFactories.Clear();
      injectionActions.Clear();

      List<Type> servicesToInstantiate = new();

      BuildTargetProfile currentProfile = BuildProfileDetector.Current;
      BuildTargetPlatform currentPlatform = BuildPlatformDetector.Current;

      // ── First pass: collect every [Service]-decorated type grouped by interface ──────────────
      var allCandidates = new Dictionary<Type, List<ServiceCandidate>>();

      foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
      {
        if (assembly.GetCustomAttribute<ServiceAssemblyAttribute>() == null) continue;

        foreach (var t in assembly.GetTypes())
        {
          ServiceAttribute svcAttr = (ServiceAttribute)t.GetCustomAttribute(typeof(ServiceAttribute), inherit: true);
          if (svcAttr == null) continue;

          ServiceCandidate candidate = new()
          {
            ImplementationType = t,
            ServiceAttribute = svcAttr,
            Profiles = t.GetCustomAttribute<ServiceBuildProfileAttribute>()?.Profiles ?? BuildTargetProfile.All,
            Platforms = t.GetCustomAttribute<ServiceBuildPlatformAttribute>()?.Platforms ?? BuildTargetPlatform.All,
            Priority = t.GetCustomAttribute<ServicePriorityAttribute>()?.Priority ?? 0,
            IsFallback = t.GetCustomAttribute<ServiceFallbackAttribute>() != null
          };

          if (!allCandidates.TryGetValue(svcAttr.Interface, out var list))
          {
            list = new List<ServiceCandidate>();
            allCandidates[svcAttr.Interface] = list;
          }
          list.Add(candidate);
        }
      }

      // ── Second pass: profile filter + conflict resolution per interface ────────────────────
      foreach (var (iface, candidates) in allCandidates)
      {
        var profileMatched = candidates
          .Where(c => !c.IsFallback && c.Profiles.HasFlag(currentProfile) && (c.Platforms & currentPlatform) != 0)
          .OrderByDescending(c => c.Priority)
          .ToList();

        var fallbacks = candidates
          .Where(c => c.IsFallback && c.Profiles.HasFlag(currentProfile) && (c.Platforms & currentPlatform) != 0)
          .OrderByDescending(c => c.Priority)
          .ToList();

        // Warn when multiple non-fallback candidates share the highest priority
        if (profileMatched.Count > 1)
        {
          int topPriority = profileMatched[0].Priority;
          var tied = profileMatched.Where(c => c.Priority == topPriority).ToList();
          if (tied.Count > 1)
          {
            var names = string.Join(", ", tied.Select(c => c.ImplementationType.Name));
            Debug.LogWarning($"[ServiceProvider] Multiple services for '{iface.Name}' share priority {topPriority} in profile '{currentProfile}': [{names}]. Using '{profileMatched[0].ImplementationType.Name}' for single injection.");
          }
        }

        // All profile-matched types are available for IReadOnlyList<T> injection
        if (profileMatched.Count > 0)
          multiServiceCache[iface] = profileMatched.Select(c => c.ImplementationType).ToList();

        // Single-winner: best profile-matched → best fallback → nothing (interface skipped this build)
        ServiceCandidate winner = profileMatched.Count > 0
          ? profileMatched[0]
          : (fallbacks.Count > 0 ? fallbacks[0] : default);

        if (winner.ImplementationType == null) continue;

        serviceCache[iface] = winner.ImplementationType;
        PrecompileServiceFactory(winner.ImplementationType);
        PrecompileInjectionAction(winner.ImplementationType);

        // Pre-compile factories/actions for non-winner profile-matched types (needed for multi-injection)
        foreach (var c in profileMatched.Skip(1))
        {
          PrecompileServiceFactory(c.ImplementationType);
          PrecompileInjectionAction(c.ImplementationType);
        }

        if (winner.ImplementationType.GetCustomAttributes(typeof(InstantiateOnStartupAttribute), inherit: true).Any())
        {
          if (winner.ServiceAttribute.Type == ServiceType.SINGLETON) servicesToInstantiate.Add(winner.ImplementationType);
          else Debug.LogWarning($"[ServiceProvider] InstantiateOnStartupAttribute is not valid on {winner.ImplementationType.Name} because it is not a singleton service");
        }
      }

      // ── Handle every assembly containing injection targets (MonoBehaviours) ──────────────
      foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
      {
        if (assembly.GetCustomAttribute<InjectionTargetAssemblyAttribute>() == null) continue;

        foreach (var t in assembly.GetTypes())
        {
          if (!t.IsSubclassOf(typeof(MonoBehaviour))) continue;
          PrecompileInjectionAction(t);
        }
      }

      // ── Eagerly instantiate startup singletons ────────────────────────────────────────────
      foreach (var t in servicesToInstantiate)
      {
        ServiceAttribute attribute = (ServiceAttribute)t.GetCustomAttribute(typeof(ServiceAttribute), inherit: true);
        if (attribute == null) continue;

        var obj = Activator.CreateInstance(t);

        ServiceTargetData target = new(t.Name, ServiceTargetDataType.SINGLETON, t, obj);

        InjectDependencies(obj, -1, target);

        singletons.Add(attribute.Interface, obj);
      }

      ServiceStackValidator.Validate(serviceCache, currentProfile);
    }

    // Factory is keyed by concrete implementation type so multiple implementations of the same
    // interface can each have their own factory.
    private static void PrecompileServiceFactory(Type implementationType)
    {
      if (serviceFactories.ContainsKey(implementationType)) return;
      var newExpression = Expression.New(implementationType);
      var lambda = Expression.Lambda<Func<object>>(newExpression);
      serviceFactories[implementationType] = lambda.Compile(preferInterpretation: false);
    }

    // an injection action is used to scan for injectable properties and method parameters and perform the injection.
    private static void PrecompileInjectionAction(Type injectionObjectType)
    {
      if (injectionActions.ContainsKey(injectionObjectType)) return;

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
        bool isOptional = property.GetCustomAttribute<InjectAttribute>()?.Optional ?? false;

        // get the service creation expression — passes optional flag so missing services can inject null
        var serviceCall = Expression.Call(
          typeof(ServiceProvider),
          nameof(GetService),
          null,
          new Expression[]
          {
            Expression.Constant(property.PropertyType),
            sceneIndexParameter,
            serviceTargetDataParameter,
            Expression.Constant(isOptional)
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

        // create all injection parameters; method parameters are never optional
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
                serviceTargetDataParameter,
                Expression.Constant(false)
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

    private static object? GetService(Type _interface, int sceneIndex, ServiceTargetData target, bool optional)
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

      if (!serviceCache.TryGetValue(_interface, out Type service))
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

      if (!multiServiceCache.TryGetValue(elementType, out var serviceTypes))
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
            if (serviceCache.TryGetValue(elementType, out var winner) && winner == concreteType &&
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
      if (!serviceFactories.TryGetValue(service, out var factory))
      {
        throw new InvalidOperationException($"No factory compiled for '{service.Name}'. Ensure the type is registered as a service.");
      }

      var obj = factory();

      if (target.IsUnknown()) target = new(service.Name, dataType, service, obj);

      InjectDependencies(obj, sceneIndex, target);

      return obj;
    }
  }
}