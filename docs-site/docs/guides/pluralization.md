---
sidebar_position: 2
title: Pluralization
---

# Pluralization

MoonBuggy handles plural forms using CLDR rules for all Unicode locales. You write plural blocks in a concise pipe-separated syntax, and the source generator emits inline arithmetic for each language's plural categories.

## Basic plurals (one | other)

Wrap plural forms in `$...|...$` with `#var#` as the selector:

```csharp
_t("You have $#x# book|#x# books$", new { x })
```

The pipe separates the "one" and "other" forms. `#x#` both selects the form and renders the count in the output. The extractor produces this ICU MessageFormat in the PO file:

```gettext
msgid "You have {x, plural, one {# book} other {# books}}"
msgstr ""
```

At runtime:
- `x = 1` renders "You have 1 book"
- `x = 3` renders "You have 3 books"

### Integer types only

The selector variable must be an integer type: `int`, `long`, `byte`, `short`, and their unsigned variants. Floating-point types (`float`, `double`, `decimal`) produce compiler error **MB0009**. This matches ICU's integer-based plural selection.

## Three forms with zero (=0 | one | other)

Use `#var=0#` to add an explicit zero case:

```csharp
_t("$#x=0#No items|#x# item|#x# items$", new { x })
```

PO output:

```gettext
msgid "{x, plural, =0 {No items} one {# item} other {# items}}"
```

At runtime:
- `x = 0` renders "No items"
- `x = 1` renders "1 item"
- `x = 5` renders "5 items"

The `=0` form is an exact-value match — it takes priority over any CLDR category.

## Hidden selectors

Sometimes you want the plural form but not the number itself. Use `#~var#` to select without rendering:

```csharp
_t("$#~x#one new message|#x# new messages$", new { x })
```

PO output:

```gettext
msgid "{x, plural, one {one new message} other {# new messages}}"
```

At runtime:
- `x = 1` renders "one new message" (no number shown)
- `x = 5` renders "5 new messages"

Combine hidden selector with zero form using `#~var=0#`:

```csharp
_t("$#~x=0#no messages|one new message|#x# new messages$", new { x })
```

## CLDR plural categories

English has two categories: `one` and `other`. But many languages have more. Russian has `one`, `few`, `many`, and `other`. Arabic has `zero`, `one`, `two`, `few`, `many`, and `other`.

You only write the English forms in source code — the translator provides the correct number of forms for their language. For example, a translator working with Russian would write:

```gettext
msgid "You have {x, plural, one {# book} other {# books}}"
msgstr "У вас {x, plural, one {# книга} few {# книги} many {# книг} other {# книг}}"
```

MoonBuggy's source generator reads the CLDR plural rules for each target locale and emits the correct branching logic. The rules are baked in as integer arithmetic — no ICU runtime library is needed.

## Multiple plural blocks

A single message can contain multiple plural blocks:

```csharp
_t("$#x# file|#x# files$ and $#y# folder|#y# folders$", new { x, y })
```

Each block has its own selector variable.

## Variables inside plural branches

Regular `$var$` substitutions work inside plural branches:

```csharp
_t("$#x# book by $author$|#x# books by $author$$", new { x, author })
```

PO output:

```gettext
msgid "{x, plural, one {# book by {author}} other {# books by {author}}}"
```

## Mixing variables and plurals

Variables and plural blocks combine naturally:

```csharp
_t("Hi $name$, you have $#x# book|#x# books$", new { name, x })
```

PO output:

```gettext
msgid "Hi {name}, you have {x, plural, one {# book} other {# books}}"
```

## What translators see

Translators work exclusively with ICU MessageFormat. They never encounter MoonBuggy's `$...|...$` syntax. This means:

- Standard PO editors (Poedit, Crowdin, Weblate) understand the format natively
- Translators can add plural categories required by their language
- The `#` symbol in ICU represents the count value — translators can place it anywhere within a plural branch
