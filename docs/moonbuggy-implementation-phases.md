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
- Code generation script: JSON → `CldrPluralRules.generated.cs` + `CldrPluralCategories.generated.cs`
- Generated per-locale methods: `(int n) → PluralCategory` using pure integer arithmetic
- Generated per-locale category lists for PO validation
- Tests for plural category selection across multiple locales

---

## Phase 6: Runtime Library

**Projects:** `MoonBuggy/`, `MoonBuggy.Tests`

**Deliverables:**
- `I18n` static class with `AsyncLocal<I18nContext>` and `MarkdownPipeline`
- `I18nContext` with `LCID` property
- `Translate._t()` with runtime fallback (parses MB syntax, resolves variables, handles plurals using CLDR rules)
- `Translate._m()` with runtime fallback (parses MB + markdown via Markdig, produces `IHtmlContent`)
- Tests for runtime output (test cases 3.1–3.4, 4.1–4.2), async context isolation

---

## Phase 7: Source Generator

**Projects:** `MoonBuggy.SourceGenerator/`, `MoonBuggy.SourceGenerator.Tests`

**Deliverables:**
- `MoonBuggyGenerator` (`IIncrementalGenerator`) — discovers call sites, reads PO files, emits interceptors
- `InterceptorEmitter` — generates per-message, per-locale methods with `TextWriter.Write()` chains
- `CallSiteAnalyzer` — extracts constant string, anonymous type properties, source location
- `Diagnostics` — MB0001–MB0008 descriptors
- `DiagnosticAnalyzer` — reports compile-time errors/warnings
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
