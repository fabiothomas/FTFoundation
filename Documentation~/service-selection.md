[← Back to docs index](README.md)

# Service Selection

When multiple implementations of the same interface exist, the container applies the following rules in order to select the active one(s).

## Build Profile Filtering

Restrict a service to specific build profiles using `[ServiceBuildProfile]`:

```csharp
[ServiceBuildProfile(BuildTargetProfile.Editor | BuildTargetProfile.Development)]
[Service(typeof(ILoggerSink), ServiceType.TRANSIENT)]
public class ScreenLoggerService : ILoggerSink { ... }

[ServiceBuildProfile(BuildTargetProfile.Production | BuildTargetProfile.Staging)]
[Service(typeof(ILoggerSink), ServiceType.TRANSIENT)]
public class FileLoggerService : ILoggerSink { ... }
```

Services without `[ServiceBuildProfile]` are active in all profiles.

Available profiles: `Editor`, `Development`, `Staging`, `Production`, `All`.

## Platform Filtering

Restrict a service to specific runtime platforms using `[ServiceBuildPlatform]`:

```csharp
[ServiceBuildPlatform(BuildTargetPlatform.Desktop)]
[Service(typeof(ILoggerSink), ServiceType.TRANSIENT)]
public class ConsoleLoggerService : ILoggerSink { ... }
```

Group flags (`Desktop`, `Mobile`, `Console`, `Web`) and specific flags (`Windows`, `macOS`, `Android`, `iOS`, etc.) are both supported.

## Excluding From the Build Entirely

`[ServiceBuildProfile]`/`[ServiceBuildPlatform]` alone only filters candidates at container startup - an excluded service is still compiled into every build as dead code. To remove it from a non-matching build's compiled output entirely, wrap the class in an `#if` using the matching Unity scripting define(s):

```csharp
#if UNITY_EDITOR && UNITY_STANDALONE
[ServiceBuildProfile(BuildTargetProfile.Editor)]
[ServiceBuildPlatform(BuildTargetPlatform.Desktop)]
[Service(typeof(ILoggerSink), ServiceType.TRANSIENT)]
public class ConsoleLoggerService : ILoggerSink { ... }
#endif
```

This is safe by construction: `FTF0005` guarantees a `[Service]`-decorated type is never referenced except through its interface, so nothing can be left dangling by a class simply not existing in a given compile. The attribute stays regardless - it's still what the container uses to pick among whatever candidates *did* survive compilation for this build.

Two analyzer diagnostics keep this in sync automatically: `FTF0006` flags a restricted service that isn't wrapped in any `#if` yet, and `FTF0007` flags an existing `#if` that's drifted out of sync after the attribute changed (e.g. a platform was added but the `#if` wasn't updated) - both include the exact expression to use, and both have a one-click fix. **Full symbol mapping, the `Production` simplification, and an Editor-specific caveat: [`analyzers.md`](analyzers.md#build-gate-symbol-reference).**

## Priority

When multiple implementations pass the profile and platform filters, `[ServicePriority]` determines which one wins for single-service injection. Higher values win. The default priority is `0`.

```csharp
[ServicePriority(10)]
[Service(typeof(IAnalyticsService), ServiceType.SINGLETON)]
public class FirebaseAnalyticsService : IAnalyticsService { ... }
```

All matching implementations are always included when injecting `IReadOnlyList<T>`.

## Fallback Services

`[ServiceFallback]` marks a service as a last-resort implementation. It is only registered when no non-fallback candidate passes the current build profile and platform filters. Use this to implement the null-object pattern:

```csharp
[ServiceFallback]
[Service(typeof(IAnalyticsService), ServiceType.SINGLETON)]
public class NullAnalyticsService : IAnalyticsService
{
    public void TrackEvent(string name) { } // no-op when no real analytics provider is configured
}
```

## Eager Instantiation

By default, singletons are created lazily on first use. Add `[InstantiateOnStartup]` to construct a singleton immediately during the bootstrap phase:

```csharp
[InstantiateOnStartup]
[Service(typeof(IDebugScreenService), ServiceType.SINGLETON)]
public class DebugScreenService : IDebugScreenService { ... }
```
