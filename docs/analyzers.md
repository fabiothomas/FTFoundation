# Analyzer Reference

FTFoundation resolves `Inject()` methods, `[Inject]`/`[Config]` properties, and `[Service]` types
entirely through reflection, so the compiler can't see any of it. That cuts both ways: the compiler
raises false-positive IDE warnings on code that's actually fine, and it can't catch real mistakes
that only surface as a runtime exception once the container actually resolves the service. This
package ships a Roslyn analyzer/suppressor pair (plus a companion code-fix assembly) to handle both.

- [Where it lives](#where-it-lives)
- [Suppressions](#suppressions)
- [Diagnostics](#diagnostics)
- [Code fixes](#code-fixes)
- [Build-gate symbol reference](#build-gate-symbol-reference)
- [Rebuilding](#rebuilding)

---

## Where it lives

Two DLLs ship under `Runtime/Analyzers/`, both carrying Unity's `RoslynAnalyzer` asset label:

| DLL | Contains | Loaded by |
| --- | --- | --- |
| `FTFoundation.Analyzers.dll` | `ReflectionInjectionSuppressor` (a `DiagnosticSuppressor`) and `FTFoundationConventionAnalyzer` (a `DiagnosticAnalyzer`) | The compiler itself, for every compile |
| `FTFoundation.Analyzers.CodeFixes.dll` | `FTFoundationBuildGateCodeFixProvider` (a `CodeFixProvider`) | An IDE only, never the compiler |

They're separate assemblies deliberately: a `CodeFixProvider` needs `Microsoft.CodeAnalysis.CSharp.Workspaces`, which no compiler process ever loads. Bundling that dependency into the assembly the compiler loads for every single compile risks an "unable to resolve reference" failure at compile time for something an IDE-only feature needs - keeping them apart means a code-fix-side problem can never affect the diagnostics you actually depend on.

**Placement matters for scope.** Unity applies a `RoslynAnalyzer`-labeled DLL only to the assembly whose folder (the one containing its `.asmdef`) it lives under, plus any assembly that references that one. Both DLLs live under `Runtime/` (`FTFoundation.asmdef`'s folder) specifically so their scope covers every assembly that references `FTFoundation` - which, by construction, is every assembly that could ever declare an `Inject()` method or a `[Service]`/`[Inject]`/`[Config]` attribute. Don't move either under `Editor/`; that would scope it to `FTFoundation.Editor` alone, which nothing else references.

**Editor support.** Diagnostics and suppressions work anywhere Unity's own Roslyn analyzer support works - **Visual Studio** and **Rider** (Unity's officially supported IDEs for this feature). Code fixes ("Quick Fix" lightbulbs) are less certain in **VS Code**: the legacy OmniSharp-based "C#" extension needs `"omnisharp.enableRoslynAnalyzers": true` set explicitly (off by default), and the newer **C# Dev Kit** has had reported gaps with third-party analyzer/code-fix visibility generally, unrelated to Unity specifically. The diagnostics themselves always work regardless of any of this - code fixes are a convenience on top, never something to depend on.

---

## Suppressions

Both fire only when the reflection-based path genuinely accounts for the value - never a blanket suppression for the diagnostic ID.

### FTFSUPP001

Suppresses **IDE0051** ("private member is unused") on a non-public, non-static, ordinary method named exactly `Inject`. `ServiceCompiler` invokes it via a compiled `Expression`, never from hand-written code, so it's never actually unused.

### FTFSUPP002

Suppresses **CS8618** ("non-nullable property/field must contain a non-null value when exiting constructor") in two cases:
- A property carrying `[Inject]` or `[Config]` - the attribute alone is proof it's populated via `PropertyInfo.SetValue` after construction.
- A plain field the class's own `Inject()` method unconditionally assigns - checked by actually reading the top-level statements of `Inject()`'s body for a simple assignment targeting that exact field (not by guessing from the field or parameter name, and deliberately not recursing into `if`/`for`/`try`/nested blocks, so a match really does mean "assigned unconditionally").

---

## Diagnostics

All seven are `DiagnosticSeverity.Warning`, not errors - none of them block your build.

### FTF0001

**Multiple `Inject` methods on one class.** `Type.GetMethod("Inject", ...)` matches by name alone, so declaring more than one non-public instance method named `Inject` throws an `AmbiguousMatchException` at startup, regardless of the methods having different parameter lists. A class may declare at most one.

### FTF0002

**`[Inject]`/`[Config]` property has no setter.** FTFoundation assigns it via `PropertyInfo.SetValue` after construction, which throws at injection time for a get-only property. Properties marked `[Inject]` or `[Config]` must have a setter.

### FTF0003

**`[Service]` interface argument doesn't match the implementing type.** A class registered with `[Service(typeof(TInterface), ...)]` must actually implement `TInterface` (directly or via a base class) - resolving one that doesn't fails an `InvalidCastException`-style failure at runtime.

### FTF0004

**`[Service]` type has no public parameterless constructor.** `ServiceCompiler.PrecompileFactory` builds every instance with `Expression.New(type)`, which requires a concrete type with an accessible public parameterless constructor. Fires whether the type is abstract or just missing the right constructor.

### FTF0005

**Direct reference to a `[Service]`-decorated type.** FTFoundation only supports interface-based injection, so referencing a `[Service]`-decorated concrete class directly (`new`, `typeof`, a cast, `is`/`as`, a field/parameter/local typed as the concrete class, a base list, a generic type argument) is always a design mistake, not just a style nit - and it's also what makes it safe to assume a `[Service]` type is only ever reachable via the container's own reflection, which the build-gate stripping story in [Build-gate symbol reference](#build-gate-symbol-reference) leans on.

Exemptions:
- `nameof(...)` - it erases to a string constant with no runtime dependency on the type.
- A reference from within the service's own declaration, including a private nested helper type - e.g. a `MonoBehaviour` that needs to call back into members the interface doesn't expose (see `LifetimeService`/`LifetimeServiceHelper`). Whatever removes the type for build-stripping removes everything declared inside it too, so this can never be left dangling.

A type nested inside a `[Service]` class *purely to hold data* (not to help implement it) does **not** get the nested-type exemption once referenced from outside - that's a sign the data type belongs on its own, independent of the service.

### FTF0006

**`[ServiceBuildProfile]`/`[ServiceBuildPlatform]` service isn't excluded from non-matching builds.** The attribute alone only filters candidates at container startup - the class still compiles into every build as dead code. Fires when a restricted service's declaration isn't wrapped in any `#if` at all, and includes the exact expression to use. See [Build-gate symbol reference](#build-gate-symbol-reference) for how that expression is derived, and [Code fixes](#code-fixes) for the one-click version.

### FTF0007

**`#if` doesn't cover every build the attribute allows.** Catches drift after `[ServiceBuildProfile]`/`[ServiceBuildPlatform]` changes (e.g. a platform is added) without the `#if` being updated to match - the dangerous direction specifically, since the service would then silently never be selected in whichever builds the stale `#if` now wrongly excludes, with no error anywhere, just a service that's mysteriously missing.

This brute-forces every combination of whatever symbols the two expressions (the existing `#if`, and the one the attribute now requires) actually reference - always a small number in practice - rather than attempting general boolean simplification, so the check is exact, not heuristic. It only checks that one direction: a `#if` that's *broader* than the attribute requires is wasteful, not wrong (the container still filters it out at runtime), so that's never flagged.

Two deliberate limitations:
- Skipped entirely for an `#if` with `#elif`/`#else` branches - too ambiguous which branch is "the" condition to compare against.
- A symbol outside FTFoundation's own set (e.g. your own extra flag deliberately ANDed into the `#if`) is treated as a free variable like any other, which means an intentional extra restriction can still surface a warning here. Narrow the *attribute* to match rather than treating that as a bug in the check.

---

## Code fixes

`FTFoundationBuildGateCodeFixProvider` handles both `FTF0006` and `FTF0007`, computing the exact same expression the diagnostic message already shows (via the shared `BuildGateSymbols.ComputeGateExpression`, so the message and the fix can never disagree):

- **FTF0006** inserts `#if <expression>` before the class declaration and `#endif` after it, via a plain text insertion rather than constructing directive trivia through `SyntaxFactory` (Roslyn's directive trivia needs correctly linked `#if`/`#endif` entries via an internal directive stack, which is fiddly to build by hand and easy to get subtly wrong; inserting text at two disjoint offsets and letting Roslyn re-parse can't produce a mismatched pair).
- **FTF0007** replaces just the existing condition's text span in place, leaving the rest of the `#if` line untouched.

---

## Build-gate symbol reference

`[ServiceBuildProfile]`/`[ServiceBuildPlatform]` values map to Unity scripting defines - the same ones `BuildProfileDetector`/`BuildPlatformDetector` already use at runtime - which is what `FTF0006`/`FTF0007` and their code fixes compute against.

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

### The Production simplification

`BuildProfileDetector` only ever defines one of `Editor`/`Development`/`Staging`/`Production` at a time, so combining `Production` with some subset of the other three is always exactly equivalent to negating whichever ones are *missing* - which is usually far simpler than spelling `Production` out as its own triple negation and OR-ing it in. Both `FTF0006`'s message and its code fix compute this automatically:

| Attribute | Generated `#if` |
| --- | --- |
| `Production \| Staging \| Editor` (i.e. everything except `Development`) | `!DEVELOPMENT_BUILD` |
| `Production` alone | `!UNITY_EDITOR && !DEVELOPMENT_BUILD && !STAGING_BUILD` |
| `Production \| Editor` (missing `Development` and `Staging`) | `!DEVELOPMENT_BUILD && !STAGING_BUILD` |

Combined profile+platform expressions are also only parenthesized when actually needed (i.e. one side is a multi-term `||`) - `FileLoggerService`'s real attributes (`Production | Staging | Editor` + `Desktop`) generate `#if !DEVELOPMENT_BUILD && UNITY_STANDALONE`, not a parenthesized, un-simplified mess.

### Editor caveat

This mapping is faithful for real player builds, but can diverge from the runtime check specifically *in the Editor*. `BuildPlatformDetector` reports `Desktop` in the Editor based on the host OS running Unity - always true on a PC/Mac/Linux machine. The `UNITY_STANDALONE` compile symbol instead tracks whichever Build Target is currently selected in Build Settings, even while just pressing Play. A dev with Build Target set to Android while working in the Editor would silently exclude a `Desktop`-gated service under `#if`, even though the runtime-only check would still include it today. This doesn't affect real platform builds, only that Editor edge case.

---

## Rebuilding

Source lives in `Runtime/Analyzers~/` (a `~`-suffixed folder, invisible to Unity):

```
dotnet build -c Release Runtime/Analyzers~/FTFoundation.Analyzers.csproj
dotnet build -c Release Runtime/Analyzers~/CodeFixes/FTFoundation.Analyzers.CodeFixes.csproj
```

Then copy each `bin/Release/netstandard2.0/*.dll` over the matching file in `Runtime/Analyzers/` - building only produces output in the source project's own `bin/`, it does **not** update the DLLs Unity actually loads. If updated warnings still don't show up in the Editor after copying, Unity's compiler daemon can keep an already-loaded analyzer assembly cached across incremental recompiles; right-click the DLL in the Project window and **Reimport**, or close and reopen the Editor to force a fresh compiler process.

**Do not bump `Microsoft.CodeAnalysis.CSharp` (or `.Workspaces`) past 3.8** in either `.csproj` - Unity's own bundled Roslyn only supports analyzers built against 3.8, and a newer version compiles fine but fails to load in Unity ("Unable to resolve reference 'Microsoft.CodeAnalysis'...").

After rebuilding, a DLL's import settings need: the `RoslynAnalyzer` label, "Any Platform" and "Editor" both unchecked (it's compile-time tooling, not a runtime dependency), and "Validate References" unchecked (Unity's static plugin-reference checker can't see the Roslyn assemblies the compiler host loads internally, so it will otherwise report the same false error even though the version is correct).
