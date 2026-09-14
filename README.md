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
  - [Priority](#priority)
  - [Fallback Services](#fallback-services)
  - [Eager Instantiation](#eager-instantiation)
- [Configuration](#configuration)
  - [Config Files](#config-files)
  - [The [Config] Attribute](#the-config-attribute)
- [Cleanup](#cleanup)
  - [IServiceCleanup](#iservicecleanup)
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

As with `[Inject(Optional = true)]`, the parameter must be able to hold a null reference. Parameters without a default value are required, same as before. Both property injection and method injection can be used simultaneously on the same class.

> **IDE warnings:** since `Inject()` is only ever called via reflection, and `[Inject]`/`[Config]` properties are only ever assigned via reflection, the compiler can't see either — IDE "unused private member" inspections (e.g. Roslyn's `IDE0051`) flag `Inject()` methods as dead code, and `CS8618` ("non-nullable property must contain a non-null value") flags `[Inject]`/`[Config]` properties as uninitialized, which is why older code in this repo used to carry `= null!;` on every one of them. The package ships a Roslyn analyzer (`Runtime/Analyzers/FTFoundation.Analyzers.dll`) that suppresses both false positives — `IDE0051` for any non-public, non-static method named exactly `Inject`, and `CS8618` for any property carrying `[Inject]` or `[Config]` — so neither the boilerplate default value nor a suppressed warning is needed. Its source lives in `Runtime/Analyzers~/` (a `~`-suffixed folder, invisible to Unity) — rebuild it with `dotnet build -c Release Runtime/Analyzers~/FTFoundation.Analyzers.csproj` and copy the output from `bin/Release/netstandard2.0/FTFoundation.Analyzers.dll` over the one in `Runtime/Analyzers/`.
>
> **Placement matters for scope:** Unity applies a `RoslynAnalyzer`-labeled DLL only to the assembly whose folder (the one containing its `.asmdef`) it lives under, plus any assembly that references that one. The DLL lives under `Runtime/` (`FTFoundation.asmdef`'s folder) specifically so its scope covers every assembly that references `FTFoundation` — which, by construction, is every assembly that could ever declare an `Inject()` method or an `[Inject]`/`[Config]` property. Don't move it under `Editor/`; that would scope it to `FTFoundation.Editor` alone, which nothing else references.
>
> **Do not bump `Microsoft.CodeAnalysis.CSharp` past 3.8** in that `.csproj` — Unity's own bundled Roslyn only supports analyzers built against 3.8, and a newer version compiles fine but fails to load in Unity ("Unable to resolve reference 'Microsoft.CodeAnalysis'..."). After rebuilding, the DLL's import settings need: the `RoslynAnalyzer` label, "Any Platform" and "Editor" both unchecked (it's compile-time tooling, not a runtime dependency), and "Validate References" unchecked (Unity's static plugin-reference checker can't see the Roslyn assemblies the compiler host loads internally, so it will otherwise report the same false error even though the version is correct).

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

| Lifetime                                    | When `OnCleanup` is called                             |
| ------------------------------------------- | ------------------------------------------------------ |
| `SCOPED`                                    | When the scene the service was created in is unloaded. |
| `TRANSIENT` (injected into `MonoBehaviour`) | When the `MonoBehaviour`'s `GameObject` is destroyed.  |
| `TRANSIENT` (owned by a `SCOPED` service)   | When the owning scoped service is cleaned up.          |
| `SINGLETON`                                 | Never — singletons are not cleaned up automatically.   |

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
