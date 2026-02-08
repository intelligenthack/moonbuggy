---
sidebar_position: 6
title: Testing Translations
---

# Testing Translations

MoonBuggy provides several ways to verify translations work correctly during development and in CI.

## Setting the locale in tests

MoonBuggy stores the current locale in an `AsyncLocal<I18nContext>`. In unit tests, set it before calling code that uses `_t()` or `_m()`:

```csharp
using MoonBuggy;
using System.Globalization;

[Fact]
public void Spanish_translation_renders_correctly()
{
    I18n.Current = new I18nContext { LCID = CultureInfo.GetCultureInfo("es").LCID };

    // Your code that uses _t() / _m()
    var result = MyService.GetGreeting();

    Assert.Equal("Bienvenido", result.ToString());
}
```

The `AsyncLocal` is isolated per async context, so parallel tests don't interfere with each other.

### Source locale (default)

If you never set `I18n.Current`, the LCID defaults to 0, which is the source locale (English). This means all your existing tests work without any locale setup — they use the English strings from source code.

### LCID values

Use `CultureInfo.GetCultureInfo()` to get the correct LCID. Note that neutral cultures have different LCIDs than specific ones:

| Culture | LCID |
|---------|------|
| `en` (neutral) | 9 |
| `en-US` | 1033 |
| `es` (neutral) | 10 |
| `es-ES` | 3082 |

MoonBuggy uses neutral culture LCIDs in the generated code, since translations are per-language, not per-region.

## CI validation with `moonbuggy validate`

Add validation to your CI pipeline to catch translation issues before they ship:

```bash
moonbuggy validate --strict
```

### What `--strict` checks

1. **Completeness** — every `msgid` has a non-empty `msgstr`. Empty translations fail the build.
2. **Variable consistency** — variables in `msgstr` match those in `msgid`. No missing or extra `{var}` placeholders.
3. **ICU validity** — `msgstr` contains valid ICU MessageFormat syntax.
4. **Plural forms** — plural categories in `msgstr` match CLDR requirements for the target locale.

### CI configuration example

```yaml
# GitHub Actions
- name: Validate translations
  run: moonbuggy validate --strict
```

Without `--strict`, missing translations produce warnings but don't fail the build. Use `--strict` in CI and non-strict during development.

### Validating specific locales

```bash
# Only validate Spanish
moonbuggy validate --locale es --strict

# Multiple locales
moonbuggy validate --locale es --locale ru --strict
```

## Compiler diagnostics

The MoonBuggy diagnostic analyzer runs in real time in your IDE, catching errors before you even build:

| ID | Severity | What it catches |
|----|----------|-----------------|
| MB0001 | Error | Non-constant first argument |
| MB0002 | Error | Variable in message with no matching arg property |
| MB0003 | Warning | Arg property with no matching variable in message |
| MB0005 | Error | Malformed MoonBuggy syntax |
| MB0007 | Error | Empty message string |
| MB0008 | Error | Non-constant context string |
| MB0009 | Error | Plural selector is not an integer type |

These diagnostics appear as squiggly underlines in Visual Studio, VS Code, and Rider. They're reported during the normal build too, so `dotnet build` with `TreatWarningsAsErrors` catches them in CI.

## Pseudolocalization for visual testing

Enable [pseudolocalization](pseudolocalization.md) to visually spot hardcoded strings:

```xml
<MoonBuggyPseudoLocale>true</MoonBuggyPseudoLocale>
```

Then activate it at runtime with LCID 4096. Any text that appears in normal ASCII characters hasn't gone through the translation system.

## Integration test strategy

For integration tests that exercise the full rendering pipeline (e.g., Razor views via `WebApplicationFactory`):

```csharp
[Fact]
public async Task Page_renders_in_Spanish()
{
    await using var app = new WebApplicationFactory<Program>();
    var client = app.CreateClient();

    // Set locale via your middleware's detection mechanism
    client.DefaultRequestHeaders.Add("Accept-Language", "es");

    var response = await client.GetStringAsync("/");
    Assert.Contains("Bienvenido", response);
}
```

The exact mechanism depends on how your middleware detects the locale (cookies, headers, route segments, etc.).
