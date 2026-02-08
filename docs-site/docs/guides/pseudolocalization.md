---
sidebar_position: 4
title: Pseudolocalization
---

# Pseudolocalization

Pseudolocalization replaces ASCII letters with accented equivalents, making it easy to spot hardcoded strings that bypass the translation system. MoonBuggy implements this as a compile-time switch with zero runtime overhead when disabled.

## Enabling pseudolocalization

Add the `MoonBuggyPseudoLocale` property to your `.csproj`:

```xml
<PropertyGroup>
  <MoonBuggyPseudoLocale>true</MoonBuggyPseudoLocale>
</PropertyGroup>
```

When enabled, the source generator adds a pseudo-locale branch to every generated interceptor. For example, the string "Hello world" becomes "Ḧëĺĺö ẅöŕĺḋ".

## Activating at runtime

The pseudo-locale uses LCID 4096 (`0x1000`, which is `LOCALE_CUSTOM_DEFAULT`). Set it in your middleware:

```csharp
using MoonBuggy;

app.Use(async (context, next) =>
{
    // Enable pseudo-locale for testing
    I18n.Current = new I18nContext { LCID = 4096 };
    await next();
});
```

You might gate this behind a query parameter or cookie during development:

```csharp
app.Use(async (context, next) =>
{
    if (context.Request.Query.ContainsKey("pseudo"))
    {
        I18n.Current = new I18nContext { LCID = 4096 };
    }
    await next();
});
```

Then visit `https://localhost:5001/?pseudo` to see the pseudo-locale in action.

## What it catches

Pseudolocalization helps you find:

- **Hardcoded strings** — any text that appears in normal ASCII characters hasn't gone through `_t()` or `_m()`.
- **Layout issues** — accented characters are often wider, revealing truncation or overflow problems that would appear with real translations.
- **Concatenation bugs** — if translated and non-translated strings are concatenated, the pseudo-locale makes the boundary obvious.

## Zero overhead in production

When `MoonBuggyPseudoLocale` is `false` (the default), no pseudo-locale code is generated at all. The generated interceptors contain only your configured locale branches. There is no runtime cost — the switch is purely a compile-time concern.

This means you can:
- Enable it in `Debug` configuration only using MSBuild conditions
- Enable it in a dedicated build configuration

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Debug'">
  <MoonBuggyPseudoLocale>true</MoonBuggyPseudoLocale>
</PropertyGroup>
```

## Character mapping

The pseudo-locale transforms each ASCII letter to a visually similar accented character. Some examples:

| Original | Pseudo |
|----------|--------|
| a | ä |
| e | ë |
| H | Ḧ |
| l | ĺ |
| o | ö |
| w | ẅ |

Non-letter characters (numbers, punctuation, whitespace) are preserved unchanged. ICU placeholders like `{name}` and HTML tags are not transformed — only the translatable text content is modified.
