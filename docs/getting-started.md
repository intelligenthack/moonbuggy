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

The source generator needs two things in your project file: the analyzer reference and the interceptors namespace.

```xml
<PropertyGroup>
  <!-- Required: allow the source generator to emit interceptors -->
  <InterceptorsNamespaces>$(InterceptorsNamespaces);MoonBuggy.Generated</InterceptorsNamespaces>
</PropertyGroup>

<ItemGroup>
  <PackageReference Include="intelligenthack.MoonBuggy" Version="1.0.0" />
  <PackageReference Include="intelligenthack.MoonBuggy.SourceGenerator" Version="1.0.0"
                    PrivateAssets="all" OutputItemType="Analyzer" />
</ItemGroup>
```

`PrivateAssets="all"` prevents the source generator from leaking into consumers. `OutputItemType="Analyzer"` tells the compiler to load it as an analyzer rather than a runtime dependency.

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

See [Syntax Reference](syntax-reference.md) for the full syntax.

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

```po
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

This fails the build if any `msgstr` is empty, catching untranslated strings before they ship. See [CLI Reference](cli-reference.md) for all flags.

## What to read next

- [Syntax Reference](syntax-reference.md) — variables, plurals, markdown, escaping
- [CLI Reference](cli-reference.md) — `extract` and `validate` commands
- [Configuration](configuration.md) — `moonbuggy.config.json`, MSBuild properties, Lingui.js co-existence
