---
sidebar_position: 1
title: Getting Started
---

# Getting Started

This guide walks through adding MoonBuggy to an ASP.NET Core project from scratch.

## Prerequisites

- .NET 8 SDK or later
- An ASP.NET Core application (Razor Pages, MVC, or Minimal APIs)

## 1. Install packages

```bash
# Runtime library + source generator
dotnet add package intelligenthack.MoonBuggy
dotnet add package intelligenthack.MoonBuggy.SourceGenerator

# CLI tool (global or local)
dotnet tool install intelligenthack.MoonBuggy.Cli
```

## 2. Configure your .csproj

Add the MoonBuggy packages:

```xml
<ItemGroup>
  <PackageReference Include="intelligenthack.MoonBuggy" Version="1.0.0" />
  <PackageReference Include="intelligenthack.MoonBuggy.SourceGenerator" Version="1.0.0"
                    PrivateAssets="all" OutputItemType="Analyzer" />
</ItemGroup>
```

`PrivateAssets="all"` prevents the source generator from leaking into consumers. `OutputItemType="Analyzer"` tells the compiler to load it as an analyzer rather than a runtime dependency.

The source generator package automatically configures the compiler interceptors feature flag, registers PO files for compilation, and provides the required `InterceptsLocationAttribute` polyfill. No manual property setup is needed.

### Razor Pages / MVC projects

If you use `_t()` or `_m()` in `.cshtml` files, add this property:

```xml
<PropertyGroup>
  <UseRazorSourceGenerator>false</UseRazorSourceGenerator>
</PropertyGroup>
```

This switches to the legacy Razor compilation pipeline, which emits `.cshtml.g.cs` files as a pre-build MSBuild step. This makes `_t()`/`_m()` calls in Razor views visible to the MoonBuggy source generator. The modern Razor source generator runs in the same compilation pass as MoonBuggy's generator, so they can't see each other's output.

This is not needed if you only call `_t()`/`_m()` from `.cs` files (e.g., Minimal APIs, background services).

## 3. Create moonbuggy.config.json

Place this in your project root (next to the `.csproj`):

```json
{
  "sourceLocale": "en",
  "locales": ["en", "es"],
  "catalogs": [
    {
      "path": "locales/{locale}/messages",
      "include": ["**/*.cs", "**/*.cshtml"]
    }
  ]
}
```

- `sourceLocale` — the language you write in source code.
- `locales` — all languages you support, including the source.
- `catalogs[].path` — where PO files go. `{locale}` is replaced per language.
- `catalogs[].include` — globs for files the extractor scans.

## 4. Add the import to your views

In `_ViewImports.cshtml`, add:

```cshtml
@using static MoonBuggy.Translate
```

This makes `_t()` and `_m()` available as bare function calls in every Razor view.

For use outside views (services, controllers, middleware), add the same `using static` at the top of the C# file.

## 5. Write your first translatable string

```cshtml
<h1>@_t("Welcome to $name$!", new { name = Model.SiteName })</h1>
<p>@_t("You have $#count# item|#count# items$ in your cart", new { count = Model.ItemCount })</p>
```

- `$name$` — variable substitution. The extractor converts this to ICU `{name}` in PO files.
- `$#count# item|#count# items$` — plural block. The pipe separates the "one" and "other" forms. `#count#` renders the number.

See [Syntax Reference](guides/syntax-reference.md) for the full syntax.

## 6. Extract strings

```bash
moonbuggy extract
```

This scans your source files, converts MoonBuggy syntax to ICU MessageFormat, and writes PO files:

```
Catalog statistics:
┌──────────┬─────────────┬─────────┐
│ Language │ Total count │ Missing │
├──────────┼─────────────┼─────────┤
│ en       │      2      │    0    │
│ es       │      2      │    2    │
└──────────┴─────────────┴─────────┘
```

Your `locales/en/messages.po` and `locales/es/messages.po` files now exist with the extracted entries.

## 7. Translate

Open `locales/es/messages.po` in any PO editor (Poedit, Crowdin, or a text editor) and fill in translations:

```gettext
msgid "Welcome to {name}!"
msgstr "Bienvenido a {name}!"

msgid "You have {count, plural, one {# item} other {# items}} in your cart"
msgstr "Tienes {count, plural, one {# artículo} other {# artículos}} en tu carrito"
```

Translators work with standard ICU MessageFormat — they never see the `$var$` syntax.

## 8. Build

```bash
dotnet build
```

The source generator reads your PO files and generates interceptor methods for every `_t()` and `_m()` call site. Each interceptor contains a locale switch that writes the correct translation directly. No dictionary lookups, no string allocations.

## 9. Set the locale at runtime

MoonBuggy stores the current locale in an `AsyncLocal`, so it's per-request and async-safe. Add middleware to set it:

```csharp
using System.Globalization;
using MoonBuggy;

app.Use(async (context, next) =>
{
    // Your logic: cookie, Accept-Language header, route segment, etc.
    var culture = CultureInfo.GetCultureInfo("es");
    I18n.Current = new I18nContext { LCID = culture.LCID };
    await next();
});
```

If you never set `I18n.Current`, MoonBuggy uses LCID 0 (the source locale). Everything works in development without any middleware.

## 10. Verify in CI

```bash
moonbuggy validate --strict
```

This fails the build if any `msgstr` is empty, catching untranslated strings before they ship. See [CLI Reference](api/cli-reference.md) for all flags.

## What to read next

- [Syntax Reference](guides/syntax-reference.md) — variables, plurals, markdown, escaping
- [CLI Reference](api/cli-reference.md) — `extract` and `validate` commands
- [Configuration](api/configuration.md) — `moonbuggy.config.json`, MSBuild properties, Lingui.js co-existence
