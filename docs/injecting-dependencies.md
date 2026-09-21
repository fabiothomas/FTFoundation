[← Back to docs index](README.md)

# Injecting Dependencies

## Property Injection

Mark a **private** property with `[Inject]`:

```csharp
[Inject] private ILoggerService Logger { get; set; }
```

No `= null!;` needed — the bundled analyzer (see [IDE warnings](#ide-warnings)) suppresses `CS8618` for `[Inject]`/`[Config]` properties, since the framework always populates them via reflection before any other code runs.

Use `Optional = true` for dependencies that may not be registered. The property will be `null` if the service is absent:

```csharp
[Inject(Optional = true)] private IAnalyticsService? Analytics { get; set; }
```

## Method Injection

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

### IDE warnings

Since `Inject()` is only ever called via reflection, and `[Inject]`/`[Config]` properties are only ever assigned via reflection, the compiler can't see either — IDE "unused private member" inspections flag `Inject()` methods as dead code, and `CS8618` flags `[Inject]`/`[Config]` properties (and fields `Inject()` assigns) as uninitialized, which is why older code in this repo used to carry `= null!;` on every one of them. A bundled Roslyn analyzer (`Runtime/Analyzers/FTFoundation.Analyzers.dll`) suppresses both, and also catches seven real mistakes at compile time that otherwise only surface as a runtime exception once the container resolves the service — duplicate `Inject` methods, missing property setters, `[Service]`/interface mismatches, missing constructors, direct references to `[Service]` types, and `#if` build-gate drift (see [Service Selection](service-selection.md#excluding-from-the-build-entirely)).

**Full diagnostic reference, rebuild instructions, and IDE support notes: [`analyzers.md`](analyzers.md).**

## Multi-Service Injection

Inject all active implementations of an interface by requesting `IReadOnlyList<T>`, `IEnumerable<T>`, or `List<T>`:

```csharp
void Inject(IReadOnlyList<ILoggerService> loggers)
{
    _loggers = loggers;
}
```

Instances are ordered by priority (highest first).

## Injecting into MonoBehaviours

Call `ServiceProvider.Inject(this)` in `Awake()`:

```csharp
public class PlayerController : MonoBehaviour
{
    [Inject] private IInputService Input { get; set; }

    void Awake() => ServiceProvider.Inject(this);
}
```
