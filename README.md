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

> **IDE warnings:** since `Inject()` is only ever called via reflection, and `[Inject]`/`[Config]` properties are only ever assigned via reflection, the compiler can't see either — IDE "unused private member" inspections (e.g. Roslyn's `IDE0051`) flag `Inject()` methods as dead code, and `CS8618` ("non-nullable property must contain a non-null value") flags `[Inject]`/`[Config]` properties as uninitialized, which is why older code in this repo used to carry `= null!;` on every one of them. The same `CS8618` warning shows up on a plain field that's only ever assigned from inside `Inject()` — the compiler only tracks assignments reachable from the constructor, and `Inject()` isn't one. The package ships a Roslyn analyzer (`Runtime/Analyzers/FTFoundation.Analyzers.dll`) that suppresses all of these — `IDE0051` for any non-public, non-static method named exactly `Inject`, `CS8618` for any property carrying `[Inject]` or `[Config]`, and `CS8618` for any field the class's own `Inject()` method unconditionally assigns (checked by actually reading that method's body, not by guessing from the field/parameter names) — so neither the boilerplate default value nor a suppressed warning is needed. Its source lives in `Runtime/Analyzers~/` (a `~`-suffixed folder, invisible to Unity) — rebuild it with `dotnet build -c Release Runtime/Analyzers~/FTFoundation.Analyzers.csproj` and copy the output from `bin/Release/netstandard2.0/FTFoundation.Analyzers.dll` over the one in `Runtime/Analyzers/`.
>
> **Placement matters for scope:** Unity applies a `RoslynAnalyzer`-labeled DLL only to the assembly whose folder (the one containing its `.asmdef`) it lives under, plus any assembly that references that one. The DLL lives under `Runtime/` (`FTFoundation.asmdef`'s folder) specifically so its scope covers every assembly that references `FTFoundation` — which, by construction, is every assembly that could ever declare an `Inject()` method or an `[Inject]`/`[Config]` property. Don't move it under `Editor/`; that would scope it to `FTFoundation.Editor` alone, which nothing else references.
>
> **It also catches seven real mistakes at compile time**, all of which otherwise compile fine and only fail once the container actually resolves the service:
> - `FTF0001` — a class declares more than one non-public instance method named `Inject`. `Type.GetMethod("Inject", ...)` matches by name alone, so this throws `AmbiguousMatchException` at startup regardless of the methods having different parameter lists.
> - `FTF0002` — a property carries `[Inject]` or `[Config]` but has no setter. `PropertyInfo.SetValue` needs one; a get-only property throws at injection time.
> - `FTF0003` — a class is registered with `[Service(typeof(TInterface), ...)]` but doesn't actually implement `TInterface` (directly or via a base class).
> - `FTF0004` — a `[Service]`-decorated class is abstract, or has no accessible public parameterless constructor. `ServiceCompiler.PrecompileFactory` builds every instance with `Expression.New(type)`, which requires exactly that.
> - `FTF0005` — a `[Service]`-decorated concrete type is referenced directly (`new`, `typeof`, a cast, `is`/`as`, a field/parameter/local typed as the concrete class, a base list, a generic type argument) instead of through its interface. FTFoundation only supports interface-based injection, so this is always a design mistake, not just a style nit — and it's also what makes it safe to assume a `[Service]` type is only ever reachable via the container's own reflection, which build-stripping tooling can lean on. `nameof(...)` is exempt, since it erases to a string constant with no runtime dependency on the type. A reference from within the service's own declaration - including a private nested helper type, e.g. a `MonoBehaviour` that needs to call back into members the interface doesn't expose (see `LifetimeService`/`LifetimeServiceHelper`) - is also exempt, since whatever removes the type for build-stripping removes everything declared inside it too. A type nested inside a `[Service]` class purely to hold data (not to help implement it) doesn't get this exemption once referenced from outside - that's a sign the data type belongs on its own, independent of the service.
> - `FTF0006` — a `[ServiceBuildProfile]`/`[ServiceBuildPlatform]`-restricted service isn't wrapped in any `#if`, so it still compiles into every build as dead code instead of being excluded from non-matching ones entirely. See [Excluding From the Build Entirely](#excluding-from-the-build-entirely) for the symbol mapping and the exact expression the warning suggests.
> - `FTF0007` — an existing `#if` no longer covers every build the attribute allows, most likely because the attribute changed (e.g. a platform was added) without updating the `#if` to match. This is the dangerous direction specifically: the service would silently never be selected in whichever builds the `#if` now wrongly excludes, with no error anywhere - just a service that's mysteriously missing. It brute-forces every combination of whatever symbols the two expressions actually reference (always small in practice) rather than attempting general boolean simplification, so it's exact, not heuristic - but it only checks that direction; a `#if` that's *broader* than the attribute requires is wasteful, not wrong (the container still filters it at runtime), so that's not flagged. Skipped entirely for an `#if` with `#elif`/`#else` branches, and a symbol outside FTFoundation's own set (e.g. your own extra flag deliberately ANDed in) is treated as a free variable, so an intentional extra restriction can still surface here - narrow the attribute to match rather than treating that as a bug in the check.
>
> All seven are reported as warnings, not errors, so they won't block your build.
>
> **Do not bump `Microsoft.CodeAnalysis.CSharp` past 3.8** in that `.csproj` — Unity's own bundled Roslyn only supports analyzers built against 3.8, and a newer version compiles fine but fails to load in Unity ("Unable to resolve reference 'Microsoft.CodeAnalysis'..."). After rebuilding, the DLL's import settings need: the `RoslynAnalyzer` label, "Any Platform" and "Editor" both unchecked (it's compile-time tooling, not a runtime dependency), and "Validate References" unchecked (Unity's static plugin-reference checker can't see the Roslyn assemblies the compiler host loads internally, so it will otherwise report the same false error even though the version is correct).
>
> **`FTF0006` and `FTF0007` both have a one-click fix**: `Runtime/Analyzers/FTFoundation.Analyzers.CodeFixes.dll`, built from `Runtime/Analyzers~/CodeFixes/FTFoundation.Analyzers.CodeFixes.csproj`, inserts the `#if`/`#endif` for `FTF0006` and replaces the stale condition in place for `FTF0007` - both computed the same way the diagnostic message already shows. It's a separate DLL/project from the main analyzer on purpose: a `CodeFixProvider` needs `Microsoft.CodeAnalysis.CSharp.Workspaces`, which only an IDE loads, never the compiler - bundling that dependency into the same assembly the compiler itself loads for every compile risks exactly the kind of "unable to resolve reference" failure called out above, just for a different assembly. Both DLLs carry the `RoslynAnalyzer` label and the same import settings. Whether the fix actually shows up as a lightbulb depends on your editor: it should work in **Visual Studio** and **Rider** (Unity's own officially supported IDEs for this feature). In **VS Code**, it depends on which C# backend is active - the legacy OmniSharp-based "C#" extension needs `"omnisharp.enableRoslynAnalyzers": true` set explicitly (off by default), and the newer **C# Dev Kit** has had reported gaps with third-party analyzer/code-fix visibility in general, unrelated to Unity specifically. Both diagnostics always work everywhere regardless - the code fixes are a convenience on top, not something to depend on.

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

Symbol mapping (`BuildProfileDetector`/`BuildPlatformDetector` already use exactly these):

| `BuildTargetProfile` | `#if` symbol |
| --- | --- |
| `Editor` | `UNITY_EDITOR` |
| `Development` | `DEVELOPMENT_BUILD` |
| `Staging` | `STAGING_BUILD` |
| `Production` | has no define of its own - it's "none of the others" (see below) |

| `BuildTargetPlatform` | `#if` symbol |
| --- | --- |
| `Desktop` | `UNITY_STANDALONE` |
| `Mobile` | `UNITY_ANDROID \|\| UNITY_IOS` |
| `Console` | `UNITY_PS4 \|\| UNITY_PS5 \|\| UNITY_GAMECORE \|\| UNITY_XBOXONE \|\| UNITY_SWITCH` |
| `Web` | `UNITY_WEBGL` |
| `Windows` / `macOS` / `Linux` | `UNITY_STANDALONE_WIN` / `_OSX` / `_LINUX` |
| `Android` / `iOS` / `Switch` | `UNITY_ANDROID` / `UNITY_IOS` / `UNITY_SWITCH` |
| `PlayStation` / `Xbox` | `UNITY_PS4 \|\| UNITY_PS5` / `UNITY_GAMECORE \|\| UNITY_XBOXONE` |

> Console/Xbox/PlayStation symbol names have shifted across Unity versions (GameCore vs. the older per-console defines) - double check these against the Unity manual for the version you're on before relying on them.

`BuildProfileDetector` only ever defines one of `Editor`/`Development`/`Staging`/`Production` at a time, so combining `Production` with some subset of the other three is always exactly equivalent to negating whichever ones are *missing* - which is usually far simpler than spelling `Production` out as its own triple negation and OR-ing it in. Both the `FTF0006` message and its code fix compute this automatically:

| Attribute | Generated `#if` |
| --- | --- |
| `Production \| Staging \| Editor` (i.e. everything except `Development`) | `!DEVELOPMENT_BUILD` |
| `Production` alone | `!UNITY_EDITOR && !DEVELOPMENT_BUILD && !STAGING_BUILD` |
| `Production \| Editor` (missing `Development` and `Staging`) | `!DEVELOPMENT_BUILD && !STAGING_BUILD` |

Combined profile+platform expressions are also only parenthesized when actually needed (i.e. one side is a multi-term `\|\|`) - `FileLoggerService`'s real attributes (`Production \| Staging \| Editor` + `Desktop`) generate `#if !DEVELOPMENT_BUILD && UNITY_STANDALONE`, not a parenthesized, un-simplified mess.

> **Editor caveat:** this is faithful for real player builds, but can diverge from the runtime check specifically *in the Editor*. `BuildPlatformDetector` reports `Desktop` in the Editor based on the host OS running Unity - always true on a PC/Mac/Linux machine. The `UNITY_STANDALONE` compile symbol instead tracks whichever Build Target is currently selected in Build Settings, even while just pressing Play. A dev with Build Target set to Android while working in the Editor would silently exclude a `Desktop`-gated service under `#if`, even though the runtime-only check would still include it today. This doesn't affect real platform builds, only that Editor edge case.

The analyzer flags a restricted service that isn't wrapped in any `#if` at all (`FTF0006`), and includes the exact expression to use in the warning message - it can't verify a hand-written `#if` actually matches the attribute, only that one is present.

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
