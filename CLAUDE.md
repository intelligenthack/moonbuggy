# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

MoonBuggy is a .NET i18n library (`intelligenthack/moonbuggy`) that co-exists with Lingui.js. It uses PO format with ICU MessageFormat and bakes translations at compile time via C# source generators and interceptors for zero-lookup, zero-allocation runtime. Both MoonBuggy and Lingui.js extractors write to the same PO files using `msgid` (ICU MessageFormat) as the shared key.

## Build Commands

```bash
dotnet build                          # Build entire solution
dotnet test                           # Run all tests
dotnet test tests/MoonBuggy.Tests     # Run unit tests only
dotnet test --filter "FullyQualifiedName~TestName"  # Run a single test
```

The CLI tool is invoked as `moonbuggy extract` / `moonbuggy validate` (installed via `dotnet tool install`).

## Solution Layout

```
src/MoonBuggy/                  # Runtime library (net8.0) — NuGet: intelligenthack.MoonBuggy
src/MoonBuggy.Core/             # Shared internals (netstandard2.0) — NOT a standalone package
src/MoonBuggy.SourceGenerator/  # Roslyn source generator (netstandard2.0)
src/MoonBuggy.Cli/              # CLI tool (net8.0) — dotnet tool
tests/MoonBuggy.Tests/          # Unit tests: runtime library (xUnit)
tests/MoonBuggy.Core.Tests/     # Unit tests: core parsing, ICU, PO, markdown, pseudo
tests/MoonBuggy.CldrGen.Tests/  # Unit tests: CLDR generation
tests/MoonBuggy.SourceGenerator.Tests/  # Source generator integration tests
tests/MoonBuggy.Cli.Tests/      # CLI integration tests
build/cldr/                     # CLDR plural rules download + codegen script
build/MoonBuggy.CldrGen/        # CLDR generation classes (netstandard2.0, standalone)
```

## Architecture

### Target Framework Constraints
- **MoonBuggy.Core** and **MoonBuggy.SourceGenerator** MUST target `netstandard2.0` — Roslyn requires this for analyzers/generators.
- **MoonBuggy** runtime and **MoonBuggy.Cli** target `net8.0` (interceptors require .NET 8+).
- The SourceGenerator NuGet package must be self-contained: Core and Markdig DLLs packed into `analyzers/dotnet/cs/`.

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

The project follows a 15-phase build order where each phase depends on previous ones. See `docs/moonbuggy-implementation-phases.md` for details. Phases 1–9 (core library) are complete. Remaining: (10) Initial Polish — DiagnosticAnalyzer, CLI flags, multi-target `net8.0;net10.0`, (11) Sample Project, (12) Microbenchmarks, (13) NuGet + CD, (14) GitHub Docs, (15) Docs Site.

## Key Design Decisions

- CLDR plural rules are **generated C# code** checked into the repo (`build/cldr/` script generates `Plural/*.generated.cs`). Builds don't require network access.
- Pseudolocalization is a compile-time switch (`MoonBuggyPseudoLocale` MSBuild property) — zero overhead when off.
- Configuration lives in `moonbuggy.config.json` (shared with Lingui.js) and `.csproj` properties (build-only).
- Test framework is **xUnit**. Source generator tests use `Microsoft.CodeAnalysis.CSharp.Analyzer.Testing`.

## Reference Documents

Detailed specs are in `docs/`:
- `moonbuggy-spec.md` — full specification with syntax rules and examples
- `moonbuggy-api-surface.md` — exact public types and method signatures
- `moonbuggy-test-cases.md` — expected behavior for all test scenarios
- `moonbuggy-project-structure.md` — solution layout and NuGet packaging
- `moonbuggy-implementation-phases.md` — phase dependencies and deliverables
