# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

MoonBuggy is a .NET i18n library (`intelligenthack/moonbuggy`) that co-exists with Lingui.js. It uses PO format with ICU MessageFormat and bakes translations at compile time via C# source generators and interceptors for zero-lookup, zero-allocation runtime. Both MoonBuggy and Lingui.js extractors write to the same PO files using `msgid` (ICU MessageFormat) as the shared key.

## Build Commands

```bash
dotnet build                                          # Build entire solution
dotnet test                                           # Run all tests (~293 tests)
dotnet test tests/MoonBuggy.Tests                     # Runtime library tests (22)
dotnet test tests/MoonBuggy.Core.Tests                # Core tests (134)
dotnet test tests/MoonBuggy.CldrGen.Tests             # CLDR generation tests (33)
dotnet test tests/MoonBuggy.Cli.Tests                 # CLI integration tests (41)
dotnet test tests/MoonBuggy.SourceGenerator.Tests     # Source generator tests (63)
dotnet test --filter "FullyQualifiedName~TestName"    # Run a single test
dotnet test --filter "FullyQualifiedName~A|FullyQualifiedName~B"  # Multiple (| = OR)
```

The CLI tool is invoked as `moonbuggy extract` / `moonbuggy validate` (installed via `dotnet tool install`).

## Solution Layout

```
src/MoonBuggy/                  # Runtime library (net8.0;net10.0) — NuGet: intelligenthack.MoonBuggy
src/MoonBuggy.Core/             # Shared internals (netstandard2.0) — NOT a standalone package
src/MoonBuggy.SourceGenerator/  # Roslyn source generator + DiagnosticAnalyzer (netstandard2.0)
src/MoonBuggy.Cli/              # CLI tool (net8.0;net10.0) — dotnet tool
tests/MoonBuggy.Tests/          # Unit tests: runtime library (xUnit, net10.0)
tests/MoonBuggy.Core.Tests/     # Unit tests: core (subfolders: Config/ Icu/ Markdown/ Parsing/ Po/ Pseudo/)
tests/MoonBuggy.CldrGen.Tests/  # Unit tests: CLDR generation (subfolder: Plural/)
tests/MoonBuggy.SourceGenerator.Tests/  # Source generator integration tests
tests/MoonBuggy.Cli.Tests/      # CLI integration tests
build/cldr/                     # CLDR plural rules download + codegen script (.csx)
build/MoonBuggy.CldrGen/        # CLDR generation classes (netstandard2.0, standalone, no Core dep)
```

## Architecture

### Target Framework Constraints
- **MoonBuggy.Core** and **MoonBuggy.SourceGenerator** MUST target `netstandard2.0` — Roslyn requires this for analyzers/generators.
- **MoonBuggy** runtime and **MoonBuggy.Cli** multi-target `net8.0;net10.0` (interceptors require .NET 8+).
- The SourceGenerator NuGet package must be self-contained: Core, Markdig, System.Text.Json, and System.Text.Encodings.Web DLLs packed into `analyzers/dotnet/cs/` via `GetDependencyTargetPaths` MSBuild target.
- Sample projects target `net8.0`.

### Core Processing Pipeline
1. **MbParser** tokenizes the custom `$var$` / `$...|...$` / `#var#` syntax from C# source
2. **IcuTransformer** converts MB tokens → ICU MessageFormat strings
3. **MarkdownPlaceholderExtractor** (for `_m()`) converts markdown → indexed `<0>`, `<1>` placeholders via Markdig
4. **PoReader/PoWriter** handle PO file I/O, preserving existing translations on update

### Source Generator Flow
The source generator reads PO files at build time and emits one interceptor method per `_t()`/`_m()` call site. Each interceptor contains a locale switch with direct `TextWriter.Write()` chains — no dictionary lookups or string allocations. CLDR plural rules are inlined as integer arithmetic.

### Fail-Fast Design
`Translate._t()` and `Translate._m()` throw `InvalidOperationException` when called without an active source generator interceptor. This surfaces a clear error if the source generator package is missing, rather than silently falling back.

### Public API (consumed via `using static MoonBuggy.Translate`)
- `_t(message, args?, context?)` → `TranslatedString` (readonly struct, `IHtmlContent`, implicit `string` conversion)
- `_m(message, args?, context?)` → `TranslatedHtml` via `IHtmlContent` (pre-rendered HTML)
- `I18n.Current.LCID` — per-async-context locale (AsyncLocal)
- First argument must be a compile-time constant string

## MB Variable Syntax (NOT standard ICU — custom syntax in C# source)

| Syntax | Meaning |
|--------|---------|
| `$var$` | Variable substitution → `{var}` in PO |
| `$...\|...$` | Plural block (pipe-separated forms) |
| `#var#` | Plural selector, rendered (inside plural block only) |
| `#~var#` | Plural selector, hidden |
| `#var=0#` | Plural with zero form (3 forms: =0 \| one \| other) |
| `$$` | Escaped literal `$` |
| `##` | Escaped literal `#` (inside plural blocks) |
| `\|\|` | Escaped literal `\|` (inside plural blocks) |

## Compiler Diagnostics

MB0001: non-constant first arg; MB0002: missing arg property; MB0003: extra arg property; MB0004: PO file not found (warning); MB0005: malformed MB syntax; MB0006: bad markdown output (warning); MB0007: empty message; MB0008: non-constant context; MB0009: plural selector is not an integer type.

## Implementation Phases

The project follows a 16-phase build order where each phase depends on previous ones. See `docs/moonbuggy-implementation-phases.md` for details. Phases 1–12 are complete. Next: (13) Microbenchmarks, (14) NuGet + CD (includes consumer build integration: `.props`/`.targets` auto-config, generator-emitted polyfill), (15) Docs Site, (16) Modern Razor Pipeline Compatibility (eliminate `UseRazorSourceGenerator=false` hack).

## Code Conventions

From `Directory.Build.props`: `LangVersion=latest`, `Nullable=enable`, `TreatWarningsAsErrors=true`, `ImplicitUsings=enable`.

Test projects require explicit `global using Xunit;` in `GlobalUsings.cs` — ImplicitUsings doesn't cover xUnit.

## Gotchas

- **netstandard2.0 polyfills**: Core needs `IsExternalInit` polyfill for records. Auto-generated files need explicit `#nullable enable` (CS8669). `switch` on struct with static readonly fields fails (CS9135) — use if/else.
- **Interceptor consumers need**: `<InterceptorsNamespaces>$(InterceptorsNamespaces);MoonBuggy.Generated</InterceptorsNamespaces>`.
- **Generator emits `file class InterceptsLocationAttribute`** polyfill — consumers don't need the runtime attribute.
- **`PreScanForPipe`**: MbParser uses this to solve ambiguity between `$var$` and `$...|...$` plural blocks.
- **Markdig strips trailing whitespace** from paragraphs — preserve spaces at segment boundaries.
- **LCID values**: `CultureInfo.GetCultureInfo("en").LCID` = 9 (neutral, not 1033); `"es"` = 10 (not 1034). Pseudo-locale LCID = 4096 (0x1000).
- **xUnit `--filter`**: Uses `|` for OR, not the word `or`.
- **`.csx` scripts**: `using var` not supported at top level — use `var` only.
- **CLDR regen**: Must `dotnet build build/MoonBuggy.CldrGen` before running the `.csx` script.
- **Core `InternalsVisibleTo`** → `MoonBuggy.Core.Tests`, `MoonBuggy`, `MoonBuggy.SourceGenerator`, `MoonBuggy.Cli`.
- **Source generator reads MSBuild properties** via `context.AnalyzerConfigOptionsProvider` → `build_property.PropertyName`. Testing requires custom `AnalyzerConfigOptionsProvider` impl.
- **`PluralBlockToken.SelectorVariable`** (not `.Variable`) for plural variable name.
- **DiagnosticAnalyzer** reports MB0001–MB0009 in real-time but does NOT report PO-dependent diagnostics (MB0004, MB0006).

## Key Design Decisions

- CLDR plural rules are **generated C# code** checked into the repo (`build/cldr/` script generates `Plural/*.generated.cs`). Builds don't require network access.
- Pseudolocalization is a compile-time switch (`MoonBuggyPseudoLocale` MSBuild property) — zero overhead when off.
- Configuration lives in `moonbuggy.config.json` (shared with Lingui.js) and `.csproj` properties (build-only).
- Test framework is **xUnit**. Source generator tests use `Microsoft.CodeAnalysis.CSharp.Analyzer.Testing`.
- CLDR generation classes (CldrRuleParser, IntegerSimplifier, CSharpPluralEmitter) live in standalone `MoonBuggy.CldrGen` project — `.csx` script references via `#r`.
- `CldrPluralRuleConditions.generated.cs` is the only generated CLDR file (conditions as strings for source generator).

## Reference Documents

Detailed specs are in `docs/`:
- `moonbuggy-spec.md` — full specification with syntax rules and examples
- `moonbuggy-api-surface.md` — exact public types and method signatures
- `moonbuggy-test-cases.md` — expected behavior for all test scenarios
- `moonbuggy-project-structure.md` — solution layout and NuGet packaging
- `moonbuggy-implementation-phases.md` — phase dependencies and deliverables
