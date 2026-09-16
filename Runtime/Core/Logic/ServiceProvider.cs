using System;
using System.Collections.Generic;
using System.Reflection;
using FTFoundation.BuildInReferences;
using FTFoundation.Core.Validation;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FTFoundation.Core
{
  public static class ServiceProvider
  {
    // Single-winner service resolution: interface -> winning concrete type
    internal static readonly Dictionary<Type, Type> serviceCache = new();

    // All profile-matched concrete types per interface, ordered by priority (for IReadOnlyList<T> injection)
    internal static readonly Dictionary<Type, List<Type>> multiServiceCache = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    private static void InitializeServiceProvider()
    {
      // Resetting static values in case of 'domain reloading' being disabled
      serviceCache.Clear();
      multiServiceCache.Clear();
      ServiceResolver.Clear();
      ServiceCompiler.Clear();
      ConfigLoader.Clear();
      SceneManager.sceneUnloaded -= OnSceneUnloaded;
      SceneManager.sceneUnloaded += OnSceneUnloaded;

      var problems = new List<ProblemDetail>();

      BuildTargetProfile currentProfile = BuildProfileDetector.Current;
      BuildTargetPlatform currentPlatform = BuildPlatformDetector.Current;

      ConfigLoader.Initialize(currentProfile);

      // ── Resolve service candidates ────────────────────────────────────────────────────────
      var resolved = ServiceCandidateResolver.Resolve(currentProfile, currentPlatform);

      problems.AddRange(resolved.Warnings);

      // ── Populate lookup caches ────────────────────────────────────────────────────────────
      foreach (var (iface, winnerType) in resolved.Winners)
        serviceCache[iface] = winnerType;

      foreach (var (iface, matchedTypes) in resolved.AllMatched)
        multiServiceCache[iface] = matchedTypes;

      // ── Pre-compile factories and injection actions ───────────────────────────────────────
      foreach (var (iface, winnerType) in resolved.Winners)
      {
        try
        {
          ServiceCompiler.PrecompileFactory(winnerType);
          ServiceCompiler.PrecompileInjectionAction(winnerType);
        }
        catch (Exception e)
        {
          serviceCache.Remove(iface);
          multiServiceCache.Remove(iface);
          problems.Add(new ProblemDetail(ProblemDetailType.ERROR, $"Failed to compile service '{winnerType.Name}' for '{iface.Name}': {e.Message}"));
        }
      }

      foreach (var (iface, matchedTypes) in resolved.AllMatched)
      {
        for (int i = matchedTypes.Count - 1; i >= 0; i--)
        {
          var type = matchedTypes[i];
          try
          {
            ServiceCompiler.PrecompileFactory(type);
            ServiceCompiler.PrecompileInjectionAction(type);
          }
          catch (Exception e)
          {
            matchedTypes.RemoveAt(i);
            if (matchedTypes.Count == 0) multiServiceCache.Remove(iface);
            problems.Add(new ProblemDetail(ProblemDetailType.ERROR, $"Failed to compile multi-service '{type.Name}' for '{iface.Name}': {e.Message}"));
          }
        }
      }

      // ── Pre-compile injection actions for MonoBehaviours in injection-target assemblies ───
      foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
      {
        if (assembly.GetCustomAttribute<InjectionTargetAssemblyAttribute>() == null) continue;

        foreach (var t in assembly.GetTypes())
        {
          if (!t.IsSubclassOf(typeof(MonoBehaviour))) continue;
          try
          {
            ServiceCompiler.PrecompileInjectionAction(t);
          }
          catch (Exception e)
          {
            problems.Add(new ProblemDetail(ProblemDetailType.ERROR, $"Failed to compile injection action for MonoBehaviour '{t.Name}': {e.Message}"));
          }
        }
      }

      // ── Eagerly instantiate startup singletons ────────────────────────────────────────────
      foreach (var t in resolved.EagerStartups)
      {
        ServiceAttribute attribute = (ServiceAttribute)t.GetCustomAttribute(typeof(ServiceAttribute), inherit: true);
        if (attribute == null) continue;

        try
        {
          ServiceResolver.GetService(attribute.Interface, -1, ServiceTargetData.EmptyServiceTargetData(), optional: false);
        }
        catch (Exception e)
        {
          serviceCache.Remove(attribute.Interface);
          problems.Add(new ProblemDetail(ProblemDetailType.ERROR, $"Failed to instantiate startup singleton '{t.Name}': {e.Message}"));
        }
      }

      ServiceStackValidator.Validate(serviceCache, multiServiceCache, currentProfile, problems);

      // ── Validate required config values ──────────────────────────────────────────────────
      foreach (var serviceType in serviceCache.Values)
      {
        foreach (var prop in serviceType.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic))
        {
          var configAttr = prop.GetCustomAttribute<ConfigAttribute>();
          if (configAttr?.Required == true && !ConfigLoader.TryGetValue(serviceType, prop.Name, out _))
            problems.Add(new ProblemDetail(ProblemDetailType.ERROR,
              $"Service '{serviceType.Name}' requires config value '{ConfigLoader.GetServiceKey(serviceType)}.{ConfigLoader.LowercaseFirst(prop.Name)}' which is not defined in any appsettings file."));
        }
      }

      // ── Flush collected diagnostics via ILoggerService ───────────────────────────────────
      if (problems.Count > 0)
        FlushProblems(problems);
    }

    /// <summary>
    /// <para> This method can be called to inject services into a MonoBehaviour </para> 
    /// <para> This method should probably be used on the MonoBehaviour itself in the 'Awake()' method passing 'this' as it's parameter </para>
    /// </summary>
    /// <param name="instance">Target MonoBehaviour instance</param>
    public static void Inject(MonoBehaviour instance)
    {
      ServiceTargetData target = new(instance.name, ServiceTargetDataType.MONOBEHAVIOUR, instance.GetType(), instance);

      ServiceResolver.CurrentTransientContext = new List<object>();
      List<object> transients;
      try
      {
        InjectDependencies(instance, instance.gameObject.scene.handle, target);
      }
      finally
      {
        // Must run even if InjectDependencies threw
        transients = ServiceResolver.CurrentTransientContext;
        ServiceResolver.CurrentTransientContext = null;
      }

      if (transients.Count > 0)
      {
        if (!instance.gameObject.TryGetComponent<ServiceCleanupTracker>(out var tracker)) tracker = instance.gameObject.AddComponent<ServiceCleanupTracker>();
        tracker.AddServices(transients);
      }
    }

    private static void OnSceneUnloaded(Scene scene)
    {
      ServiceResolver.CleanupScoped(scene.handle);
    }

    internal static void InjectDependencies(object obj, int sceneHandle, ServiceTargetData target)
    {
      if (obj == null) throw new ArgumentNullException(nameof(obj));

      Type type = obj.GetType();

      if (ServiceCompiler.TryGetInjectionAction(type, out var injectionAction))
      {
        injectionAction(obj, sceneHandle, target);
      }
    }

    internal static void FlushProblems(List<ProblemDetail> problems)
    {
      IReadOnlyList<ILoggerService>? loggers;
      try
      {
        loggers = ServiceResolver.GetService(typeof(IReadOnlyList<ILoggerService>), -1, ServiceTargetData.FoundationServiceTargetData(), optional: true) as IReadOnlyList<ILoggerService>;
      }
      catch (Exception e)
      {
        Debug.LogError($"[FTFoundation] Failed to resolve ILoggerService while flushing startup diagnostics; falling back to Debug.Log: {e.Message}");
        loggers = null;
      }

      foreach (var problem in problems)
      {
        if (loggers == null || loggers.Count == 0)
        {
          switch (problem.ProblemDetailType)
          {
            case ProblemDetailType.INFORMATION: Debug.Log(problem.Message); break;
            case ProblemDetailType.WARNING: Debug.LogWarning(problem.Message); break;
            case ProblemDetailType.ERROR: Debug.LogError(problem.Message); break;
          }
          continue;
        }

        foreach (var logger in loggers)
        {
          switch (problem.ProblemDetailType)
          {
            case ProblemDetailType.INFORMATION:
              logger.Log(problem.Message);
              break;
            case ProblemDetailType.WARNING:
              logger.LogWarning(problem.Message);
              break;
            case ProblemDetailType.ERROR:
              logger.LogError(problem.Message);
              break;
          }
        }
      }
    }
  }
}