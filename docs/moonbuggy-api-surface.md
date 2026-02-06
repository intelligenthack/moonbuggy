# MoonBuggy - Public API Surface

This document defines the exact public types, methods, and signatures for the MoonBuggy library. See [moonbuggy-spec.md](moonbuggy-spec.md) for design rationale and examples.

---

## Package: `intelligenthack.MoonBuggy`

Runtime library. Contains the translation entry points, locale management, and Markdig dependency for markdown processing.

### `I18n` — Locale management

```csharp
namespace MoonBuggy;

using Markdig;

/// <summary>
/// Static entry point for per-request i18n state and markdown configuration.
/// </summary>
public static class I18n
{
    private static readonly AsyncLocal<I18nContext> _current = new();

    /// <summary>
    /// Per-async-context i18n state. Set by application middleware,
    /// read by generated interceptor code.
    /// Lazily initialized — reading without setting returns a default
    /// context (LCID 0 = source locale). Safe for tests and
    /// single-locale apps that never set it.
    /// </summary>
    public static I18nContext Current
    {
        get => _current.Value ??= new I18nContext();
        set => _current.Value = value;
    }

    /// <summary>
    /// Markdig pipeline used for _m() markdown-to-HTML conversion
    /// in the runtime fallback path. Configure at app startup to
    /// match your application's markdown rendering.
    /// The CLI extractor and source generator use the default
    /// pipeline at build time.
    /// </summary>
    public static MarkdownPipeline MarkdownPipeline { get; set; }
        = new MarkdownPipelineBuilder().Build();
}
```

### `I18nContext` — Per-request context

```csharp
namespace MoonBuggy;

/// <summary>
/// Holds i18n state for the current async context.
/// Stored in AsyncLocal via I18n.Current.
/// </summary>
public class I18nContext
{
    /// <summary>
    /// LCID for the current locale.
    /// 0 = source locale (fallback/default).
    /// Set by application middleware per request.
    /// Read by generated interceptor code.
    /// </summary>
    public int LCID { get; set; }
}
```

### `Translate` — Translation entry points

```csharp
namespace MoonBuggy;

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Html;

/// <summary>
/// Translation entry points. Consumed via `using static MoonBuggy.Translate;`
/// so that _t() and _m() are available as bare function calls.
///
/// Method bodies provide runtime fallback behavior (source-locale string
/// with $var$ placeholders resolved). When the source generator is active,
/// C# interceptors redirect every call site to generated methods —
/// these bodies are never reached in production.
///
/// The fallback exists so that:
/// - Unit tests work without the source generator
/// - IDE IntelliSense shows meaningful return values
/// - Single-locale apps can use MB without the generator package
/// </summary>
public static class Translate
{
    /// <summary>
    /// Translate a plain-text message.
    /// </summary>
    /// <param name="message">
    /// MB-syntax string. Must be a compile-time constant (string literal,
    /// const variable, or local assigned from a literal). The source
    /// generator emits diagnostic MB0001 if this is not constant.
    /// Uses $var$ for variable substitution, $...|...$ for plurals.
    /// </param>
    /// <param name="args">
    /// Anonymous object whose property names match $var$ names in the message.
    /// Null when the message has no variables.
    /// </param>
    /// <param name="context">
    /// Optional disambiguation string for messages with identical English text
    /// but different meanings. Maps to PO msgctxt. Must be a compile-time constant.
    /// </param>
    /// <returns>Plain text string (HTML-unsafe — Razor will encode it).</returns>
    public static string _t(
        [ConstantExpected] string message,
        object? args = null,
        [ConstantExpected] string? context = null
    );

    /// <summary>
    /// Translate a markdown message. The developer writes markdown;
    /// the output is safe HTML rendered via Markdig.
    /// </summary>
    /// <param name="message">
    /// MB-syntax string with markdown. Any syntax supported by the
    /// configured Markdig pipeline can be used.
    /// Must be a compile-time constant. The source generator emits
    /// diagnostic MB0001 if this is not constant.
    /// </param>
    /// <param name="args">
    /// Anonymous object whose property names match $var$ names in the message.
    /// Null when the message has no variables.
    /// </param>
    /// <param name="context">
    /// Optional disambiguation string. Maps to PO msgctxt. Must be a compile-time constant.
    /// </param>
    /// <returns>IHtmlContent containing safe HTML (Razor renders without encoding).</returns>
    public static IHtmlContent _m(
        [ConstantExpected] string message,
        object? args = null,
        [ConstantExpected] string? context = null
    );
}
```

### How `_t` / `_m` are made available

```csharp
// Razor views — add to _ViewImports.cshtml:
@using static MoonBuggy.Translate

// C# classes:
using static MoonBuggy.Translate;

// Then use as bare calls:
var greeting = _t("Hello $name$!", new { name });
var html = _m("Click **here**");
```

### Customizing the Markdig pipeline

```csharp
// App startup — optional, only if you need non-default Markdig behavior
I18n.MarkdownPipeline = new MarkdownPipelineBuilder()
    .UseEmphasisExtras()
    .UseAutoLinks()
    .Build();
```

This affects the runtime fallback only. The CLI extractor and source generator
use the default Markdig pipeline at build time.

### Dependencies

- `Markdig` — CommonMark-compliant markdown processor (transitive dependency)

---

## Package: `intelligenthack.MoonBuggy.SourceGenerator`

No public API. Ships as a Roslyn analyzer + source generator.

### NuGet consumption

```xml
<PackageReference Include="intelligenthack.MoonBuggy.SourceGenerator"
                  Version="1.0.0"
                  PrivateAssets="all"
                  OutputItemType="Analyzer" />
```

### What it emits

- One **interceptor method** per `_t()` / `_m()` call site in the consuming project
- Generated methods are `internal static` in a `<RootNamespace>.MoonBuggyGenerated` class
- No public types are added to the consuming assembly
- Reads PO files from catalog paths configured in `moonbuggy.config.json`

### Compiler diagnostics

| ID | Severity | Description |
|----|----------|-------------|
| MB0001 | Error | First argument to `_t()` / `_m()` is not a compile-time constant string |
| MB0002 | Error | Variable `$var$` in message has no matching property in args object |
| MB0003 | Error | Property in args object has no matching `$var$` in message |
| MB0004 | Warning | PO file not found for configured locale — fallback to source locale |
| MB0005 | Error | Malformed MB syntax (unmatched `$`, invalid plural block) |
| MB0006 | Warning | Markdig produced unexpected or empty HTML from `_m()` message |
| MB0007 | Error | Empty message string passed to `_t()` or `_m()` |
| MB0008 | Error | `context` argument is not a compile-time constant string |

---

## Package: `intelligenthack.MoonBuggy.Cli`

No public API. Ships as a .NET tool.

### Installation

```bash
# Global
dotnet tool install -g intelligenthack.MoonBuggy.Cli

# Local (project-level)
dotnet tool install intelligenthack.MoonBuggy.Cli
```

### Commands

```
moonbuggy extract [files...]
    [--clean]           # Remove obsolete messages no longer in source
    [--locale <locale>] # Extract for specific locale(s) only
    [--verbose]         # Show detailed extraction info
    [--watch]           # Watch mode — re-extract on file changes

moonbuggy validate
    [--strict]          # Fail on any missing translations
    [--locale <locale>] # Validate specific locale(s)
```
