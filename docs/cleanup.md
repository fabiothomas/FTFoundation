[← Back to docs index](README.md)

# Cleanup

## IServiceCleanup

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
