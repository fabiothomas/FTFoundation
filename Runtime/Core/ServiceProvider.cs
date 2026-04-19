using System;
using System.Collections.Generic;
using System.Reflection;
using FTFoundation.Core.Validation;
using UnityEngine;

namespace FTFoundation.Core
{
  public static class ServiceProvider
  {
    // Single-winner service resolution: interface → winning concrete type
    internal static readonly Dictionary<Type, Type> serviceCache = new();

    // All profile-matched concrete types per interface, ordered by priority (for IReadOnlyList<T> injection)
    internal static readonly Dictionary<Type, List<Type>> multiServiceCache = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    private static void InitializeServiceProvider()
    {
      // resetting static values in case of 'domain reloading' being disabled
      serviceCache.Clear();
      multiServiceCache.Clear();
      ServiceResolver.Clear();
      ServiceCompiler.Clear();

      BuildTargetProfile currentProfile = BuildProfileDetector.Current;
      BuildTargetPlatform currentPlatform = BuildPlatformDetector.Current;

      // ── Resolve service candidates ────────────────────────────────────────────────────────
      var resolved = ServiceCandidateResolver.Resolve(currentProfile, currentPlatform);

      foreach (var warning in resolved.Warnings)
        SendWarning(warning);

      // ── Populate lookup caches ────────────────────────────────────────────────────────────
      foreach (var (iface, winnerType) in resolved.Winners)
        serviceCache[iface] = winnerType;

      foreach (var (iface, matchedTypes) in resolved.AllMatched)
        multiServiceCache[iface] = matchedTypes;

      // ── Pre-compile factories and injection actions ───────────────────────────────────────
      foreach (var (_, winnerType) in resolved.Winners)
      {
        ServiceCompiler.PrecompileFactory(winnerType);
        ServiceCompiler.PrecompileInjectionAction(winnerType);
      }

      foreach (var (_, matchedTypes) in resolved.AllMatched)
      {
        foreach (var type in matchedTypes)
        {
          ServiceCompiler.PrecompileFactory(type);
          ServiceCompiler.PrecompileInjectionAction(type);
        }
      }

      // ── Pre-compile injection actions for MonoBehaviours in injection-target assemblies ───
      foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
      {
        if (assembly.GetCustomAttribute<InjectionTargetAssemblyAttribute>() == null) continue;

        foreach (var t in assembly.GetTypes())
        {
          if (!t.IsSubclassOf(typeof(MonoBehaviour))) continue;
          ServiceCompiler.PrecompileInjectionAction(t);
        }
      }

      // ── Eagerly instantiate startup singletons ────────────────────────────────────────────
      foreach (var t in resolved.EagerStartups)
      {
        ServiceAttribute attribute = (ServiceAttribute)t.GetCustomAttribute(typeof(ServiceAttribute), inherit: true);
        if (attribute == null) continue;

        var obj = Activator.CreateInstance(t);

        ServiceTargetData target = new(t.Name, ServiceTargetDataType.SINGLETON, t, obj);

        InjectDependencies(obj, -1, target);

        ServiceResolver.RegisterStartupSingleton(attribute.Interface, obj);
      }

      ServiceStackValidator.Validate(serviceCache, currentProfile);
    }

    /// <summary>
    /// <para> This method can be called to inject services into a MonoBehaviour </para> 
    /// <para> This method should probably be used on the MonoBehaviour itself in the 'Awake()' method passing 'this' as it's parameter </para>
    /// </summary>
    /// <param name="instance">Target MonoBehaviour instance</param>
    public static void Inject(MonoBehaviour instance)
    {
      ServiceTargetData target = new(instance.name, ServiceTargetDataType.MONOBEHAVIOUR, instance.GetType(), instance);
      InjectDependencies(instance, instance.gameObject.scene.buildIndex, target);
    }

    internal static void InjectDependencies(object obj, int sceneIndex, ServiceTargetData target)
    {
      if (obj == null) throw new ArgumentNullException(nameof(obj));

      Type type = obj.GetType();

      if (ServiceCompiler.TryGetInjectionAction(type, out var injectionAction))
      {
        injectionAction(obj, sceneIndex, target);
      }
    }

    [HideInCallstack]
    private static void SendWarning(string message)
    {
#if UNITY_EDITOR
      Debug.LogWarning($"<color=#f22800><b>[ServiceProvider]</b></color> <color=#cc9b05ff>{message}</color>");
#endif
    }
  }
}