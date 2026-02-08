---
sidebar_position: 2
title: Configuration
---

# Configuration

MoonBuggy is configured through two mechanisms: a JSON config file read by the CLI and source generator, and MSBuild properties in your `.csproj` for build-time flags.

## moonbuggy.config.json

Place this file in your project root (next to the `.csproj`). Both the CLI tool and the source generator read it.

```json
{
  "sourceLocale": "en",
  "locales": ["en", "es", "ru", "ja"],
  "catalogs": [
    {
      "path": "locales/{locale}/messages",
      "include": ["**/*.cs", "**/*.cshtml"]
    }
  ]
}
```

### Fields

| Field | Required | Description |
|-------|----------|-------------|
| `sourceLocale` | Yes | The language used in source code strings. |
| `locales` | Yes | All supported locales, including the source locale. |
| `catalogs` | Yes | Array of catalog definitions (usually one). |
| `catalogs[].path` | Yes | Path pattern for PO files. `{locale}` is replaced per locale. The `.po` extension is appended automatically. |
| `catalogs[].include` | Yes | Glob patterns for files to scan during extraction. |

### PO file layout

Given the config above, PO files are created at:

```
locales/
  en/
    messages.po
  es/
    messages.po
  ru/
    messages.po
  ja/
    messages.po
```

## MSBuild properties

Set these in your `.csproj` `<PropertyGroup>`:

### InterceptorsNamespaces (required)

```xml
<InterceptorsNamespaces>$(InterceptorsNamespaces);MoonBuggy.Generated</InterceptorsNamespaces>
```

This enables the compiler to accept interceptor methods emitted by the source generator. Without it, the build fails with CS9137.

### MoonBuggyPseudoLocale

```xml
<MoonBuggyPseudoLocale>true</MoonBuggyPseudoLocale>
```

When enabled, the source generator adds a pseudo-locale to every generated interceptor. The pseudo-locale replaces ASCII letters with accented equivalents (e.g., "Hello" becomes "Ḧëĺĺö"), making it easy to spot hardcoded strings that are not going through the translation system.

The pseudo-locale uses LCID 4096 (0x1000, `LOCALE_CUSTOM_DEFAULT`). To activate it at runtime, set `I18n.Current = new I18nContext { LCID = 4096 }`.

When the switch is off (the default), no pseudo-locale code is generated — zero overhead in production builds.

## Package references

```xml
<ItemGroup>
  <PackageReference Include="intelligenthack.MoonBuggy" Version="1.0.0" />
  <PackageReference Include="intelligenthack.MoonBuggy.SourceGenerator" Version="1.0.0"
                    PrivateAssets="all" OutputItemType="Analyzer" />
</ItemGroup>
```

- `PrivateAssets="all"` prevents the source generator from becoming a transitive dependency of consumers.
- `OutputItemType="Analyzer"` tells the compiler to load it as an analyzer/generator rather than a runtime reference.

## Locale management

MoonBuggy provides `I18n.Current` (an `AsyncLocal<I18nContext>`) for per-request locale state. Your application sets it; MoonBuggy reads it. The library does not provide middleware, cookie handling, or header parsing — that is application-level logic.

```csharp
using MoonBuggy;

// Middleware example
app.Use(async (context, next) =>
{
    var culture = CultureInfo.GetCultureInfo("es");
    I18n.Current = new I18nContext { LCID = culture.LCID };
    await next();
});
```

If `I18n.Current` is never set, the LCID defaults to 0 (source locale). This means the application works without any locale middleware during development and in tests.

## Lingui.js co-existence

MoonBuggy and Lingui.js can share the same PO files. Both use ICU MessageFormat as the `msgid` key, so entries from either extractor coexist in one catalog.

Point both configs at the same catalog path:

**moonbuggy.config.json:**
```json
{
  "sourceLocale": "en",
  "locales": ["en", "es"],
  "catalogs": [
    {
      "path": "src/locales/{locale}/messages",
      "include": ["**/*.cs", "**/*.cshtml"]
    }
  ]
}
```

**lingui.config.ts:**
```ts
import { defineConfig } from "@lingui/cli";

export default defineConfig({
  sourceLocale: "en",
  locales: ["en", "es"],
  catalogs: [
    {
      path: "src/locales/{locale}/messages",
      include: ["src/**/*.ts", "src/**/*.tsx"],
    },
  ],
});
```

Run both extractors independently:

```bash
moonbuggy extract    # C# strings
npx lingui extract   # JavaScript strings
```

Both update the same `.po` files. Entries are keyed by `msgid`, so if both extractors produce the same message (e.g., "Save changes"), it appears once with one translation.
