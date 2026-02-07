# MoonBuggy - Project Structure

See [moonbuggy-spec.md](moonbuggy-spec.md) for design rationale, [moonbuggy-api-surface.md](moonbuggy-api-surface.md) for public API.

**Repository:** `intelligenthack/moonbuggy`

---

## Solution Layout

```
moonbuggy/
├── src/
│   ├── MoonBuggy/                         # NuGet: intelligenthack.MoonBuggy
│   ├── MoonBuggy.Core/                    # Internal shared library (not shipped alone)
│   ├── MoonBuggy.SourceGenerator/         # NuGet: intelligenthack.MoonBuggy.SourceGenerator
│   └── MoonBuggy.Cli/                     # NuGet: intelligenthack.MoonBuggy.Cli (dotnet tool)
├── build/
│   ├── cldr/
│   │   ├── plurals.json                    # Downloaded from Unicode CLDR
│   │   └── generate-plural-rules.csx       # Script: JSON → generated C# in Core
│   ├── MoonBuggy.CldrGen/                  # CLDR generation classes (netstandard2.0)
│   └── GenerateCldr.targets                # MSBuild target wired into Core build
├── tests/
│   ├── MoonBuggy.Tests/                   # Unit tests: runtime library
│   ├── MoonBuggy.Core.Tests/             # Unit tests: core parsing, ICU, PO, markdown, pseudo
│   ├── MoonBuggy.CldrGen.Tests/          # Unit tests: CLDR generation
│   ├── MoonBuggy.SourceGenerator.Tests/   # Integration tests: generated code
│   └── MoonBuggy.Cli.Tests/              # Integration tests: extract + validate
├── MoonBuggy.slnx
├── Directory.Build.props                   # Shared build properties
└── README.md
```

---

## Project Details

### `src/MoonBuggy/` — Runtime Library

**NuGet package:** `intelligenthack.MoonBuggy`

**Target:** `net8.0`

**Dependencies:**
- `Markdig`
- `Microsoft.AspNetCore.Html.Abstractions` (for `IHtmlContent`)

**Contents:**

| File | Description |
|------|-------------|
| `I18n.cs` | Static class with `AsyncLocal<I18nContext>` and `MarkdownPipeline` property |
| `I18nContext.cs` | Per-async-context state (LCID) |
| `Translate.cs` | `_t()` and `_m()` — fail-fast bodies that throw `InvalidOperationException` |

The method bodies throw `InvalidOperationException` when called without an active source generator interceptor, surfacing a clear error rather than silently falling back.

---

### `src/MoonBuggy.Core/` — Shared Internal Library

**Not a standalone NuGet package.** Compiled into both the runtime library and the source generator package. Contains all parsing, transformation, and data logic shared between components.

**Target:** `netstandard2.0` (widest compatibility — source generators require netstandard2.0)

**Dependencies:**
- `Markdig` (for markdown-to-placeholder transform)

**Contents:**

| File / Directory | Description |
|-----------------|-------------|
| `Parsing/MbParser.cs` | Tokenizes MB source syntax: `$var$`, `$...\|...$`, `#var#`, `#~var#`, `#var=0#`, escapes (`$$`, `##`, `\|\|`) |
| `Parsing/MbToken.cs` | Token types: `Text`, `Variable`, `PluralBlock`, `PluralSelector`, etc. |
| `Icu/IcuTransformer.cs` | Transforms parsed MB tokens → ICU MessageFormat string |
| `Icu/IcuParser.cs` | Parses ICU MessageFormat strings (for reading PO msgid/msgstr) |
| `Markdown/MarkdownPlaceholderExtractor.cs` | Converts markdown → indexed placeholders (`<0>`, `<1>`) via Markdig |
| `Markdown/PlaceholderMapping.cs` | Maps placeholder index → HTML tag pair (open/close) |
| `Po/PoReader.cs` | Reads PO files into an in-memory catalog |
| `Po/PoWriter.cs` | Writes/updates PO files, preserving existing translations |
| `Po/PoCatalog.cs` | In-memory representation of a PO file (entries keyed by msgid + msgctxt) |
| `Po/PoEntry.cs` | Single PO entry: msgid, msgstr, msgctxt, comments |

| `Plural/CldrPluralRuleConditions.generated.cs` | **Generated at build time.** Per-locale plural rule conditions as strings, used by the source generator to emit inline plural selection. Generated from CLDR JSON data via `build/cldr/generate-plural-rules.csx`. |
| `Plural/PluralCategory.cs` | Enum: `Zero`, `One`, `Two`, `Few`, `Many`, `Other` |
| `Pseudo/PseudoLocalizer.cs` | Accent transform for pseudolocalization |
| `Config/MoonBuggyConfig.cs` | Reads/represents `moonbuggy.config.json` |

---

### `src/MoonBuggy.SourceGenerator/` — Source Generator + Analyzer

**NuGet package:** `intelligenthack.MoonBuggy.SourceGenerator`

**Target:** `netstandard2.0` (required for Roslyn analyzers/generators)

**Dependencies:**
- `Microsoft.CodeAnalysis.CSharp` (Roslyn APIs)
- `MoonBuggy.Core` (packed into the analyzer package, not a runtime dependency)

**Packaging:** The `.csproj` must pack `MoonBuggy.Core` into the analyzer output so the generator is self-contained. Uses `GetTargetPathDependsOn` or similar to include the Core assembly in the `analyzers/dotnet/cs` folder.

**Contents:**

| File | Description |
|------|-------------|
| `MoonBuggyGenerator.cs` | `IIncrementalGenerator` entry point — discovers `_t()`/`_m()` call sites, reads PO files, emits interceptors |
| `InterceptorEmitter.cs` | Generates interceptor methods: locale switch + `TranslatedString`/`TranslatedHtml` construction |
| `CallSiteAnalyzer.cs` | Extracts constant string argument, anonymous type properties, call site location |
| `Diagnostics.cs` | MB0001–MB0008 diagnostic descriptors (diagnostics reported inline from generator) |

---

### `src/MoonBuggy.Cli/` — CLI Tool

**NuGet package:** `intelligenthack.MoonBuggy.Cli` (packed as dotnet tool)

**Target:** `net8.0`

**Dependencies:**
- `MoonBuggy.Core` (project reference)

**Contents:**

| File | Description |
|------|-------------|
| `Program.cs` | Entry point, command parsing, formatted output |
| `Commands/ExtractCommand.cs` | `moonbuggy extract` — scans source files, extracts messages, updates PO files |
| `Commands/ValidateCommand.cs` | `moonbuggy validate` — validates PO files for completeness and correctness |
| `SourceScanner.cs` | Walks .cs/.cshtml files, finds `_t()`/`_m()` calls, extracts constant string arguments |

---

## Test Projects

### `tests/MoonBuggy.Tests/`

Unit tests for the runtime library.

**Framework:** xUnit

**Coverage areas:**
- `_t()` / `_m()` fail-fast behavior (throws `InvalidOperationException`)
- `I18n.Current` async context isolation

### `tests/MoonBuggy.Core.Tests/`

Unit tests for core logic (parsing, ICU, PO, markdown, pseudo, config).

**Framework:** xUnit

**Coverage areas:**
- MB syntax parser (all token types, escaping, edge cases)
- MB → ICU transformation
- ICU parsing
- PO file reading and writing (round-trip)
- Markdown → placeholder extraction
- Placeholder mapping resolution
- Pseudolocalization accent transform
- Config file loading

### `tests/MoonBuggy.CldrGen.Tests/`

Unit tests for CLDR plural rule generation.

**Framework:** xUnit

**Coverage areas:**
- CLDR rule parsing
- Integer simplification
- C# plural emitter code generation

### `tests/MoonBuggy.SourceGenerator.Tests/`

Integration tests for the source generator.

**Framework:** xUnit

**Coverage areas:**
- Interceptor generation for each message type
- Locale switching in generated code
- Diagnostics (MB0001–MB0008) trigger correctly
- Fallback when PO translation is missing
- Plural code generation with CLDR rules
- Markdown placeholder resolution in generated code
- Pseudolocalization integration

### `tests/MoonBuggy.Cli.Tests/`

Integration tests for the CLI.

**Framework:** xUnit

**Coverage areas:**
- Extract command: new entries, preserved translations, `--clean`
- Validate command: completeness checks, `--strict`, `--locale`, `--verbose`
- Source scanner regex extraction
- Statistics output format

---

## NuGet Package Structure

### `intelligenthack.MoonBuggy`

Standard library package. Contains runtime DLL + Markdig dependency.

```
lib/
  net8.0/
    MoonBuggy.dll
    MoonBuggy.Core.dll    # internalized or ILMerged
```

### `intelligenthack.MoonBuggy.SourceGenerator`

Analyzer package. Must be self-contained (no runtime dependency resolution).

```
analyzers/
  dotnet/cs/
    MoonBuggy.SourceGenerator.dll
    MoonBuggy.Core.dll              # packed alongside
    Markdig.dll                       # packed alongside (needed by Core)
```

Uses `PrivateAssets="all"` + `OutputItemType="Analyzer"` on the consumer side.

### `intelligenthack.MoonBuggy.Cli`

Dotnet tool package.

```xml
<PackAsTool>true</PackAsTool>
<ToolCommandName>moonbuggy</ToolCommandName>
```

---

## Key Build Constraints

1. **MoonBuggy.Core targets `netstandard2.0`** — required because source generators must target netstandard2.0. The Core library is shared, so it must use the lowest common denominator.

2. **Source generator must be self-contained** — all dependencies (Core, Markdig) must be packed into the analyzer folder. The compiler won't resolve NuGet dependencies for analyzers at runtime.

3. **Runtime library targets `net8.0`** — interceptors require .NET 8+. No need for broader TFM support.

4. **CLDR plural rules are generated code** — a build-time script downloads CLDR JSON (`unicode-org/cldr-json` on GitHub, `cldr-core/supplemental/plurals.json`) and generates C# source files into `MoonBuggy.Core/Plural/`. The generated code contains only the plural selection logic needed (pure integer arithmetic — modulo checks), not the entire CLDR dataset. This keeps the library small while ensuring the source generator can emit correct, zero-alloc plural selection inline. The generated files are checked into the repo so builds don't require network access; the script is re-run when upgrading CLDR versions.
