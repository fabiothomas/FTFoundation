[← Back to docs index](README.md)

# Configuration

FTFoundation supports a layered JSON settings system. Values are loaded once at startup and injected into services before any other dependencies are resolved.

## Config Files

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

## The [Config] Attribute

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
