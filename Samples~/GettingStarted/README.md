# Getting Started

A minimal, end-to-end walkthrough of FTFoundation's core mechanics:

- **`IGreetingService` / `GreetingService.cs`** — defining and registering your own service with `[Service]`, and injecting a built-in service (`ILoggerService`) into it via `[Inject]`.
- **`GreetingBehaviour.cs`** — injecting into a `MonoBehaviour`, using both property injection (`[Inject]`) and method injection (`void Inject(...)`) side by side.
- **`AssemblyInfo.cs`** — the two assembly-level attributes (`[assembly: ServiceAssembly]`, `[assembly: InjectionTargetAssembly]`) every assembly needs to participate: one to register services, one to allow `MonoBehaviour`s to call `ServiceProvider.Inject(this)`.

## Try it

1. Add `GreetingBehaviour` to any `GameObject` in a scene.
2. Press Play.
3. Check the Console — you should see the greeting logged twice: once from inside `GreetingService` itself, once from `GreetingBehaviour`'s own `Inject()` method.

See the main [FTFoundation README](https://github.com/fabiothomas/FTFoundation/blob/main/README.md) for everything else — lifetimes, build-profile/platform filtering, config files, and the full list of built-in services.
