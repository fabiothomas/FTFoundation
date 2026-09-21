[← Back to docs index](README.md)

# Defining Services

Decorate a class with `[Service]` to register it with the container:

```csharp
[Service(typeof(IAnalyticsService), ServiceType.SINGLETON)]
public class AnalyticsService : IAnalyticsService
{
    // ...
}
```

The class requires a **public parameterless constructor**.

## Service Lifetimes

| Lifetime                | Behaviour                                                                                                   |
| ----------------------- | ----------------------------------------------------------------------------------------------------------- |
| `ServiceType.SINGLETON` | One instance for the entire application lifetime.                                                           |
| `ServiceType.SCOPED`    | One instance per scene. A new instance is created when the same interface is first resolved in a new scene. |
| `ServiceType.TRANSIENT` | A new instance is created for each injection.                                                               |

> **Note:** Scoped services cannot be injected into singleton services — the container will throw at runtime if attempted.

## Assembly Registration

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

## Active Service Overview

FTFoundation provides an overview of all active services in your project:

`Window -> FTFoundation`

In the header of this view you can select a profile and platform to get the accurate service context of a specific environment.
