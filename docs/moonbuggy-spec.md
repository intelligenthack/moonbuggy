# MoonBuggy - Internationalization Library Specification

**Repository:** `intelligenthack/moonbuggy`

**Purpose:** Modern i18n library for .NET that co-exists with Lingui.js, uses PO format with ICU MessageFormat, and bakes translations in at compile time for zero-lookup, zero-allocation runtime performance. Designed to co-exist with Lingui.js for applications that have both server-rendered views and JavaScript.

**API Surface:** [moonbuggy-api-surface.md](moonbuggy-api-surface.md) — exact public types, methods, signatures, and compiler diagnostics.

**Test Cases:** [moonbuggy-test-cases.md](moonbuggy-test-cases.md) — expected behavior derived from this spec.

**Project Structure:** [moonbuggy-project-structure.md](moonbuggy-project-structure.md) — solution layout, packages, and build constraints.

**Implementation Phases:** [moonbuggy-implementation-phases.md](moonbuggy-implementation-phases.md) — build order and per-phase deliverables.

---

## Design Principles

1. MoonBuggy (MB) is a **standalone i18n library** for .NET
2. MB **co-exists with Lingui.js** — applications may have code in JS and server-rendered views
3. **No internationalization of DB content** — only code/view strings
4. Pure backend i18n is supported but is a **secondary concern** — views are the primary surface
5. Both extractors (MB + Lingui.js) **write to the same PO files** so there is one unified set of strings to send for translation
6. MB extracts strings with a CLI command similar to `lingui extract`
7. MB **bakes in strings at compile time** — the compiled code does zero lookups and zero allocations. The only runtime parameter is the **locale**; all else is baked in
8. MB uses the following **variable syntax** (`$var$`) in C# source code, with `#var#` used exclusively inside plural blocks. The extractor transforms these to ICU format when writing PO files
9. The **ID strategy is consistent** between MB and Lingui.js — both use `msgid` (the ICU MessageFormat string) as the key in PO files, avoiding conflicts

---

## Variable Syntax (C# Source Code)

MB uses `$var$` for all variable substitution outside of plural blocks:

| Syntax | Where | Meaning | ICU output in PO file |
|--------|-------|---------|----------------------|
| `$name$` | Anywhere | Variable substitution | `{name}` |
| `#var#` | Inside `$...\|...$` only | Numeric count (rendered) | `#` (ICU plural count) |
| `#~var#` | Inside `$...\|...$` only | Numeric count (hidden, selector only) | *(nothing — used for selection)* |
| `#var=0#` | Start of `$...\|...\|...$` | Selector with zero block (rendered) | `=0` form + `one` + `other` |
| `#~var=0#` | Start of `$...\|...\|...$` | Selector with zero block (hidden) | `=0` form + `one` + `other` |

### Escaping

- `$$` produces a literal `$` character. E.g., `_t("Price: $$5")` → `"Price: $5"`. Note that `$5` alone is already unambiguous (not a valid variable name), but `$$marco$$` is needed to produce the literal text `$marco$` instead of a variable substitution.
- `##` inside a plural block produces a literal `#` character. Outside plural blocks, `#` has no special meaning and is used verbatim.
- `||` inside a `$...|...$` block produces a literal `|` character (see Plural Syntax rule 9).

### Design rationale

- `$var$` is visually distinct from C# string interpolation `{var}`, avoiding ambiguity
- `#var#` only appears inside plural blocks where it signals "this is the numeric selector and should be rendered"
- The extractor transforms `$var$` to standard ICU `{var}` in PO files — translators never see `$` syntax

### Simple examples

```csharp
// Text variable
_t("Welcome to $name$!", new { name = site.Name })
// PO msgid: "Welcome to {name}!"

// All variables use $var$ — even numeric ones when not pluralized
_t("Page $num$ of $total$", new { num = currentPage, total = pageCount })
// PO msgid: "Page {num} of {total}"
```

---

## Plural Syntax

Pluralization uses the `$...|...$` block syntax. The `|` (pipe) separates plural forms.

### Rules

1. `$...$` **without** `|` → simple variable substitution
2. `$...|...$` **with** `|` → plural block
3. The **first** `#var#`, `#~var#`, or `#var=0#` / `#~var=0#` in a plural block identifies the **selector variable**
4. `#var#` — selector variable, **rendered** (count is shown)
5. `#~var#` — selector variable, **not rendered** at that position (count is hidden)
6. `#var=0#` — selector with zero block: three forms `=0 | one | other` (count rendered)
7. `#~var=0#` — selector with zero block: three forms `=0 | one | other` (count hidden)
8. Subsequent `#var#` (without `~`) within any form **renders** the count value
9. `||` escapes a literal pipe character inside a `$...$` block

**Form count:**
- Without `=0`: two pipe-separated forms → `one | other`
- With `=0`: three pipe-separated forms → `=0 | one | other`

### Examples

**Two-form plural (one | other):**
```csharp
_t("You have $#x# book|#x# books$", new { x })
```
PO file:
```po
msgid "You have {x, plural, one {# book} other {# books}}"
```
Output with `x=1`: `"You have 1 book"`
Output with `x=3`: `"You have 3 books"`

**Three-form plural with zero (=0 | one | other):**
```csharp
_t("You have $#x=0#no books|#x# book|#x# books$", new { x })
```
PO file:
```po
msgid "You have {x, plural, =0 {no books} one {# book} other {# books}}"
```
Output with `x=0`: `"You have no books"`
Output with `x=1`: `"You have 1 book"`
Output with `x=5`: `"You have 5 books"`

**Zero with hidden count:**
```csharp
_t("$#~x=0#no messages|one new message|#x# new messages$", new { x })
```
PO file:
```po
msgid "{x, plural, =0 {no messages} one {one new message} other {# new messages}}"
```
Output with `x=0`: `"no messages"`
Output with `x=1`: `"one new message"`
Output with `x=5`: `"5 new messages"`

**Plural with count hidden in one form (no zero):**
```csharp
_t("$#~y#just one apple|many apples (#y#)!$", new { y })
```
PO file:
```po
msgid "{y, plural, one {just one apple} other {many apples (#)!}}"
```
Output with `y=1`: `"just one apple"`
Output with `y=7`: `"many apples (7)!"`

**Full composed example — text vars + multiple plurals:**
```csharp
_t("Hi $name$, you have $#x# book|#x# books$ and $#~y#just one apple|many apples (#y#)!$", new { name, x, y })
```
PO file:
```po
msgid "Hi {name}, you have {x, plural, one {# book} other {# books}} and {y, plural, one {just one apple} other {many apples (#)!}}"
```
Output with `name="Alice", x=3, y=1`: `"Hi Alice, you have 3 books and just one apple"`
Output with `name="Alice", x=1, y=7`: `"Hi Alice, you have 1 book and many apples (7)!"`

### How translators add plural forms

Developers only specify **source language** (English) forms: `=0`, `one`, and `other`. Translators add forms for their language in the PO file:

```po
# English source
msgid "You have {x, plural, one {# book} other {# books}}"
msgstr "You have {x, plural, one {# book} other {# books}}"

# Russian translation (translator adds few/many)
msgid "You have {x, plural, one {# book} other {# books}}"
msgstr "У вас {x, plural, one {# книга} few {# книги} many {# книг} other {# книг}}"

# Japanese translation (no plural forms)
msgid "You have {x, plural, one {# book} other {# books}}"
msgstr "本が{x}冊あります"
```

CLDR plural categories: `zero`, `one`, `two`, `few`, `many`, `other` — each language uses a subset.

---

## PO File Format

MB uses standard **gettext PO format** with **ICU MessageFormat** in `msgid`/`msgstr` (matching Lingui.js):

```po
# Simple string
msgid "Save changes"
msgstr "Guardar cambios"

# String with variables
msgid "Welcome to {name}!"
msgstr "Bienvenido a {name}!"

# Pluralization
msgid "You have {x, plural, one {# book} other {# books}}"
msgstr "Tienes {x, plural, one {# libro} other {# libros}}"
```

### Co-existence with Lingui.js

Both extractors write to the **same PO files**. The `msgid` is the key — if both MB and Lingui.js extract `"Save changes"`, it's one PO entry with one translation.

```bash
# Extract from C#
moonbuggy extract

# Extract from JavaScript
npx lingui extract

# Both update the same messages.po files
```

### Context / disambiguation

No context by default — most strings are unique enough that `msgid` alone disambiguates. For the rare case where the same English string needs different translations, use an explicit `context` parameter (matches Lingui.js):

```csharp
_t("Submit", context: "button")
_t("Submit", context: "form-label")
```

PO file uses standard `msgctxt`:
```po
msgctxt "button"
msgid "Submit"
msgstr "Enviar"

msgctxt "form-label"
msgid "Submit"
msgstr "Presentar"
```

Works with `_m()` too:
```csharp
_m("Click **here**", context: "navigation")
```

Context values are free-form strings chosen by the developer. The extractor includes `msgctxt` in the PO output. Two entries with the same `msgid` but different `msgctxt` are treated as separate messages.

---

## Compile-Time Baking

**Target:** .NET 8+ (required for interceptors)

**Strategy:** Source generator + C# interceptors

The compiled code contains **no dictionary lookups and no string allocations** for translated messages. The only runtime input is the current locale. All translations are baked into generated code.

### How it works

**1. Source generator reads PO files at build time** and generates an optimized method per message, per locale. For `_m()` messages, placeholder-to-HTML mapping is resolved at this stage — the generated code writes final HTML directly.

**2. Generated methods** write directly to the output stream (e.g. `TextWriter` in Razor views). No intermediate string allocation:

```csharp
// Conceptual generated code for: _t("Welcome to $name$!", new { name })
// PO: msgid "Welcome to {name}!" / msgstr "Bienvenido a {name}!"
static void __msg_a1b2c3(TextWriter writer, int lcid, string name) {
    switch (lcid) {
        case 10: // Spanish
            writer.Write("Bienvenido a ");
            writer.Write(name);
            writer.Write("!");
            break;
        default:
            writer.Write("Welcome to ");
            writer.Write(name);
            writer.Write("!");
            break;
    }
}
```

For `_m()` with placeholders, the generated code writes resolved HTML directly:
```csharp
// Conceptual generated code for: _m("Click **[here]($url$)** to continue", new { url })
// PO: msgid "Click <0><1>here</1></0> to continue"
// PO: msgstr "Haz clic <0><1>aquí</1></0> para continuar"
static void __msg_x7y8z9(TextWriter writer, int lcid, string url) {
    switch (lcid) {
        case 10: // Spanish
            writer.Write("Haz clic <strong><a href=\"");
            writer.Write(url);
            writer.Write("\">aquí</a></strong> para continuar");
            break;
        default:
            writer.Write("Click <strong><a href=\"");
            writer.Write(url);
            writer.Write("\">here</a></strong> to continue");
            break;
    }
}
```

For plurals, the generated code includes CLDR rules inline:
```csharp
// Conceptual generated code for: _t("You have $#x# book|#x# books$", new { x })
static void __msg_p1q2r3(TextWriter writer, int lcid, int x) {
    switch (lcid) {
        case 25: // Russian
            writer.Write("У вас ");
            // Russian CLDR rules baked in
            var mod10 = x % 10; var mod100 = x % 100;
            if (mod10 == 1 && mod100 != 11) { writer.Write(x); writer.Write(" книга"); }
            else if (mod10 >= 2 && mod10 <= 4 && (mod100 < 10 || mod100 >= 20)) { writer.Write(x); writer.Write(" книги"); }
            else { writer.Write(x); writer.Write(" книг"); }
            break;
        default:
            writer.Write("You have ");
            if (x == 1) { writer.Write("1 book"); }
            else { writer.Write(x); writer.Write(" books"); }
            break;
    }
}
```

**3. C# interceptors** redirect each `_t()` / `_m()` call site to the corresponding generated method. The original `_t()` method body is never reached in production:

```csharp
// Developer writes:
@_t("Welcome to $name$!", new { name })

// Interceptor redirects this specific call site to:
__msg_a1b2c3(Output, CurrentLCID, name)
```

Zero lookup — the compiler wires the call directly to the generated method at each call site.

### Return types

- `_t()` → always returns `string` (plain text)
- `_m()` → always returns `IHtmlContent` (contains HTML markup — Razor renders it without encoding)

### Razor `@` optimization

In Razor `@` expressions, the source generator can intercept and write directly to the `TextWriter` for zero allocation:

- `@_t(...)` → writes to TextWriter with **HTML encoding** (plain text must be encoded for safe output)
- `@_m(...)` → writes to TextWriter **without encoding** (output is already safe HTML, generated from markdown)

```csharp
// Normal C# code
string msg = _t("Welcome!");           // returns string
IHtmlContent html = _m("Click **here**"); // returns IHtmlContent

// Razor — source generator intercepts, writes directly to TextWriter
@_t("Welcome!")           // → HtmlEncode + Write to TextWriter
@_m("Click **here**")    // → Write to TextWriter (already HTML)
```

### Fallback behavior

When a translation is missing (empty `msgstr` in PO file), the generated code **falls back to the source locale**. This is a core feature of MoonBuggy — untranslated strings show the source language text, never crash or produce empty output.

Strictness is enforced externally via `moonbuggy validate --strict` in CI, not by the source generator.

### Compile-time constant requirement

The first argument to `_t()` and `_m()` must be resolvable to a **compile-time constant string**. This includes string literals, `const` variables, and local variables assigned from literals (interned immutable strings):

```csharp
// OK — string literal
_t("Welcome to $name$!", new { name })

// OK — compile-time constant variable
const string greeting = "Welcome to $name$!";
_t(greeting, new { name })

// OK — local variable from literal (interned, immutable)
var key = "Welcome to $name$!";
_t(key, new { name })

// ERROR — runtime-computed string, not statically analyzable
_t(GetMessageKey(), new { name })
```

This ensures every `_t()` / `_m()` call is statically analyzable by both the extractor and the source generator.

### Placeholder metadata for `_m()` (re-derived from source)

For `_m()` messages, the source generator needs to know what `<0>`, `<1>` etc. map to in HTML. This mapping is **re-derived from the source code** — the source generator re-parses the `_m()` call's markdown argument to reconstruct the placeholder-to-HTML mapping. No separate metadata file is needed, since the source generator already requires access to both source code (for interceptor call sites) and PO files (for translations).

---

## Markdown Support (_m)

`_m()` is the markdown variant of `_t()`. The developer writes markdown in the source string; the extractor converts markdown to **indexed placeholders** (`<0>`, `<1>`, etc.) in the PO file. Translators see opaque placeholders and can't break HTML structure.

### Markdown processing

MoonBuggy uses **Markdig** (CommonMark-compliant) for all markdown-to-HTML conversion. The same Markdig pipeline is used by the CLI extractor, the source generator, and the runtime fallback.

Applications that need to customize the Markdig pipeline (e.g. to enable extensions or change output) can configure it at startup via `I18n.MarkdownPipeline`. The CLI and source generator use the default pipeline at build time.

### Markdown examples

Common inline constructs and their HTML output:

| Markdown | HTML output |
|----------|------------|
| `**text**` | `<strong>text</strong>` |
| `*text*` | `<em>text</em>` |
| `[text](url)` | `<a href="url">text</a>` |
| `` `code` `` | `<code>code</code>` |

Any markdown syntax supported by the configured Markdig pipeline can be used in `_m()` messages.

### Placeholder rules

1. Each markdown construct gets a **unique sequential index** across the entire message
2. Indices are assigned in **order of appearance** in the source string
3. Nested markdown uses **nested placeholders**: `**[text](url)**` → `<0><1>text</1></0>`
4. `$var$` in **visible text** → `{var}` (normal variable transform)
5. `$var$` in **attributes** (e.g. inside link URL parens) → hidden from translator, stored in placeholder metadata
6. Inside plural branches, each markdown construct gets its **own index** (no sharing across branches)

### Examples

**Simple bold:**
```csharp
_m("Click **here** to continue")
```
```po
msgid "Click <0>here</0> to continue"
```
`<0>` = `<strong>`. Output: `Click <strong>here</strong> to continue`

**Multiple formatting:**
```csharp
_m("Read **this** and click [here]($url$)", new { url })
```
```po
msgid "Read <0>this</0> and click <1>here</1>"
```
- `<0>` = `<strong>`
- `<1>` = `<a href="{url}">`
- `$url$` is an attribute variable — hidden from translator

Spanish:
```po
msgstr "Lee <0>esto</0> y haz clic <1>aquí</1>"
```

**Nested markdown (bold link):**
```csharp
_m("Click **[here]($url$)** to continue", new { url })
```
```po
msgid "Click <0><1>here</1></0> to continue"
```
- `<0>` = `<strong>`
- `<1>` = `<a href="{url}">`

**Variable in visible text inside formatting:**
```csharp
_m("Hello **$name$**!", new { name })
```
```po
msgid "Hello <0>{name}</0>!"
```
`<0>` = `<strong>`, `{name}` is visible translatable content.

**Markdown inside plural blocks (separate indices per branch):**
```csharp
_m("You have $#x=0#no **new** items|**#x#** new item|**#x#** new items$", new { x })
```
```po
msgid "You have {x, plural, =0 {no <0>new</0> items} one {<1>#</1> new item} other {<2>#</2> new items}}"
```
- `<0>` = `<strong>` (in =0 branch)
- `<1>` = `<strong>` (in one branch)
- `<2>` = `<strong>` (in other branch)
- All are `<strong>` but get separate indices since they're in different plural branches

**Full composed example:**
```csharp
_m("Hi **$name$**, you have $#x=0#no *new* items|*#x#* new item|*#x#* new items$", new { name, x })
```
```po
msgid "Hi <0>{name}</0>, you have {x, plural, =0 {no <1>new</1> items} one {<2>#</2> new item} other {<3>#</3> new items}}"
```

### How translators work with placeholders

Translators preserve `<N>...</N>` markers and translate the text around and inside them:

```po
# English
msgid "Read <0>this</0> and click <1>here</1>"

# Spanish — reordered, text translated, placeholders preserved
msgstr "Haz clic <1>aquí</1> y lee <0>esto</0>"

# Japanese — different word order
msgstr "<0>これ</0>を読んで、<1>ここ</1>をクリックしてください"
```

The library maps each `<N>` back to the correct HTML element when baking in the translations. Translators can reorder placeholders freely — the indices ensure correct mapping regardless of position.

---

## CLI

Installed as a .NET tool:

```bash
dotnet tool install -g moonbuggy
# or as a local tool
dotnet tool install moonbuggy
```

### `moonbuggy extract`

Scans `.cs` and `.cshtml` files for `_t()` / `_m()` calls, transforms MB syntax to ICU MessageFormat, and writes/updates PO files.

```
moonbuggy extract [files...]
    [--clean]           # Remove obsolete messages no longer in source
    [--locale <locale>] # Extract for specific locale(s) only
    [--verbose]         # Show detailed extraction info
    [--watch]           # Watch mode — re-extract on file changes
```

**Extraction transforms:**
- `$var$` → `{var}`
- `#var#` / `#~var#` inside plural blocks → ICU `#` or plural selector
- `$...|...$` plural blocks → `{var, plural, one {...} other {...}}`
- `$...|...|...$` with `=0` → `{var, plural, =0 {...} one {...} other {...}}`
- `_m()` markdown → indexed placeholders (`<0>`, `<1>`, etc.)

**Output (Lingui-style statistics):**
```
> moonbuggy extract

Catalog statistics:
┌──────────┬─────────────┬─────────┐
│ Language │ Total count │ Missing │
├──────────┼─────────────┼─────────┤
│ en       │     42      │    0    │
│ es       │     42      │    3    │
│ ru       │     42      │    7    │
└──────────┴─────────────┴─────────┘
```

**Key constraint:** Must produce **identical `msgid` strings** to what Lingui.js produces for the same logical message — this is critical for shared PO files.

Preserves existing translations when updating PO files. New entries get empty `msgstr`. With `--clean`, entries no longer found in source are removed.

### `moonbuggy validate`

Validates PO files without extracting. Useful in CI pipelines:

```
moonbuggy validate
    [--strict]          # Fail on any missing translations
    [--locale <locale>] # Validate specific locale(s)
```

**Checks:**
- All `msgid` entries have non-empty `msgstr` (completeness)
- Variables in `msgstr` match `msgid` (no missing/extra `{var}`)
- ICU MessageFormat syntax is valid in all entries
- Plural forms match CLDR requirements for the target locale

### No `compile` command

Unlike Lingui.js, MoonBuggy does not need a separate compile step. The source generator reads PO files and generates optimized code automatically during `dotnet build`.

---

## Select / Advanced ICU Features

**Not supported in v1.** `select`, `selectOrdinal`, and ICU number/date formatting are out of scope. If needed in the future, they could be supported via raw ICU syntax in the source string.

---

## Culture / Locale Management

The library is **agnostic** about how the current locale is determined. It provides minimal storage for the current LCID; everything else (detection, middleware, cookies, headers) is the application's responsibility.

### Library API

```csharp
public static class I18n
{
    private static readonly AsyncLocal<I18nContext> _current = new();

    public static I18nContext Current
    {
        get => _current.Value ??= new I18nContext();
        set => _current.Value = value;
    }
}

public class I18nContext
{
    public int LCID { get; set; }
}
```

That's the entire culture API. One `AsyncLocal<I18nContext>` — thread-safe, async-context-safe, zero dependencies. Lazily initialized so reading without setting returns a default context (LCID 0 = source locale).

### Usage pattern (application-level, not part of the library)

```csharp
// Example ASP.NET Core middleware — application code, not library code
app.Use(async (context, next) =>
{
    I18n.Current.LCID = DetermineLocale(context); // application logic
    await next();
});
```

The interceptor-generated code reads `I18n.Current.LCID` to select the right locale branch:
```csharp
// _t("Welcome!") is intercepted and becomes:
__msg_abc123(Output, I18n.Current.LCID);
```

---

## Pseudolocalization

Uses Lingui.js terminology and conventions. Pseudolocalization is a method to verify i18n readiness before sending strings to translators.

### What it detects

- **Hardcoded strings** — any text on the page without accented characters is not going through the translation system
- **Encoding bugs** — accented characters reveal broken Unicode handling

### How it works

Pseudolocalization is controlled by a **compile switch**. When enabled, the source generator auto-generates a pseudo-locale by transforming source language strings. No PO file is needed — it's purely synthetic.

```xml
<!-- .csproj -->
<PropertyGroup>
  <MoonBuggyPseudoLocale>true</MoonBuggyPseudoLocale>
</PropertyGroup>
```

When the switch is **on**, the source generator adds a `pseudo-LOCALE` case to every generated method:

```csharp
// Auto-generated pseudo-locale case
case LCID_PSEUDO:
    writer.Write("Ŵëĺçöḿë ţö ");
    writer.Write(name);  // variables preserved as-is
    writer.Write("!");
    break;
```

When the switch is **off**, no pseudo-locale code is generated — zero overhead in production builds.

### Transforms

1. **Accented characters** — each ASCII letter is replaced with a combining-character variant, making translated strings visually distinct from hardcoded ones
2. **Variable preservation** — `$var$` placeholders and ICU syntax (`{var}`, `{var, plural, ...}`) pass through untransformed
3. **Placeholder preservation** — `<0>`, `<1>` markers in `_m()` messages pass through untransformed
4. **Non-letter preservation** — digits, punctuation, whitespace pass through unchanged
5. **No text expansion** — the pseudo-locale only accents characters, it does not pad strings for length

### Accent mapping

Each letter is combined with a Unicode combining character and normalized:

| Characters | Combining character | Example |
|-----------|-------------------|---------|
| `a`, `u`, `A`, `U` | `\u030A` ring above | å, ů, Å, Ů |
| `e`, `i`, `h`, `o`, `w`, `x`, `y` + uppercase | `\u0308` diaeresis | ë, ï, ḧ, ö, ẅ, ẍ, ÿ |
| `b`, `d`, `f`, `B`, `D`, `F`, `Q` | `\u0307` dot above | ḃ, ḋ, ḟ |
| `v`, `V` | `\u0303` tilde | ṽ, Ṽ |
| `t`, `T` | `\u0327` cedilla | ţ, Ţ |
| All other letters | `\u0301` acute accent | ć, ǵ, ĺ, ḿ, ń, etc. |

The `Combine` operation creates a two-character sequence (letter + combining character) and calls `.Normalize()` to produce the precomposed Unicode form where available.

**LCID:** Default 4096 (0x1000 — `LOCALE_CUSTOM_DEFAULT`), configurable. This Windows-reserved value for custom locales is unlikely to collide with any real locale.

---

## Configuration

Two configuration locations: a **JSON config file** for shared i18n settings (mirrors Lingui.js structure) and **.csproj properties** for build-specific flags.

### `moonbuggy.config.json`

Read by the CLI tool and the source generator. Structure mirrors Lingui.js's `lingui.config.ts` so catalog paths and locales can be kept in sync:

```json
{
  "sourceLocale": "en",
  "locales": ["en", "es", "ru", "ja"],
  "catalogs": [
    {
      "path": "src/locales/{locale}/messages",
      "include": ["**/*.cs", "**/*.cshtml"]
    }
  ]
}
```

| Field | Description |
|-------|-------------|
| `sourceLocale` | The language used in source code strings |
| `locales` | All supported locales (including source) |
| `catalogs[].path` | Path pattern for PO files — `{locale}` is replaced per locale |
| `catalogs[].include` | Glob patterns for files to scan during extraction |

### `.csproj` properties

Build-specific settings that don't need to be shared with Lingui.js:

```xml
<PropertyGroup>
  <MoonBuggyPseudoLocale>true</MoonBuggyPseudoLocale>
</PropertyGroup>

<ItemGroup>
  <PackageReference Include="intelligenthack.MoonBuggy" Version="1.0.0" />
  <PackageReference Include="intelligenthack.MoonBuggy.SourceGenerator" Version="1.0.0"
                    PrivateAssets="all" OutputItemType="Analyzer" />
</ItemGroup>
```

| Property | Default | Description |
|----------|---------|-------------|
| `MoonBuggyPseudoLocale` | `false` | Enable pseudo-locale generation in source generator |

### Corresponding Lingui.js config

The Lingui.js config should use the same catalog path so both extractors write to the same PO files:

```ts
// lingui.config.ts
import { defineConfig } from "@lingui/cli";

export default defineConfig({
  sourceLocale: "en",
  locales: ["en", "es", "ru", "ja"],
  catalogs: [
    {
      path: "src/locales/{locale}/messages",
      include: ["src/**/*.ts", "src/**/*.tsx"],
    },
  ],
});
```

---

## File Structure

Follows the **Lingui.js catalog path convention** so both extractors share the same layout:

```
/src/locales
  /{locale}
    messages.po       # Translation catalog
```

Example:
```
/src/locales
  /en
    messages.po       # English (source language)
  /es
    messages.po       # Spanish translations
  /ru
    messages.po       # Russian translations
```

The exact path is configured in the catalog config (shared with Lingui.js). Source generator reads PO files from these paths at build time — no separate compile output directory needed, as the generated code is emitted directly by the source generator.

---

## References

- [Lingui.js Documentation](https://lingui.dev/)
- [ICU MessageFormat Guide](https://unicode-org.github.io/icu/userguide/format_parse/messages/)
- [Unicode CLDR Plural Rules](https://www.unicode.org/cldr/charts/42/supplemental/language_plural_rules.html)
- [PO File Format (GNU gettext)](https://www.gnu.org/software/gettext/manual/html_node/PO-Files.html)
- [Markdig Markdown Processor](https://github.com/xoofx/markdig)
