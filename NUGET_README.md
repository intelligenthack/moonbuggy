# MoonBuggy

![MoonBuggy](https://raw.githubusercontent.com/intelligenthack/moonbuggy/main/docs/moonbuggy-wordmark.svg)

MoonBuggy performs compile-time translation for .NET applications. You write Razor views in English using a lightweight translation syntax, send PO files to translators, and during the build the compiler embeds every translated string directly into the binary. There are no resource files, dictionary lookups, or per-request allocations involved. The application writes the correct language directly to the output stream.

If your stack includes both Razor views and a JavaScript frontend, MoonBuggy can share PO files with Lingui.js. This allows you to maintain a single set of translation files for both server and client.

## Quick Start

```bash
dotnet add package intelligenthack.MoonBuggy
dotnet add package intelligenthack.MoonBuggy.SourceGenerator
dotnet tool install intelligenthack.MoonBuggy.Cli
```

Create `moonbuggy.config.json`, write `_t()` calls in your views, run `moonbuggy extract`, translate the PO files, and build. The source generator package handles all compiler configuration automatically. See [Getting Started](https://intelligenthack.github.io/moonbuggy/docs/getting-started) for the full walkthrough.

> **Razor Pages / MVC:** If you use `_t()` or `_m()` in `.cshtml` files, add `<UseRazorSourceGenerator>false</UseRazorSourceGenerator>` to your `.csproj`. This switches to the legacy Razor compilation pipeline, which makes Razor call sites visible to the MoonBuggy source generator.

## What it looks like

In Razor views, you wrap translatable text with `_t()` for plain strings:

```csharp
// _ViewImports.cshtml
@using static MoonBuggy.Translate

// In any view
<h1>@_t("Welcome to $name$!", new { name = Model.SiteName })</h1>
<p>@_t("You have $#x# new message|#x# new messages$", new { x = Model.MessageCount })</p>
```

The `$name$` token marks a variable placeholder, and the `$...|...$` construct expresses plural forms. The pipe character separates the "one" and "other" variants, and `#x#` represents the count.

For strings that require formatting, `_m()` allows you to write markdown while keeping translators away from raw HTML:

```csharp
<p>@_m("Read **this** and click [here]($url$)", new { url = Model.HelpUrl })</p>
```

## The extract / translate / build loop

**1. Write views in the source language (English).** Use `_t()` for plain text and `_m()` for markdown.

**2. Extract strings using the CLI.**

```bash
moonbuggy extract
```

The extract command scans your source files, transforms the MoonBuggy syntax into standard ICU MessageFormat, and writes PO files.

**3. Translators fill in the PO files.** PO is a widely supported format with mature tooling (Poedit, Crowdin, Transifex, Weblate).

**4. Build the application.** During `dotnet build`, the source generator reads PO files and generates a C# interceptor for every call site. Each interceptor writes strings directly to the output — no lookups, no allocations:

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

**5. Repeat as text changes.** Run `moonbuggy extract` again after modifying strings. The extract step is non-destructive. Use `--clean` to remove deleted entries. In CI, `moonbuggy validate --strict` catches untranslated strings before they ship.

## Documentation

- [Getting Started](https://intelligenthack.github.io/moonbuggy/docs/getting-started) — step-by-step setup for a new project
- [Syntax Reference](https://intelligenthack.github.io/moonbuggy/docs/guides/syntax-reference) — variables, plurals, markdown, escaping
- [CLI Reference](https://intelligenthack.github.io/moonbuggy/docs/api/cli-reference) — `extract` and `validate` commands
- [Configuration](https://intelligenthack.github.io/moonbuggy/docs/api/configuration) — config file, MSBuild properties, Lingui.js co-existence

## License

[MIT](https://github.com/intelligenthack/moonbuggy/blob/main/LICENSE)
