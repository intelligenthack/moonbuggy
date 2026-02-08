---
sidebar_position: 5
title: Lingui.js Co-existence
---

# Lingui.js Co-existence

MoonBuggy and [Lingui.js](https://lingui.dev/) can share the same PO files. This is useful when your application has both a .NET backend (Razor views, APIs) and a JavaScript frontend (React, Vue, etc.) — you maintain a single set of translation files for both.

## How it works

Both MoonBuggy and Lingui.js use ICU MessageFormat as the `msgid` key in PO files. When both extractors write to the same PO file, entries from either tool coexist because they share the same key format.

For example, if your Razor view has:

```csharp
_t("Save changes")
```

And your React component has:

```tsx
t`Save changes`
```

Both produce a PO entry with `msgid "Save changes"` — there's no conflict. The translation is shared.

## Configuration

Point both configurations at the same catalog path:

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

The `path` pattern must match. The `include` globs are different because each extractor scans its own source files.

## Extraction workflow

Run both extractors independently:

```bash
moonbuggy extract    # Scans .cs and .cshtml files
npx lingui extract   # Scans .ts and .tsx files
```

Both commands are non-destructive — they merge new entries into existing PO files without removing entries added by the other extractor.

The order doesn't matter. Each extractor only adds or updates entries based on its own source files.

## Shared translations

When both codebases use the same English string, it appears as a single PO entry with one translation. This is a natural deduplication:

```gettext
#: Pages/Index.cshtml.cs:15
#: src/components/Header.tsx:8
msgid "Save changes"
msgstr "Guardar cambios"
```

The translator provides one translation that serves both the .NET and JavaScript sides.

## Different translations for same text

If the same English text needs different translations depending on context (server vs client), use the `context` parameter:

**In C#:**
```csharp
_t("Submit", context: "server-form")
```

**In JavaScript:**
```tsx
t({message: "Submit", context: "client-button"})
```

These produce separate PO entries with different `msgctxt` values.

## CI validation

Run both validators in your CI pipeline:

```bash
moonbuggy validate --strict
npx lingui compile --strict
```

This catches missing translations on both sides before they ship.
