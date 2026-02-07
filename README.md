# MoonBuggy

[![CI](https://github.com/intelligenthack/moonbuggy/actions/workflows/ci.yml/badge.svg)](https://github.com/intelligenthack/moonbuggy/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/intelligenthack.MoonBuggy.svg)](https://www.nuget.org/packages/intelligenthack.MoonBuggy)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

<p align="center">
  <img src="docs/moonbuggy-wordmark.svg" alt="MoonBuggy" width="480">
</p>

MoonBuggy performs compile-time translation for .NET applications. You write Razor views in English using a lightweight translation syntax, send PO files to translators, and during the build the compiler embeds every translated string directly into the binary. There are no resource files, dictionary lookups, or per-request allocations involved. The application writes the correct language directly to the output stream.

If your stack includes both Razor views and a JavaScript frontend, MoonBuggy can share PO files with Lingui.js. This allows you to maintain a single set of translation files for both server and client.

## Quick Start

```bash
dotnet add package intelligenthack.MoonBuggy
dotnet add package intelligenthack.MoonBuggy.SourceGenerator
dotnet tool install intelligenthack.MoonBuggy.Cli
```

Create `moonbuggy.config.json`, write `_t()` calls in your views, run `moonbuggy extract`, translate the PO files, and build. The source generator package handles all compiler configuration automatically. See [Getting Started](docs/getting-started.md) for the full walkthrough.

> **Razor Pages / MVC:** If you use `_t()` or `_m()` in `.cshtml` files, add `<UseRazorSourceGenerator>false</UseRazorSourceGenerator>` to your `.csproj`. This switches to the legacy Razor compilation pipeline, which makes Razor call sites visible to the MoonBuggy source generator. See the [technical explanation](docs/getting-started.md#razor-pages--mvc-projects) for details.

## What it looks like

In Razor views, you wrap translatable text with `_t()` for plain strings:

```html
@* _ViewImports.cshtml *@
@using static MoonBuggy.Translate

@* In any view *@
<h1>@_t("Welcome to $name$!", new { name = Model.SiteName })</h1>
<p>@_t("You have $#x# new message|#x# new messages$", new { x = Model.MessageCount })</p>
```

The `$name$` token marks a variable placeholder, and the `$...|...$` construct expresses plural forms. The pipe character separates the “one” and “other” variants, and `#x#` represents the count. The syntax is intentionally distinct from C# string interpolation, so there is no ambiguity between translation variables and regular code.

For strings that require formatting, `_m()` allows you to write markdown while keeping translators away from raw HTML:

```html
<p>@_m("Read **this** and click [here]($url$)", new { url = Model.HelpUrl })</p>
```

The same APIs are also available from regular C# code, not only in Razor views:

```csharp
using static MoonBuggy.Translate;

var subject = _t("You have $#x# new notification|#x# new notifications$", new { x = count });
```

## The extract / translate / build loop

Most internationalization libraries resolve translations at runtime. They load resource files, perform key lookups, and format strings for each request. MoonBuggy uses a different approach: it resolves translations once at compile time, and the resulting binary performs only direct string writes.

The workflow consists of the following steps.

**1. Write views in the source language (English).** Use `_t()` for plain text and `_m()` for markdown, and use `$var$` to denote variables.

**2. Extract strings using the CLI.**

```bash
moonbuggy extract
```

The extract command scans your `.cs` and `.cshtml` files, finds every `_t()` and `_m()` call, transforms the MoonBuggy syntax into standard ICU MessageFormat, and writes PO files, one per locale. For example, this view code:

```html
<p>@_t("You have $#x# item|#x# items$ in your cart")</p>
```

produces this PO entry:

```po
msgid "You have {x, plural, one {# item} other {# items}} in your cart"
msgstr ""
```

The `msgstr` field is empty. That is where the translation goes.

**3. Translators fill in the PO files.** PO is a widely supported format with mature tooling (Poedit, Crowdin, Transifex, Weblate, and others). Translators work with standard ICU syntax rather than your source code:

```po
# Spanish
msgid "You have {x, plural, one {# item} other {# items}} in your cart"
msgstr "Tienes {x, plural, one {# artículo} other {# artículos}} en tu carrito"

# Russian (the translator adds plural forms required by the language)
msgid "You have {x, plural, one {# item} other {# items}} in your cart"
msgstr "В корзине {x, plural, one {# товар} few {# товара} many {# товаров} other {# товаров}}"
```

**4. Build the application.** During `dotnet build`, a Roslyn source generator reads the PO files and generates a C# interceptor for every `_t()` and `_m()` call site. Each interceptor contains a locale switch that writes strings directly to the output, with no lookups, formatting, or allocations:

```csharp
// Conceptual output of the source generator (you never see this)
switch (lcid) {
    case 10: // Spanish
        writer.Write("Tienes ");
        if (x == 1) { writer.Write("1 artículo"); }
        else { writer.Write(x); writer.Write(" artículos"); }
        writer.Write(" en tu carrito");
        break;
    default: // English
        writer.Write("You have ");
        if (x == 1) { writer.Write("1 item"); }
        else { writer.Write(x); writer.Write(" items"); }
        writer.Write(" in your cart");
        break;
}
```

CLDR plural rules for each language are baked in as inline arithmetic, so no ICU runtime library is needed.

**5. Repeat as text changes.** Run `moonbuggy extract` again after adding or modifying strings. The extract step is non-destructive: it merges new strings into existing PO files without altering translations that are already present. Use `--clean` to remove entries for strings that have been deleted from source. In CI, `moonbuggy validate --strict` catches untranslated strings before they ship.

## Installation

```bash
dotnet add package intelligenthack.MoonBuggy
dotnet add package intelligenthack.MoonBuggy.SourceGenerator
dotnet tool install intelligenthack.MoonBuggy.Cli
```

That's it. The source generator NuGet package auto-configures the compiler interceptors feature flag, registers PO files for compilation, and provides all required polyfills. No manual `.csproj` property setup is needed.

For Razor Pages or MVC projects that use `_t()`/`_m()` in `.cshtml` files, add one property:

```xml
<PropertyGroup>
  <UseRazorSourceGenerator>false</UseRazorSourceGenerator>
</PropertyGroup>
```

This is not needed if you only call `_t()`/`_m()` from `.cs` files.

## Setting the locale

MoonBuggy does not dictate how you determine the user's language. Your application sets `I18n.Current.LCID` for each request using whatever detection logic you prefer:

```csharp
app.Use(async (context, next) =>
{
    I18n.Current.LCID = DetermineLocale(context); // your logic
    await next();
});
```

If you do not set it, MoonBuggy uses the source locale (English). This means everything works during development and in tests without any locale middleware. You only need it when you are ready to serve multiple languages.

## Configuration

Create a `moonbuggy.config.json` file in your project root:

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

If you also use Lingui.js, point both configurations at the same `path`. The extractors share PO files because they both use ICU MessageFormat as the `msgid` key.

## Syntax reference

### Variables

`$var$` marks a variable. The extractor transforms it to ICU `{var}` in PO files.

```html
<p>@_t("Welcome to $name$!", new { name = Model.SiteName })</p>
@* PO msgid: "Welcome to {name}!" *@
```

Use `$$` for a literal dollar sign: `_t("Price: $$9.99")` outputs "Price: $9.99".

### Plurals

Wrap plural forms in `$...|...$` with `#var#` as the selector:

```html
@* Two forms: one | other *@
<p>@_t("$#x# book|#x# books$", new { x = Model.Count })</p>

@* Three forms with an explicit zero *@
<p>@_t("$#x=0#No items|#x# item|#x# items$", new { x = Model.Count })</p>

@* Hidden count (the selector chooses the form but the number is not rendered) *@
<p>@_t("$#~x#one new message|new messages$", new { x = Model.Count })</p>
```

### Markdown

`_m()` converts markdown to HTML. Translators see numbered `<0>`, `<1>` placeholders that they can reorder freely:

```html
<p>@_m("Click **[here]($url$)** to continue", new { url = Model.NextUrl })</p>

@* PO msgid: "Click <0><1>here</1></0> to continue" *@
@* A translator can reorder: "<0><1>ici</1></0>, cliquez pour continuer" *@
```

### Context

When the same English text needs different translations depending on where it appears, use the `context` parameter:

```html
<button>@_t("Submit", context: "button")</button>
<label>@_t("Submit", context: "form-label")</label>
```

These become separate PO entries with different `msgctxt` values, so translators can provide distinct translations for each.

## Documentation

- [Getting Started](docs/getting-started.md) — step-by-step setup for a new project
- [Syntax Reference](docs/syntax-reference.md) — variables, plurals, markdown, escaping
- [CLI Reference](docs/cli-reference.md) — `extract` and `validate` commands
- [Configuration](docs/configuration.md) — config file, MSBuild properties, Lingui.js co-existence

## License

[MIT](LICENSE)
