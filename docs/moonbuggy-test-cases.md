# MoonBuggy - Test Cases

Derived from [moonbuggy-spec.md](moonbuggy-spec.md). Only unambiguous cases are included here. See [Ambiguities and Edge Cases](#ambiguities-and-edge-cases) at the bottom for open questions.

---

## 1. Extraction: Variable Syntax (`_t`)

Tests for the extractor transforming MB source syntax to ICU MessageFormat in PO files.

### 1.1 Simple variable substitution

| # | Source | Expected `msgid` |
|---|--------|-------------------|
| 1.1.1 | `_t("Welcome to $name$!", new { name })` | `Welcome to {name}!` |
| 1.1.2 | `_t("Page $num$ of $total$", new { num, total })` | `Page {num} of {total}` |
| 1.1.3 | `_t("Save changes")` | `Save changes` |

### 1.2 Two-form plurals (one | other)

| # | Source | Expected `msgid` |
|---|--------|-------------------|
| 1.2.1 | `_t("You have $#x# book\|#x# books$", new { x })` | `You have {x, plural, one {# book} other {# books}}` |
| 1.2.2 | `_t("$#~y#just one apple\|many apples (#y#)!$", new { y })` | `{y, plural, one {just one apple} other {many apples (#)!}}` |

### 1.3 Three-form plurals (=0 | one | other)

| # | Source | Expected `msgid` |
|---|--------|-------------------|
| 1.3.1 | `_t("You have $#x=0#no books\|#x# book\|#x# books$", new { x })` | `You have {x, plural, =0 {no books} one {# book} other {# books}}` |
| 1.3.2 | `_t("$#~x=0#no messages\|one new message\|#x# new messages$", new { x })` | `{x, plural, =0 {no messages} one {one new message} other {# new messages}}` |

### 1.4 Composed (variables + multiple plurals)

| # | Source | Expected `msgid` |
|---|--------|-------------------|
| 1.4.1 | `_t("Hi $name$, you have $#x# book\|#x# books$ and $#~y#just one apple\|many apples (#y#)!$", new { name, x, y })` | `Hi {name}, you have {x, plural, one {# book} other {# books}} and {y, plural, one {just one apple} other {many apples (#)!}}` |

### 1.5 Context

| # | Source | Expected PO output |
|---|--------|-------------------|
| 1.5.1 | `_t("Submit", context: "button")` | `msgctxt "button"` + `msgid "Submit"` |
| 1.5.2 | `_t("Submit", context: "form-label")` | `msgctxt "form-label"` + `msgid "Submit"` |
| 1.5.3 | `_t("Submit")` | `msgid "Submit"` (no `msgctxt`) |

---

## 2. Extraction: Markdown (`_m`)

Tests for the extractor transforming markdown to indexed placeholders.

### 2.1 Simple markdown constructs

| # | Source | Expected `msgid` | Placeholder mapping |
|---|--------|-------------------|---------------------|
| 2.1.1 | `_m("Click **here** to continue")` | `Click <0>here</0> to continue` | `<0>` = `<strong>` |
| 2.1.2 | `_m("Read *this* carefully")` | `Read <0>this</0> carefully` | `<0>` = `<em>` |
| 2.1.3 | `_m("See the \x60code\x60 example")` | `See the <0>code</0> example` | `<0>` = `<code>` |

### 2.2 Links

| # | Source | Expected `msgid` | Placeholder mapping |
|---|--------|-------------------|---------------------|
| 2.2.1 | `_m("Click [here]($url$)", new { url })` | `Click <0>here</0>` | `<0>` = `<a href="{url}">` |

### 2.3 Multiple formatting constructs

| # | Source | Expected `msgid` | Placeholder mapping |
|---|--------|-------------------|---------------------|
| 2.3.1 | `_m("Read **this** and click [here]($url$)", new { url })` | `Read <0>this</0> and click <1>here</1>` | `<0>` = `<strong>`, `<1>` = `<a href="{url}">` |

### 2.4 Nested markdown

| # | Source | Expected `msgid` | Placeholder mapping |
|---|--------|-------------------|---------------------|
| 2.4.1 | `_m("Click **[here]($url$)** to continue", new { url })` | `Click <0><1>here</1></0> to continue` | `<0>` = `<strong>`, `<1>` = `<a href="{url}">` |

### 2.5 Variables in visible text inside formatting

| # | Source | Expected `msgid` |
|---|--------|-------------------|
| 2.5.1 | `_m("Hello **$name$**!", new { name })` | `Hello <0>{name}</0>!` |

### 2.6 Markdown inside plural blocks

| # | Source | Expected `msgid` |
|---|--------|-------------------|
| 2.6.1 | `_m("You have $#x=0#no **new** items\|**#x#** new item\|**#x#** new items$", new { x })` | `You have {x, plural, =0 {no <0>new</0> items} one {<1>#</1> new item} other {<2>#</2> new items}}` |
| 2.6.2 | `_m("Hi **$name$**, you have $#x=0#no *new* items\|*#x#* new item\|*#x#* new items$", new { name, x })` | `Hi <0>{name}</0>, you have {x, plural, =0 {no <1>new</1> items} one {<2>#</2> new item} other {<3>#</3> new items}}` |

### 2.7 Context with `_m()`

| # | Source | Expected PO output |
|---|--------|-------------------|
| 2.7.1 | `_m("Click **here**", context: "navigation")` | `msgctxt "navigation"` + `msgid "Click <0>here</0>"` |

---

## 3. Runtime Output (`_t`)

Tests for rendered output given source-locale strings and variable values.
These are verified via source generator integration tests (inspecting generated code),
since `_t()` throws `InvalidOperationException` without an active source generator.

### 3.1 Simple variables

| # | Source | Args | Expected output |
|---|--------|------|-----------------|
| 3.1.1 | `_t("Welcome to $name$!", ...)` | `name = "Acme"` | `Welcome to Acme!` |
| 3.1.2 | `_t("Page $num$ of $total$", ...)` | `num = 2, total = 10` | `Page 2 of 10` |
| 3.1.3 | `_t("Save changes")` | *(none)* | `Save changes` |

### 3.2 Two-form plurals

| # | Source | Args | Expected output |
|---|--------|------|-----------------|
| 3.2.1 | `_t("You have $#x# book\|#x# books$", ...)` | `x = 1` | `You have 1 book` |
| 3.2.2 | `_t("You have $#x# book\|#x# books$", ...)` | `x = 3` | `You have 3 books` |
| 3.2.3 | `_t("$#~y#just one apple\|many apples (#y#)!$", ...)` | `y = 1` | `just one apple` |
| 3.2.4 | `_t("$#~y#just one apple\|many apples (#y#)!$", ...)` | `y = 7` | `many apples (7)!` |

### 3.3 Three-form plurals

| # | Source | Args | Expected output |
|---|--------|------|-----------------|
| 3.3.1 | `_t("You have $#x=0#no books\|#x# book\|#x# books$", ...)` | `x = 0` | `You have no books` |
| 3.3.2 | `_t("You have $#x=0#no books\|#x# book\|#x# books$", ...)` | `x = 1` | `You have 1 book` |
| 3.3.3 | `_t("You have $#x=0#no books\|#x# book\|#x# books$", ...)` | `x = 5` | `You have 5 books` |
| 3.3.4 | `_t("$#~x=0#no messages\|one new message\|#x# new messages$", ...)` | `x = 0` | `no messages` |
| 3.3.5 | `_t("$#~x=0#no messages\|one new message\|#x# new messages$", ...)` | `x = 1` | `one new message` |
| 3.3.6 | `_t("$#~x=0#no messages\|one new message\|#x# new messages$", ...)` | `x = 5` | `5 new messages` |

### 3.4 Composed

| # | Source | Args | Expected output |
|---|--------|------|-----------------|
| 3.4.1 | `_t("Hi $name$, you have $#x# book\|#x# books$ and $#~y#just one apple\|many apples (#y#)!$", ...)` | `name = "Alice", x = 3, y = 1` | `Hi Alice, you have 3 books and just one apple` |
| 3.4.2 | *(same)* | `name = "Alice", x = 1, y = 7` | `Hi Alice, you have 1 book and many apples (7)!` |

---

## 4. Runtime Output (`_m`)

Tests for rendered HTML output from markdown messages.
These are verified via source generator integration tests (inspecting generated code),
since `_m()` throws `InvalidOperationException` without an active source generator.

### 4.1 Simple markdown

| # | Source | Expected HTML output |
|---|--------|---------------------|
| 4.1.1 | `_m("Click **here** to continue")` | `Click <strong>here</strong> to continue` |

### 4.2 With variables

| # | Source | Args | Expected HTML output |
|---|--------|------|---------------------|
| 4.2.1 | `_m("Click **[here]($url$)** to continue", ...)` | `url = "/page"` | `Click <strong><a href="/page">here</a></strong> to continue` |

---

## 5. Translation (source generator with PO files)

Tests for baked-in translations selected by LCID.

### 5.1 Simple translation

Given PO:
```po
msgid "Save changes"
msgstr "Guardar cambios"
```

| # | LCID | Expected output |
|---|------|-----------------|
| 5.1.1 | Spanish | `Guardar cambios` |
| 5.1.2 | Source (0) | `Save changes` |

### 5.2 Translation with variables

Given PO:
```po
msgid "Welcome to {name}!"
msgstr "Bienvenido a {name}!"
```

| # | LCID | Args | Expected output |
|---|------|------|-----------------|
| 5.2.1 | Spanish | `name = "Acme"` | `Bienvenido a Acme!` |

### 5.3 Translation with plurals

Given PO:
```po
msgid "You have {x, plural, one {# book} other {# books}}"
msgstr "Tienes {x, plural, one {# libro} other {# libros}}"
```

| # | LCID | Args | Expected output |
|---|------|------|-----------------|
| 5.3.1 | Spanish | `x = 1` | `Tienes 1 libro` |
| 5.3.2 | Spanish | `x = 5` | `Tienes 5 libros` |

### 5.4 Fallback on missing translation

Given PO:
```po
msgid "Save changes"
msgstr ""
```

| # | LCID | Expected output |
|---|------|-----------------|
| 5.4.1 | Spanish | `Save changes` (falls back to source) |

### 5.5 Translator-reordered placeholders (`_m`)

Given PO:
```po
msgid "Read <0>this</0> and click <1>here</1>"
msgstr "Haz clic <1>aquí</1> y lee <0>esto</0>"
```

| # | LCID | Expected HTML output |
|---|------|---------------------|
| 5.5.1 | Spanish | `Haz clic <a href="...">aquí</a> y lee <strong>esto</strong>` |

### 5.6 Context-based disambiguation

Given PO:
```po
msgctxt "button"
msgid "Submit"
msgstr "Enviar"

msgctxt "form-label"
msgid "Submit"
msgstr "Presentar"
```

| # | Source | LCID | Expected output |
|---|--------|------|-----------------|
| 5.6.1 | `_t("Submit", context: "button")` | Spanish | `Enviar` |
| 5.6.2 | `_t("Submit", context: "form-label")` | Spanish | `Presentar` |

---

## 6. Pseudolocalization

Tests for the pseudo-locale accent transform.

### 6.1 Accent mapping

| # | Input char | Expected output | Rule |
|---|-----------|-----------------|------|
| 6.1.1 | `a` | `å` | ring above \u030A |
| 6.1.2 | `u` | `ů` | ring above \u030A |
| 6.1.3 | `A` | `Å` | ring above \u030A |
| 6.1.4 | `U` | `Ů` | ring above \u030A |
| 6.1.5 | `e` | `ë` | diaeresis \u0308 |
| 6.1.6 | `i` | `ï` | diaeresis \u0308 |
| 6.1.7 | `o` | `ö` | diaeresis \u0308 |
| 6.1.8 | `h` | `ḧ` | diaeresis \u0308 |
| 6.1.9 | `w` | `ẅ` | diaeresis \u0308 |
| 6.1.10 | `x` | `ẍ` | diaeresis \u0308 |
| 6.1.11 | `y` | `ÿ` | diaeresis \u0308 |
| 6.1.12 | `b` | `ḃ` | dot above \u0307 |
| 6.1.13 | `d` | `ḋ` | dot above \u0307 |
| 6.1.14 | `f` | `ḟ` | dot above \u0307 |
| 6.1.15 | `Q` | `Q̇` | dot above \u0307 |
| 6.1.16 | `v` | `ṽ` | tilde \u0303 |
| 6.1.17 | `V` | `Ṽ` | tilde \u0303 |
| 6.1.18 | `t` | `ţ` | cedilla \u0327 |
| 6.1.19 | `T` | `Ţ` | cedilla \u0327 |
| 6.1.20 | `c` | `ć` | acute accent \u0301 |
| 6.1.21 | `g` | `ǵ` | acute accent \u0301 |
| 6.1.22 | `l` | `ĺ` | acute accent \u0301 |
| 6.1.23 | `m` | `ḿ` | acute accent \u0301 |
| 6.1.24 | `n` | `ń` | acute accent \u0301 |

### 6.2 Preservation rules

| # | Input | Expected output | Rule |
|---|-------|-----------------|------|
| 6.2.1 | `"Hello world"` | `"Ḧëĺĺö ẅöŕĺḋ"` | Letters accented |
| 6.2.2 | `"Page 42!"` | `"Ṕåǵë 42!"` | Digits and punctuation preserved |
| 6.2.3 | `"Hi {name}"` | `"Ḧï {name}"` | ICU variables preserved |
| 6.2.4 | `"Click <0>here</0>"` | `"Ćĺïćḱ <0>ḧëŕë</0>"` | Placeholders preserved |

---

## 7. Compiler Diagnostics

### 7.1 Compile-time constant requirement

| # | Source | Expected |
|---|--------|----------|
| 7.1.1 | `_t("Hello")` | OK |
| 7.1.2 | `const string s = "Hello"; _t(s)` | OK |
| 7.1.3 | `var s = "Hello"; _t(s)` | OK |
| 7.1.4 | `_t(GetString())` | Error MB0001 |

---

## 8. PO File Operations

### 8.1 Extract creates new entries

Given source contains `_t("Save changes")` and PO file is empty:
- After `moonbuggy extract`: PO file contains `msgid "Save changes"` with empty `msgstr ""`

### 8.2 Extract preserves existing translations

Given PO already has:
```po
msgid "Save changes"
msgstr "Guardar cambios"
```
After `moonbuggy extract` (source still contains `_t("Save changes")`):
- `msgstr "Guardar cambios"` is preserved

### 8.3 Extract --clean removes obsolete entries

Given PO has `msgid "Old message"` but source no longer contains `_t("Old message")`:
- After `moonbuggy extract --clean`: entry is removed
- After `moonbuggy extract` (without --clean): entry is preserved

### 8.4 Validate --strict

Given PO has `msgid "Save changes"` with `msgstr ""`:
- `moonbuggy validate` → passes (missing translations are allowed)
- `moonbuggy validate --strict` → fails

---

## 9. Escaping

### 9.1 Dollar sign escaping

| # | Source | Expected `msgid` | Expected output |
|---|--------|-------------------|-----------------|
| 9.1.1 | `_t("Price: $$5")` | `Price: $5` | `Price: $5` |
| 9.1.2 | `_t("$$dollar$$ sign")` | `$dollar$ sign` | `$dollar$ sign` |
| 9.1.3 | `_t("$$marco$$ was here")` | `$marco$ was here` | `$marco$ was here` |
| 9.1.4 | `_t("Say $name$, costs $$10", new { name })` | `Say {name}, costs $10` | `Say Alice, costs $10` (name="Alice") |

### 9.2 Hash escaping inside plural blocks

| # | Source | Expected `msgid` |
|---|--------|-------------------|
| 9.2.1 | `_t("$#x# item (##)|#x# items (##)$", new { x })` | `{x, plural, one {# item (#)} other {# items (#)}}` |

### 9.3 Pipe escaping inside plural blocks

| # | Source | Expected `msgid` |
|---|--------|-------------------|
| 9.3.1 | `_t("$#x# a\|\|b|#x# c\|\|d$", new { x })` | `{x, plural, one {# a\|b} other {# c\|d}}` |

---

## 10. Edge Cases (Resolved)

### 10.1 `#var#` outside plural blocks — treated as literal text

| # | Source | Expected `msgid` | Expected output |
|---|--------|-------------------|-----------------|
| 10.1.1 | `_t("You have #x# items")` | `You have #x# items` | `You have #x# items` |

### 10.2 `$var$` inside plural branches — valid, normal variable substitution

| # | Source | Expected `msgid` |
|---|--------|-------------------|
| 10.2.1 | `_t("$#x# book by $author$\|#x# books by $author$$", new { x, author })` | `{x, plural, one {# book by {author}} other {# books by {author}}}` |

### 10.3 Markdown in `_t()` — literal text, not processed

| # | Source | Expected output |
|---|--------|-----------------|
| 10.3.1 | `_t("Click **here**")` | `Click **here**` |

### 10.4 Empty message — error

| # | Source | Expected |
|---|--------|----------|
| 10.4.1 | `_t("")` | Error MB0007 |
| 10.4.2 | `_m("")` | Error MB0007 |

### 10.5 Message with only variables — valid

| # | Source | Expected `msgid` | Expected output |
|---|--------|-------------------|-----------------|
| 10.5.1 | `_t("$name$", new { name })` | `{name}` | `Alice` (name="Alice") |

### 10.6 Duplicate messages across files — single PO entry

| # | Scenario | Expected |
|---|----------|----------|
| 10.6.1 | `_t("Save")` in `FileA.cs` and `_t("Save")` in `FileB.cs` | One `msgid "Save"` entry in PO file |
| 10.6.2 | `_t("Save", context: "button")` and `_t("Save", context: "menu")` | Two separate entries (different `msgctxt`) |

### 10.7 Context must be compile-time constant

| # | Source | Expected |
|---|--------|----------|
| 10.7.1 | `_t("Save", context: "button")` | OK |
| 10.7.2 | `_t("Save", context: GetContext())` | Error MB0008 |

### 10.8 Plural selector must be integer type

| # | Source | Expected |
|---|--------|----------|
| 10.8.1 | `_t("$#x# item\|#x# items$", new { x = 1 })` | OK (int) |
| 10.8.2 | `_t("$#x# item\|#x# items$", new { x = 1L })` | OK (long) |
| 10.8.3 | `_t("$#x# item\|#x# items$", new { x = 1.5 })` | Error (double — not an integer type) |

### 10.9 Placeholder indices are global, no restart across plural boundaries

| # | Source | Expected `msgid` |
|---|--------|-------------------|
| 10.9.1 | `_m("Click **here** — $#x# *new* item\|*#x#* items$")` | `Click <0>here</0> — {x, plural, one {<1>new</1> item} other {<2>#</2> items}}` |

### 10.10 HTML in `_m()` — passed to Markdig, behavior depends on processor

| # | Scenario | Expected |
|---|----------|----------|
| 10.10.1 | `_m("Click <strong>here</strong>")` | Output determined by Markdig (CommonMark passes through raw HTML by default) |
