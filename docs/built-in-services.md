[← Back to docs index](README.md)

# Built-in Services

FTFoundation ships with a set of ready-to-use services behind stable interfaces.

| Interface                 | Description                                                                                                                                                                                                                               |
| ------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `ILoggerService`          | Facade that fans a log call out to every currently-active `ILoggerSink` (console, screen overlay, file, or your own). Implement `ILoggerSink` to add a new logging destination — no changes to `ILoggerService` consumers needed.        |
| `IEventService`           | Typed pub/sub event bus. Events are plain types implementing `IEvent`; subscribers and publishers are matched by event type, so new events need no central registry.                                                                     |
| `ILifetimeService`        | Subscribe to Unity's `Update`, `FixedUpdate`, and `LateUpdate` loops from plain C# classes. Returns an `IDisposable` to unsubscribe.                                                                                                      |
| `IReferenceService`       | Global registry holding at most one active `MonoBehaviour` instance per type, reachable from anywhere in the application.                                                                                                                 |
| `IDedicatedObjectService` | Creates and manages a dedicated `GameObject` (with optional Canvas hierarchy helpers) scoped to the requesting service. Implements `IServiceCleanup` — the `GameObject` is destroyed automatically when the owning service is cleaned up. |
| `IDebugScreenService`     | In-editor/development overlay for log output, debug buttons (with optional keyboard hotkeys), and value watchers.                                                                                                                         |
