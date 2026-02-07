# Syntax Reference

MoonBuggy uses a custom variable syntax in C# source code. The CLI extractor transforms it to standard ICU MessageFormat when writing PO files. Translators only see ICU syntax.

## Variables

`$var$` marks a substitution point. The property name must match a property on the anonymous `args` object.

```csharp
_t("Welcome to $name$!", new { name = site.Name })
// PO msgid: "Welcome to {name}!"
```

```csharp
_t("Page $num$ of $total$", new { num = currentPage, total = pageCount })
// PO msgid: "Page {num} of {total}"
```

Messages without variables need no `args`:

```csharp
_t("Save changes")
// PO msgid: "Save changes"
```

## Plurals

Wrap plural forms in `$...|...$`. The pipe separates forms. The first `#var#` or `#~var#` inside the block identifies the selector variable.

### Two forms (one | other)

```csharp
_t("You have $#x# book|#x# books$", new { x })
// PO: "You have {x, plural, one {# book} other {# books}}"
// x=1 → "You have 1 book"
// x=3 → "You have 3 books"
```

`#x#` renders the count and selects the plural form. The selector variable must be an integer type (`int`, `long`, `byte`, `short`, and their unsigned variants). Floating-point types produce compiler error MB0009.

### Three forms with zero (=0 | one | other)

Use `#var=0#` to add an explicit zero case:

```csharp
_t("$#x=0#No items|#x# item|#x# items$", new { x })
// PO: "{x, plural, =0 {No items} one {# item} other {# items}}"
// x=0 → "No items"
// x=1 → "1 item"
// x=5 → "5 items"
```

### Hidden selectors

`#~var#` selects the form but does not render the count:

```csharp
_t("$#~x#one new message|#x# new messages$", new { x })
// PO: "{x, plural, one {one new message} other {# new messages}}"
// x=1 → "one new message"
// x=5 → "5 new messages"
```

`#~var=0#` combines hidden selector with zero form:

```csharp
_t("$#~x=0#no messages|one new message|#x# new messages$", new { x })
// PO: "{x, plural, =0 {no messages} one {one new message} other {# new messages}}"
```

### Mixing variables and plurals

Variables and plural blocks can appear in the same message:

```csharp
_t("Hi $name$, you have $#x# book|#x# books$", new { name, x })
// PO: "Hi {name}, you have {x, plural, one {# book} other {# books}}"
```

Multiple plural blocks in one message are also supported:

```csharp
_t("$#x# file|#x# files$ and $#y# folder|#y# folders$", new { x, y })
```

### Variables inside plural branches

Regular `$var$` substitutions work inside plural branches:

```csharp
_t("$#x# book by $author$|#x# books by $author$$", new { x, author })
// PO: "{x, plural, one {# book by {author}} other {# books by {author}}}"
```

## Markdown (`_m`)

`_m()` processes markdown and converts formatting to indexed placeholders in PO files. Translators can reorder placeholders freely without touching HTML.

```csharp
_m("Click **here** to continue")
// PO: "Click <0>here</0> to continue"
// Output: Click <strong>here</strong> to continue
```

```csharp
_m("Read **this** and click [here]($url$)", new { url })
// PO: "Read <0>this</0> and click <1>here</1>"
// <0> = <strong>, <1> = <a href="{url}">
```

Nested markdown:

```csharp
_m("Click **[here]($url$)** to continue", new { url })
// PO: "Click <0><1>here</1></0> to continue"
// <0> = <strong>, <1> = <a href="{url}">
```

Variables in link URLs become hidden from translators (stored in placeholder metadata):

```csharp
_m("Visit [our site]($url$)", new { url })
// PO: "Visit <0>our site</0>"
// Translator sees only the link text, not the URL
```

Markdown inside plural blocks: each branch gets its own placeholder indices.

```csharp
_m("$#x=0#no **new** items|**#x#** new item|**#x#** new items$", new { x })
// PO: "{x, plural, =0 {no <0>new</0> items} one {<1>#</1> new item} other {<2>#</2> new items}}"
```

## Context

When the same English text requires different translations, use the `context` parameter:

```csharp
_t("Submit", context: "button")
_t("Submit", context: "form-label")
```

These produce separate PO entries with `msgctxt`:

```po
msgctxt "button"
msgid "Submit"
msgstr "Enviar"

msgctxt "form-label"
msgid "Submit"
msgstr "Presentar"
```

Context works with `_m()` too:

```csharp
_m("Click **here**", context: "navigation")
```

The `context` value must be a compile-time constant string.

## Escaping

| Sequence | Where | Produces |
|----------|-------|----------|
| `$$` | Anywhere | Literal `$` |
| `##` | Inside plural blocks | Literal `#` |
| `\|\|` | Inside plural blocks | Literal `\|` |

```csharp
_t("Price: $$9.99")              // → "Price: $9.99"
_t("$$marco$$ was here")         // → "$marco$ was here"
```

Outside plural blocks, `#` has no special meaning and needs no escaping.

## Return types

- `_t()` returns `TranslatedString` — a readonly struct implementing `IHtmlContent`. In Razor, the view engine calls `WriteTo` directly (zero allocation). Variable values are HTML-encoded; developer-authored literals are not. Has implicit conversion to `string` for use in C# code.

- `_m()` returns `TranslatedHtml` (via `IHtmlContent`) — pre-rendered HTML. `WriteTo` writes all segments without encoding.

## Compiler diagnostics

The source generator and analyzer report these diagnostics in real time:

| ID | Severity | Meaning |
|----|----------|---------|
| MB0001 | Error | First argument is not a compile-time constant |
| MB0002 | Error | Variable in message has no matching arg property |
| MB0003 | Warning | Arg property has no matching variable in message |
| MB0004 | Warning | PO file not found for configured locale |
| MB0005 | Error | Malformed MoonBuggy syntax |
| MB0006 | Warning | Markdown produced unexpected HTML |
| MB0007 | Error | Empty message string |
| MB0008 | Error | Context is not a compile-time constant |
| MB0009 | Error | Plural selector is not an integer type |
