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
[Inject] private ILoggerService Logger { get; set; } = null!;
```

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

Method parameters are never optional. Both property injection and method injection can be used simultaneously on the same class.

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
    [Inject] private IInputService Input { get; set; } = null!;

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
[Service(typeof(ILoggerService), ServiceType.TRANSIENT)]
public class ScreenLoggerService : ILoggerService { ... }

[ServiceBuildProfile(BuildTargetProfile.Production | BuildTargetProfile.Staging)]
[Service(typeof(ILoggerService), ServiceType.TRANSIENT)]
public class FileLoggerService : ILoggerService { ... }
```

Services without `[ServiceBuildProfile]` are active in all profiles.

Available profiles: `Editor`, `Development`, `Staging`, `Production`, `All`.

### Platform Filtering

Restrict a service to specific runtime platforms using `[ServiceBuildPlatform]`:

```csharp
[ServiceBuildPlatform(BuildTargetPlatform.Desktop)]
[Service(typeof(ILoggerService), ServiceType.TRANSIENT)]
public class ConsoleLoggerService : ILoggerService { ... }
```

Group flags (`Desktop`, `Mobile`, `Console`, `Web`) and specific flags (`Windows`, `macOS`, `Android`, `iOS`, etc.) are both supported.

### Priority

When multiple implementations pass the profile and platform filters, `[ServicePriority]` determines which one wins for single-service injection. Higher values win. The default priority is `0`.

```csharp
[ServicePriority(10)]
[Service(typeof(ILoggerService), ServiceType.TRANSIENT)]
public class ConsoleLoggerService : ILoggerService { ... }
```

All matching implementations are always included when injecting `IReadOnlyList<T>`.

### Fallback Services

`[ServiceFallback]` marks a service as a last-resort implementation. It is only registered when no non-fallback candidate passes the current build profile and platform filters. Use this to implement the null-object pattern:

```csharp
[ServiceFallback]
[Service(typeof(ILoggerService), ServiceType.SINGLETON)]
public class NullLoggerService : ILoggerService
{
    public bool Disabled { get; set; }
    public void Log(string message) { }
    public void LogWarning(string message) { }
    public void LogError(string message) { }
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
[Config] private string ApiEndpoint { get; set; } = null!;
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
| `ILoggerService`          | Structured logging with build-profile-aware implementations (console, screen overlay, file).                                                                                                                                              |
| `IEventService`           | Typed pub/sub event bus backed by `UnityEvent`.                                                                                                                                                                                           |
| `ILifetimeService`        | Subscribe to Unity's `Update`, `FixedUpdate`, and `LateUpdate` loops from plain C# classes. Returns an `IDisposable` to unsubscribe.                                                                                                      |
| `IReferenceService`       | Scene-scoped registry for `MonoBehaviour` references.                                                                                                                                                                                     |
| `IDedicatedObjectService` | Creates and manages a dedicated `GameObject` (with optional Canvas hierarchy helpers) scoped to the requesting service. Implements `IServiceCleanup` — the `GameObject` is destroyed automatically when the owning service is cleaned up. |
| `IDebugScreenService`     | In-editor/development overlay for log output, debug buttons (with optional keyboard hotkeys), and value watchers.                                                                                                                         |
