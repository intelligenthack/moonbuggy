---
sidebar_position: 3
title: Markdown Translations
---

# Markdown Translations

`_m()` lets you write translatable strings with markdown formatting. MoonBuggy converts the markdown to indexed placeholders in PO files, so translators can reorder formatting without touching raw HTML.

## Basic usage

```csharp
_m("Click **here** to continue")
```

PO entry:

```gettext
msgid "Click <0>here</0> to continue"
```

Output HTML:

```html
Click <strong>here</strong> to continue
```

The `<0>...</0>` placeholder represents `<strong>`. The translator can move it freely:

```gettext
msgstr "Continuez en cliquant <0>ici</0>"
```

## Links

Markdown links are converted to numbered placeholders:

```csharp
_m("Read **this** and click [here]($url$)", new { url })
```

PO entry:

```gettext
msgid "Read <0>this</0> and click <1>here</1>"
```

- `<0>` maps to `<strong>`
- `<1>` maps to `<a href="{url}">`

The URL variable is hidden from the translator — it's stored in the placeholder metadata. The translator only sees the link text:

```gettext
msgstr "Lea <0>esto</0> y haga clic <1>aquí</1>"
```

## Nested markdown

Bold links or other nested constructs produce nested placeholders:

```csharp
_m("Click **[here]($url$)** to continue", new { url })
```

PO entry:

```gettext
msgid "Click <0><1>here</1></0> to continue"
```

- `<0>` = `<strong>`
- `<1>` = `<a href="{url}">`

## Variables in `_m()`

Regular `$var$` substitutions work in markdown messages:

```csharp
_m("Welcome **$name$** to our site", new { name })
```

PO entry:

```gettext
msgid "Welcome <0>{name}</0> to our site"
```

The variable `{name}` appears inside the bold placeholder. Values are HTML-encoded at runtime.

## Markdown with plurals

Markdown and plural blocks can be combined:

```csharp
_m("$#x=0#no **new** items|**#x#** new item|**#x#** new items$", new { x })
```

PO entry:

```gettext
msgid "{x, plural, =0 {no <0>new</0> items} one {<1>#</1> new item} other {<2>#</2> new items}}"
```

Each plural branch gets its own set of placeholder indices, since the markdown structure can differ between branches.

## Return type

`_m()` returns `TranslatedHtml` which implements `IHtmlContent`. In Razor views, the view engine calls `WriteTo` directly — the pre-rendered HTML segments are written without additional encoding.

Unlike `_t()` which returns `TranslatedString` (with HTML encoding of variable values), `_m()` output is already HTML. The markdown-to-HTML conversion happens at compile time, not at runtime.

## When to use `_m()` vs `_t()`

Use `_t()` for plain text and `_m()` for formatted text:

| Scenario | Function |
|----------|----------|
| Plain message | `_t("Save changes")` |
| Message with variable | `_t("Hello $name$", new { name })` |
| Bold/italic text | `_m("Click **here**")` |
| Links | `_m("Visit [our site]($url$)", new { url })` |
| HTML in message | `_m("Read **this**")` |

Using `_t()` for messages with HTML would require the translator to handle raw HTML tags, which is error-prone. `_m()` abstracts the HTML behind numbered placeholders.
