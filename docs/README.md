# FTFoundation Documentation

New here? Start with the [package README](../README.md) for installation and a 3-step getting-started guide.

## Guides

| Page | Covers |
| --- | --- |
| [Defining Services](defining-services.md) | `[Service]`, lifetimes (`SINGLETON`/`SCOPED`/`TRANSIENT`), assembly registration, the Active Service Overview window |
| [Injecting Dependencies](injecting-dependencies.md) | Property injection, method injection, multi-service injection, injecting into `MonoBehaviour`s, and the IDE warnings that come with reflection-based injection |
| [Service Selection](service-selection.md) | `[ServiceBuildProfile]`/`[ServiceBuildPlatform]` filtering, excluding a service from a build entirely via `#if`, `[ServicePriority]`, `[ServiceFallback]`, `[InstantiateOnStartup]` |
| [Configuration](configuration.md) | Layered `appsettings*.json` files and the `[Config]` attribute |
| [Cleanup](cleanup.md) | `IServiceCleanup` and when the container calls it per lifetime |
| [Managed Code Stripping](managed-code-stripping.md) | Why IL2CPP stripping is a real risk for a reflection-based container, and the two mitigations already in place |
| [Built-in Services](built-in-services.md) | The services FTFoundation ships out of the box |
| [Analyzer Reference](analyzers.md) | Every `FTF00xx`/`FTFSUPP00x` diagnostic, the code fixes, the build-gate symbol tables, and how to rebuild the analyzer |
