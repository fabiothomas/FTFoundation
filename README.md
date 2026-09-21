# FTFoundation

A lightweight, attribute-driven dependency injection framework for Unity that aims to split upp the code in seperate de-coupled modules defined with assembly defenitions.

FTFoundation lets you wire up services across assembly boundaries without any manual registration code. Injection actions and service factories are pre-compiled once at startup using `System.Linq.Expressions`, so there is no per-frame or per-injection reflection overhead.

---

## Getting Started

1. Mark the assembly that contains your service implementations with `[assembly: ServiceAssembly]` in an `AssemblyInfo.cs` file.
2. Mark any assembly whose `MonoBehaviour`s need injection with `[assembly: InjectionTargetAssembly]`.
3. Call `ServiceProvider.Inject(this)` from `Awake()` in each `MonoBehaviour` that needs services.

That's it. The container bootstraps itself automatically before the splash screen via `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]`.

---

## Documentation

| Page | Covers |
| --- | --- |
| [Defining Services](docs/defining-services.md) | `[Service]`, lifetimes, assembly registration, the Active Service Overview window |
| [Injecting Dependencies](docs/injecting-dependencies.md) | Property/method/multi-service injection, `MonoBehaviour`s, IDE warnings |
| [Service Selection](docs/service-selection.md) | Build profile/platform filtering, excluding a service from a build entirely, priority, fallbacks |
| [Configuration](docs/configuration.md) | Layered `appsettings*.json` files and the `[Config]` attribute |
| [Cleanup](docs/cleanup.md) | `IServiceCleanup` and when the container calls it |
| [Managed Code Stripping](docs/managed-code-stripping.md) | IL2CPP stripping risk and the mitigations already in place |
| [Built-in Services](docs/built-in-services.md) | The services FTFoundation ships out of the box |
| [Analyzer Reference](docs/analyzers.md) | Every diagnostic, the code fixes, and the build-gate symbol tables |

See [`docs/README.md`](docs/README.md) for the same list with a longer description of each page.
