[← Back to docs index](README.md)

# Managed Code Stripping

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
