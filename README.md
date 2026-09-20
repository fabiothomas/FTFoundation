# FTFoundation

A lightweight, attribute-driven dependency injection framework for Unity that aims to split upp the code in seperate de-coupled modules defined with assembly defenitions.

FTFoundation lets you wire up services across assembly boundaries without any manual registration code. Injection actions and service factories are pre-compiled once at startup using `System.Linq.Expressions`, so there is no per-frame or per-injection reflection overhead.

---

## Table of Contents

- [Getting Started](#getting-started)
- [Defining Services](#defining-services)
  - [Service Lifetimes](#service-lifetimes)
  - [Assembly Registration](#assembly-registration)
  - [Active Service Overview](#active-service-overview)
- [Injecting Dependencies](#injecting-dependencies)
  - [Property Injection](#property-injection)
  - [Method Injection](#method-injection)
  - [Multi-Service Injection](#multi-service-injection)
  - [Injecting into MonoBehaviours](#injecting-into-monobehaviours)
- [Service Selection](#service-selection)
  - [Build Profile Filtering](#build-profile-filtering)
  - [Platform Filtering](#platform-filtering)
  - [Excluding From the Build Entirely](#excluding-from-the-build-entirely)
  - [Priority](#priority)
  - [Fallback Services](#fallback-services)
  - [Eager Instantiation](#eager-instantiation)
- [Configuration](#configuration)
  - [Config Files](#config-files)
  - [The [Config] Attribute](#the-config-attribute)
- [Cleanup](#cleanup)
  - [IServiceCleanup](#iservicecleanup)
- [Managed Code Stripping](#managed-code-stripping)
- [Built-in Services](#built-in-services)

---

## Getting Started

1. Mark the assembly that contains your service implementations with `[assembly: ServiceAssembly]` in an `AssemblyInfo.cs` file.
2. Mark any assembly whose `MonoBehaviour`s need injection with `[assembly: InjectionTargetAssembly]`.
3. Call `ServiceProvider.Inject(this)` from `Awake()` in each `MonoBehaviour` that needs services.

That's it. The container bootstraps itself automatically before the splash screen via `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]`.

---

## Defining Services

Decorate a class with `[Service]` to register it with the container:

```csharp
[Service(typeof(IAnalyticsService), ServiceType.SINGLETON)]
public class AnalyticsService : IAnalyticsService
{
    // ...
}
```

The class requires a **public parameterless constructor**.

### Service Lifetimes

| Lifetime                | Behaviour                                                                                                   |
| ----------------------- | ----------------------------------------------------------------------------------------------------------- |
| `ServiceType.SINGLETON` | One instance for the entire application lifetime.                                                           |
| `ServiceType.SCOPED`    | One instance per scene. A new instance is created when the same interface is first resolved in a new scene. |
| `ServiceType.TRANSIENT` | A new instance is created for each injection.                                                               |

> **Note:** Scoped services cannot be injected into singleton services — the container will throw at runtime if attempted.

### Assembly Registration

Every assembly that contains `[Service]`-decorated types must declare itself as a service assembly:

```csharp
// AssemblyInfo.cs
using FTFoundation.Core;
[assembly: ServiceAssembly]
```

Every assembly whose `MonoBehaviour`s call `ServiceProvider.Inject(this)` must declare itself as an injection target. This allows the container to pre-compile injection actions for all `MonoBehaviour` types in that assembly at startup:

```csharp
// AssemblyInfo.cs
using FTFoundation.Core;
[assembly: InjectionTargetAssembly]
```

### Active Service Overview

FTFoundation provides an overview of all active services in your project:

`Window -> FTFoundation`

In the header of this view you can select a profile and platform to get the accurate service context of a specific environment.

---

## Injecting Dependencies

### Property Injection

Mark a **private** property with `[Inject]`:

```csharp
[Inject] private ILoggerService Logger { get; set; }
```

No `= null!;` needed — the bundled analyzer (see [IDE warnings](#method-injection)) suppresses `CS8618` for `[Inject]`/`[Config]` properties, since the framework always populates them via reflection before any other code runs.

Use `Optional = true` for dependencies that may not be registered. The property will be `null` if the service is absent:

```csharp
[Inject(Optional = true)] private IAnalyticsService? Analytics { get; set; }
```

### Method Injection

Declare a **private** method named exactly `Inject`. Its parameters are resolved as services:

```csharp
void Inject(ILoggerService logger, IEventService events)
{
    _logger = logger;
    _events = events;
}
```

A parameter is optional if it declares a default value:

```csharp
void Inject(ILoggerService logger, IAnalyticsService? analytics = null)
{
    _logger = logger;
    _analytics = analytics; // null if IAnalyticsService isn't registered
}
```

As with `[Inject(Optional = true)]`, the parameter must be able to hold a null reference. Parameters without a default value are required, same as before. Both property injection and method injection can be used simultaneously on the same class — and when they are, `[Config]`/`[Inject]` properties are always fully populated *before* `Inject()` runs, so it's safe to use a property-injected service from inside `Inject()`.

> **IDE warnings:** since `Inject()` is only ever called via reflection, and `[Inject]`/`[Config]` properties are only ever assigned via reflection, the compiler can't see either — IDE "unused private member" inspections flag `Inject()` methods as dead code, and `CS8618` flags `[Inject]`/`[Config]` properties (and fields `Inject()` assigns) as uninitialized, which is why older code in this repo used to carry `= null!;` on every one of them. A bundled Roslyn analyzer (`Runtime/Analyzers/FTFoundation.Analyzers.dll`) suppresses both, and also catches seven real mistakes at compile time that otherwise only surface as a runtime exception once the container resolves the service — duplicate `Inject` methods, missing property setters, `[Service]`/interface mismatches, missing constructors, direct references to `[Service]` types, and `#if` build-gate drift (see [Excluding From the Build Entirely](#excluding-from-the-build-entirely)). **Full diagnostic reference, rebuild instructions, and IDE support notes: [`docs/analyzers.md`](docs/analyzers.md).**

### Multi-Service Injection

Inject all active implementations of an interface by requesting `IReadOnlyList<T>`, `IEnumerable<T>`, or `List<T>`:

```csharp
void Inject(IReadOnlyList<ILoggerService> loggers)
{
    _loggers = loggers;
}
```

Instances are ordered by priority (highest first).

### Injecting into MonoBehaviours

Call `ServiceProvider.Inject(this)` in `Awake()`:

```csharp
public class PlayerController : MonoBehaviour
{
    [Inject] private IInputService Input { get; set; }

    void Awake() => ServiceProvider.Inject(this);
}
```

---

## Service Selection

When multiple implementations of the same interface exist, the container applies the following rules in order to select the active one(s).

### Build Profile Filtering

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

### Platform Filtering

Restrict a service to specific runtime platforms using `[ServiceBuildPlatform]`:

```csharp
[ServiceBuildPlatform(BuildTargetPlatform.Desktop)]
[Service(typeof(ILoggerSink), ServiceType.TRANSIENT)]
public class ConsoleLoggerService : ILoggerSink { ... }
```

Group flags (`Desktop`, `Mobile`, `Console`, `Web`) and specific flags (`Windows`, `macOS`, `Android`, `iOS`, etc.) are both supported.

### Excluding From the Build Entirely

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

Two analyzer diagnostics keep this in sync automatically: `FTF0006` flags a restricted service that isn't wrapped in any `#if` yet, and `FTF0007` flags an existing `#if` that's drifted out of sync after the attribute changed (e.g. a platform was added but the `#if` wasn't updated) - both include the exact expression to use, and both have a one-click fix. **Full symbol mapping, the `Production` simplification, and an Editor-specific caveat: [`docs/analyzers.md`](docs/analyzers.md#build-gate-symbol-reference).**

### Priority

When multiple implementations pass the profile and platform filters, `[ServicePriority]` determines which one wins for single-service injection. Higher values win. The default priority is `0`.

```csharp
[ServicePriority(10)]
[Service(typeof(IAnalyticsService), ServiceType.SINGLETON)]
public class FirebaseAnalyticsService : IAnalyticsService { ... }
```

All matching implementations are always included when injecting `IReadOnlyList<T>`.

### Fallback Services

`[ServiceFallback]` marks a service as a last-resort implementation. It is only registered when no non-fallback candidate passes the current build profile and platform filters. Use this to implement the null-object pattern:

```csharp
[ServiceFallback]
[Service(typeof(IAnalyticsService), ServiceType.SINGLETON)]
public class NullAnalyticsService : IAnalyticsService
{
    public void TrackEvent(string name) { } // no-op when no real analytics provider is configured
}
```

### Eager Instantiation

By default, singletons are created lazily on first use. Add `[InstantiateOnStartup]` to construct a singleton immediately during the bootstrap phase:

```csharp
[InstantiateOnStartup]
[Service(typeof(IDebugScreenService), ServiceType.SINGLETON)]
public class DebugScreenService : IDebugScreenService { ... }
```

---

## Configuration

FTFoundation supports a layered JSON settings system. Values are loaded once at startup and injected into services before any other dependencies are resolved.

### Config Files

Place JSON files in any `Resources/` folder. Files are merged in the following order — each layer overrides the previous:

| Priority    | File                         | Purpose                                                                  |
| ----------- | ---------------------------- | ------------------------------------------------------------------------ |
| 1 (lowest)  | `appsettings.builtin.json`   | Package-level defaults — provided by FTFoundation for built-in services. |
| 2           | `appsettings.json`           | Your project's main configuration.                                       |
| 3           | `appsettings.{profile}.json` | Profile-specific overrides, e.g. `appsettings.editor.json`.              |
| 4 (highest) | `appsettings.local.json`     | Machine-local overrides. **Add to `.gitignore`.**                        |

> **Security:** JSON files packaged with a build can be read by anyone who extracts it. Keep secrets (API keys, tokens) in `appsettings.local.json` only and never commit them to source control.

The JSON structure maps to services using their class name with the `Service` suffix stripped and the first character lowercased:

```
ConsoleLoggerService  →  "consoleLogger"
MyNetworkService      →  "myNetwork"
```

```json
{
  "myNetwork": {
    "apiEndpoint": "https://api.example.com",
    "timeout": "30"
  }
}
```

### The [Config] Attribute

Mark a **private** property with `[Config]` to have it populated from the merged config before `[Inject]` properties and the `Inject()` method are processed:

```csharp
[Config] private string ApiEndpoint { get; set; }
[Config] private int Timeout { get; set; }
```

Use `Required = true` to cause a startup error if the value is absent:

```csharp
[Config(Required = true)] private string ApiKey { get; set; } = null!;
```

Values are converted from their JSON string representation to the property's type via `Convert.ChangeType`.

---

## Cleanup

### IServiceCleanup

Implement `IServiceCleanup` on a service to receive a cleanup callback when the service is no longer needed:

```csharp
[Service(typeof(IMyService), ServiceType.SCOPED)]
public class MyService : IMyService, IServiceCleanup
{
    public void OnCleanup()
    {
        // Release resources, unsubscribe events, etc.
    }
}
```

The container calls `OnCleanup()` automatically:

| Lifetime                                        | When `OnCleanup` is called                                                                     |
| ------------------------------------------------ | ----------------------------------------------------------------------------------------------- |
| `SCOPED`                                          | When the scene the service was created in is unloaded.                                          |
| `TRANSIENT` (injected into `MonoBehaviour`)       | When the `MonoBehaviour`'s `GameObject` is destroyed.                                           |
| `TRANSIENT` (owned by a `SCOPED` or `SINGLETON` service) | When the owning scoped or singleton service is cleaned up.                               |
| `SINGLETON`                                       | Not on normal scene transitions or app quit — only if the container itself re-initializes with this singleton still cached, which in practice means "Reload Domain" disabled in the Editor and a new Play session starting. |

A `SINGLETON`'s owned `TRANSIENT` dependencies are cleaned up the same way a `SCOPED` service's are — you don't need to manually forward cleanup to them. For example, `DebugScreenService` and `LifetimeService` are both singletons that depend on `IDedicatedObjectService` (which implements `IServiceCleanup`); neither of them implements `IServiceCleanup` themselves, because the container already tears down their `IDedicatedObjectService` — and therefore the `GameObject` it owns — automatically.

---

## Managed Code Stripping

FTFoundation discovers and constructs every service entirely through reflection: assembly scanning for `[Service]`, then `Expression`-compiled factories and property/field setters for injection. None of that is visible to IL2CPP's `UnityLinker` static reachability analysis, so under **Managed Stripping Level** `Medium` or `High` (IL2CPP's own default, `Minimal`, is comparatively safe), a service that is never directly `new`'d or referenced from a serialized scene/prefab can be stripped from the build even though the container uses it at runtime.

Two mitigations are in place for this:

- **`link.xml`** at the package root preserves the `FTFoundation` and `BuildInServices` assemblies wholesale (`preserve="all"`). This fully protects every built-in service, regardless of member-level nuance, with nothing for you to do.
- **`[Service]`, `[Inject]`, and `[Config]` all derive from `UnityEngine.Scripting.PreserveAttribute`.** Since applying one of these is already required to participate in the framework, any class or property you decorate is automatically exempted from stripping too - no extra attribute or link.xml entry needed for your own services.

One residual gap: `[Preserve]` on a class guarantees the type and its default constructor survive, but doesn't blanket-protect arbitrary members. A plain method-injection `Inject(...)` method carries no attribute of its own (it's matched by name, not by decoration), so on a class in your own assembly it's theoretically still at risk under `High` stripping. Built-in services are unaffected (the `link.xml` entry covers them completely). If you rely heavily on method injection in your own assembly and build with `High` stripping, add your own `link.xml` entry for that assembly as a safety net:

```xml
<linker>
  <assembly fullname="YourAssemblyName" preserve="all" />
</linker>
```

This is best verified directly: make a Development Build with IL2CPP and Managed Stripping Level set to `High`, and confirm your services still resolve.

---

## Built-in Services

FTFoundation ships with a set of ready-to-use services behind stable interfaces.

| Interface                 | Description                                                                                                                                                                                                                               |
| ------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `ILoggerService`          | Facade that fans a log call out to every currently-active `ILoggerSink` (console, screen overlay, file, or your own). Implement `ILoggerSink` to add a new logging destination — no changes to `ILoggerService` consumers needed.        |
| `IEventService`           | Typed pub/sub event bus. Events are plain types implementing `IEvent`; subscribers and publishers are matched by event type, so new events need no central registry.                                                                     |
| `ILifetimeService`        | Subscribe to Unity's `Update`, `FixedUpdate`, and `LateUpdate` loops from plain C# classes. Returns an `IDisposable` to unsubscribe.                                                                                                      |
| `IReferenceService`       | Global registry holding at most one active `MonoBehaviour` instance per type, reachable from anywhere in the application.                                                                                                                 |
| `IDedicatedObjectService` | Creates and manages a dedicated `GameObject` (with optional Canvas hierarchy helpers) scoped to the requesting service. Implements `IServiceCleanup` — the `GameObject` is destroyed automatically when the owning service is cleaned up. |
| `IDebugScreenService`     | In-editor/development overlay for log output, debug buttons (with optional keyboard hotkeys), and value watchers.                                                                                                                         |
