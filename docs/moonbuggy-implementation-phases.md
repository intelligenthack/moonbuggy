# MoonBuggy - Implementation Phases

See [moonbuggy-project-structure.md](moonbuggy-project-structure.md) for solution layout, [moonbuggy-test-cases.md](moonbuggy-test-cases.md) for expected behavior.

Each phase produces testable output. Later phases depend on earlier ones.

---

## Phase 1: MB Parser + ICU Transformer

**Projects:** `MoonBuggy.Core` (Parsing/, Icu/IcuTransformer), `MoonBuggy.Tests`

**Deliverables:**
- `MbParser` tokenizes MB source syntax: `$var$`, `$...|...$`, `#var#`, `#~var#`, `#var=0#`, escapes (`$$`, `##`, `||`)
- `IcuTransformer` converts parsed tokens to ICU MessageFormat strings
- Tests covering all extraction examples from the spec (test cases 1.1–1.5)

**Why first:** Foundation. Every other component depends on parsing MB syntax or producing ICU output.

---

## Phase 2: PO File Handling

**Projects:** `MoonBuggy.Core` (Po/), `MoonBuggy.Tests`

**Deliverables:**
- `PoReader` / `PoWriter` / `PoCatalog` / `PoEntry`
- Read PO files into memory, write/update PO files preserving existing translations
- Build vs. use existing library decision made here
- Tests for round-trip read/write, merge behavior (test cases 8.1–8.4)

---

## Phase 3: CLI Extract (basic — `_t()` only)

**Projects:** `MoonBuggy.Core` (Config/), `MoonBuggy.Cli`, `MoonBuggy.Cli.Tests`

**Deliverables:**
- Config file reader (`moonbuggy.config.json`)
- `SourceScanner` — walks .cs/.cshtml files, finds `_t()` calls, extracts string arguments
- `ExtractCommand` — full pipeline: scan → parse → ICU transform → write PO
- `_t()` messages only; `_m()` deferred to Phase 4
- Tests: end-to-end extraction against sample source files

**Why now:** First end-to-end deliverable. Produces real PO files from real source code, which makes testing later phases easier.

---

## Phase 4: Markdown Placeholder Extraction

**Projects:** `MoonBuggy.Core` (Markdown/), `MoonBuggy.Cli` (extend extract), `MoonBuggy.Tests`

**Deliverables:**
- `MarkdownPlaceholderExtractor` — converts markdown in `_m()` source strings to indexed placeholders via Markdig
- `PlaceholderMapping` — maps placeholder index to HTML tag pair
- Extend `SourceScanner` and `ExtractCommand` to handle `_m()` calls
- Tests covering all markdown extraction examples (test cases 2.1–2.7)

---

## Phase 5: CLDR Plural Rules Generation

**Projects:** `build/cldr/`, `MoonBuggy.Core` (Plural/), `MoonBuggy.Tests`

**Deliverables:**
- Download script for CLDR JSON (`plurals.json`)
- Code generation script: JSON → `CldrPluralRuleConditions.generated.cs` (conditions as strings for source generator)
- Generated per-locale plural rule conditions used by the source generator to emit inline plural selection
- Generated per-locale category lists for PO validation
- Tests for plural category selection across multiple locales

---

## Phase 6: Runtime Library

**Projects:** `MoonBuggy/`, `MoonBuggy.Tests`

**Deliverables:**
- `I18n` static class with `AsyncLocal<I18nContext>` and `MarkdownPipeline`
- `I18nContext` with `LCID` property
- `Translate._t()` — fail-fast body that throws `InvalidOperationException` when source generator is not active
- `Translate._m()` — fail-fast body that throws `InvalidOperationException` when source generator is not active
- `TranslatedString` (readonly struct, `IHtmlContent`, implicit `string` conversion) — zero-alloc `_t()` return type
- `TranslatedHtml` (class, `IHtmlContent`) — zero-alloc `_m()` return type
- Tests for fail-fast behavior, async context isolation

---

## Phase 7: Source Generator

**Projects:** `MoonBuggy.SourceGenerator/`, `MoonBuggy.SourceGenerator.Tests`

**Deliverables:**
- `MoonBuggyGenerator` (`IIncrementalGenerator`) — discovers call sites, reads PO files, emits interceptors
- `InterceptorEmitter` — generates per-message, per-locale methods with `TextWriter.Write()` chains
- `CallSiteAnalyzer` — extracts constant string, anonymous type properties, source location
- `Diagnostics` — MB0001–MB0009 descriptors
- Inline CLDR plural rules in generated code (zero-alloc)
- Markdown placeholder resolution in generated code
- Tests for interceptor generation, locale switching, diagnostics, fallback (test cases 5.1–5.6, 7.1)

---

## Phase 8: CLI Validate

**Projects:** `MoonBuggy.Cli` (extend), `MoonBuggy.Cli.Tests`

**Deliverables:**
- `ValidateCommand` — checks PO files for completeness, variable consistency, ICU validity, CLDR plural form correctness
- `--strict` flag for CI enforcement
- Tests for validation scenarios (test case 8.4)

---

## Phase 9: Pseudolocalization

**Projects:** `MoonBuggy.Core` (Pseudo/), `MoonBuggy.SourceGenerator` (extend), `MoonBuggy.Tests`

**Deliverables:**
- `PseudoLocalizer` — accent transform per the spec's mapping table
- Source generator integration: emit pseudo-locale case when `MoonBuggyPseudoLocale` is enabled
- Tests for accent mapping, variable/placeholder preservation (test cases 6.1–6.2)

---

## Phase 10: Initial Polish

**Projects:** `MoonBuggy.SourceGenerator` (extend), `MoonBuggy.Cli` (extend), all `.csproj` files (TFM)

**Deliverables:**
- Plural selector type checking — accept integer types (`byte`, `sbyte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`), reject `float`/`double`/`decimal` with diagnostic MB0009, emit strongly-typed interceptor code (test cases 10.8.1–10.8.7)
- `DiagnosticAnalyzer` — real-time IDE diagnostics for MB0001–MB0009 without waiting for full generator pass
- CLI extract flags: `[files...]`, `--locale`, `--watch`
- Multi-target `net8.0;net10.0` for runtime library and CLI

**Why first:** Multi-targeting directly affects NuGet package structure. DiagnosticAnalyzer improves the development experience before we put it in front of users. CLI flags complete the CLI surface before documenting it.

---

## Phase 11: Sample Project

**Projects:** `samples/MoonBuggy.Sample/` (new Razor Pages app)

**Deliverables:**
- Minimal ASP.NET Razor Pages or MVC app (`net8.0`)
- References `intelligenthack.MoonBuggy` + `intelligenthack.MoonBuggy.SourceGenerator` via ProjectReference
- `moonbuggy.config.json` with `en` + `es` locales
- Pages demonstrating:
  1. **Index** — simple `_t()` with variables, plurals, `_m()` with markdown
  2. **Locale switcher** — dropdown/links setting `I18n.Current.LCID` via middleware
  3. **Context disambiguation** — same English text, different translations
- Pre-populated PO files for English + Spanish
- `README.md` inside `samples/` explaining how to run it
- Sample is buildable standalone (`dotnet run` from its directory)
- Sample is NOT included in the solution test run
- DO NOT CHANGE THE LIBRARY. THE LIBRARY MUST WORK AS IS. USE RAZOR SYNTAX.

**Why now:** Acts as a real-world integration test for the library. Shakes out bugs, missing features, or ergonomic issues before anything is packaged and published. Also provides the concrete app that benchmarks (Phase 12), docs (Phase 14), and packaging smoke tests (Phase 13) all reference.

---

## Phase 12: Microbenchmarks

**Projects:** `tests/MoonBuggy.Benchmarks/` (new), update docs

**Deliverables:**
- BenchmarkDotNet project targeting `net10.0`
- Benchmarks covering the hot paths:
  1. **`TranslatedString.WriteTo` vs `string` baseline** — single-segment and multi-segment, measure allocations
  2. **`TranslatedHtml.WriteTo` vs `HtmlString` baseline** — same comparison
  3. **Source-locale interceptor** — simple message, variable message, plural message
  4. **Multi-locale interceptor** — LCID switch with 3–5 locales
  5. **`ToString()` path** — when `TranslatedString` is used as `string` in C# code
- Results markdown file: `docs/benchmarks.md` with tables showing throughput + allocations
- If benchmarks reveal regressions or missed optimizations, fix them in this phase (sample app validates fixes)

**Key design decisions:**
- Benchmark project is NOT included in the solution test run (no `dotnet test` — run manually via `dotnet run -c Release`)
- Compare against a "naive" baseline (plain `string.Concat` + `HtmlString`) to quantify the improvement
- Use `[MemoryDiagnoser]` to prove zero-alloc claims

**Why after sample:** The sample provides a realistic usage context. Benchmark results validate the "zero allocation" claim before packaging. Any perf fixes can be immediately verified against the sample.

---

## Phase 13: NuGet Packaging + CD Pipeline

**Projects:** All `.csproj` files, `.github/workflows/release.yml` (new), `Directory.Build.props`

**Deliverables:**

### Package metadata (Directory.Build.props or per-project)
- `PackageId`: `intelligenthack.MoonBuggy`, `intelligenthack.MoonBuggy.SourceGenerator`, `intelligenthack.MoonBuggy.Cli`
- Version strategy: single `Version` property in `Directory.Build.props`, overridable by CI
- Standard metadata: Authors, Description, License, ProjectUrl, RepositoryUrl, Tags, PackageReadmeFile, PackageIcon

### Source generator self-containment
- Pack `MoonBuggy.Core.dll` + `Markdig.dll` into `analyzers/dotnet/cs/` folder
- Ensure no runtime dependency leaks (consumer never sees Core or Markdig as transitive deps)

### CLI tool packaging
- `PackAsTool`, `ToolCommandName` already set — add metadata
- Verify `dotnet tool install --global` works from the .nupkg

### CD pipeline (`.github/workflows/release.yml`)
- Trigger: GitHub Release creation (tag `v*`)
- Steps: checkout → setup .NET → CLDR download → build → test → pack → push to nuget.org
- Uses `NUGET_API_KEY` secret
- Separate from CI workflow (CI runs on push/PR, CD runs on release)

### Packaging smoke test
- `dotnet pack` produces 3 .nupkg files
- Switch sample project from ProjectReference to local .nupkg PackageReference
- Verify sample builds and runs against packages (catches missing DLLs, wrong TFMs, broken analyzer loading)
- Sample then reverts to ProjectReference for day-to-day development (the PackageReference test is CI-only or scripted)

**Why after sample+benchmarks:** The library is battle-tested by the sample app and performance-validated by benchmarks. Packaging is a mechanical step that wraps known-good code.

---

## Phase 14: GitHub Documentation

**Projects:** `README.md` (update), `docs/` markdown files

**Deliverables:**
- **README.md** refresh:
  - Add badges (CI status, NuGet version, license)
  - Add "Quick Start" section with 3-step install + configure + use
  - Link to sample project
  - Link to docs site (placeholder until Phase 15)
  - Add benchmark highlights (from Phase 12 results)
- **`docs/getting-started.md`** — step-by-step setup for a new project
- **`docs/syntax-reference.md`** — consolidate variable, plural, markdown, escaping syntax (extracted from the spec into a user-facing format)
- **`docs/cli-reference.md`** — `extract` and `validate` commands with all flags
- **`docs/configuration.md`** — `moonbuggy.config.json` format, MSBuild properties, locale setup
- **`docs/lingui-coexistence.md`** — how to share PO files with Lingui.js
- **CONTRIBUTING.md** update — reference the sample project, build instructions

**Key principle:** GitHub docs are concise and practical. They answer "how do I use this?" Not "how does it work internally?" — that's for the docs site.

Existing docs (`moonbuggy-spec.md`, `moonbuggy-api-surface.md`, etc.) are preserved as internal reference. The new user-facing docs are a separate layer.

---

## Phase 15: Documentation Site

**Projects:** `docs-site/` (new Docusaurus project), `.github/workflows/docs.yml` (new)

**Deliverables:**

### Docusaurus site structure

1. **Getting Started**
   - Introduction (what + why)
   - Installation & Setup
   - Quick Start Tutorial (build the sample app step-by-step)

2. **Guides**
   - Variable Syntax
   - Pluralization
   - Markdown Translations (`_m()`)
   - Locale Management
   - Pseudolocalization
   - Lingui.js Co-existence
   - Testing Translations
   - Bundle Size & Performance (link benchmark results)

3. **API Reference**
   - `Translate._t()` / `_m()`
   - `TranslatedString` / `TranslatedHtml`
   - `I18n` / `I18nContext`
   - CLI Commands
   - Configuration
   - Compiler Diagnostics (MB0001–MB0009)

4. **Resources**
   - Sample Project
   - PO File Format
   - ICU MessageFormat Primer
   - Migration Guide (for future versions)

### Deployment
- GitHub Actions workflow: build Docusaurus → deploy to GitHub Pages
- Triggered on push to `main` (docs changes only) or manual dispatch
- Custom domain optional (can start with `intelligenthack.github.io/moonbuggy`)

### Content sourcing
- Docs site content is written fresh for the site — not a copy of the GitHub markdown
- GitHub docs are concise quick-reference; Docusaurus docs are detailed tutorials with examples
- Code examples in the docs site pull from or reference the sample project

---

## Dependency Graph

```
Phase 10 (Polish)
    ↓
Phase 11 (Sample App)     ← integration test for the library, uses ProjectReference
    ↓
Phase 12 (Benchmarks)     ← may feed fixes back; sample validates fixes work end-to-end
    ↓
Phase 13 (NuGet + CD)     ← switches sample to PackageReference as packaging smoke test
    ↓
Phase 14 (GitHub Docs)    ← references sample, real NuGet commands, benchmark results
    ↓
Phase 15 (Docs Site)      ← comprehensive, benefits from everything above being stable
```

## Summary Table

| Phase | Name | Depends On | Key Output |
|-------|------|-----------|------------|
| 1 | MB Parser + ICU Transformer | — | Tokenizer, ICU conversion |
| 2 | PO File Handling | 1 | PoReader/PoWriter/PoCatalog |
| 3 | CLI Extract (`_t()`) | 1, 2 | End-to-end PO extraction |
| 4 | Markdown Placeholders | 3 | `_m()` extraction via Markdig |
| 5 | CLDR Plural Rules | — | Generated plural category methods |
| 6 | Runtime Library | 1, 5 | `I18n`, `Translate._t()`/`_m()` |
| 7 | Source Generator | 1–6 | Interceptor emission, zero-alloc rendering |
| 8 | CLI Validate | 2, 5 | PO validation command |
| 9 | Pseudolocalization | 7 | Accent transforms, pseudo-locale |
| 10 | Initial Polish | 1–9 | Multi-target TFMs, DiagnosticAnalyzer, CLI flags |
| 11 | Sample Project | 10 | Razor Pages app — integration test |
| 12 | Microbenchmarks | 11 | BenchmarkDotNet project, results doc |
| 13 | NuGet + CD | 12 | 3 NuGet packages, GitHub Actions release workflow |
| 14 | GitHub Docs | 13 | User-facing markdown docs, README refresh |
| 15 | Docs Site | 14 | Docusaurus site on GitHub Pages |
