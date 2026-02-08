---
sidebar_position: 1
title: CLI Reference
---

# CLI Reference

## Installation

```bash
# Global tool
dotnet tool install -g intelligenthack.MoonBuggy.Cli

# Local tool (project-level, tracked in .config/dotnet-tools.json)
dotnet tool install intelligenthack.MoonBuggy.Cli
```

Both methods provide the `moonbuggy` command.

## moonbuggy extract

Scans source files for `_t()` and `_m()` calls, transforms MoonBuggy syntax to ICU MessageFormat, and writes or updates PO files.

```
moonbuggy extract [files...] [options]
```

### Arguments

| Argument | Description |
|----------|-------------|
| `[files...]` | Optional list of specific files to scan. When omitted, scans all files matching the `include` globs in `moonbuggy.config.json`. |

### Options

| Flag | Description |
|------|-------------|
| `--clean` | Remove PO entries for strings no longer found in source. Without this flag, obsolete entries are preserved. |
| `--locale <locale>` | Extract for specific locale(s) only. Can be repeated: `--locale es --locale ru`. Must match a locale in the config. |
| `--verbose` | Show detailed extraction output. |
| `--watch` | Watch mode. Re-extracts when source files change. Respects `--locale` and `[files...]` filters. |

### Examples

```bash
# Extract all strings for all locales
moonbuggy extract

# Extract only from specific files
moonbuggy extract Pages/Index.cshtml.cs Pages/Shared/*.cs

# Extract for Spanish only, removing obsolete entries
moonbuggy extract --clean --locale es

# Watch mode during development
moonbuggy extract --watch
```

### Output

Prints a statistics table after extraction:

```
Catalog statistics:
┌──────────┬─────────────┬─────────┐
│ Language │ Total count │ Missing │
├──────────┼─────────────┼─────────┤
│ en       │     42      │    0    │
│ es       │     42      │    3    │
│ ru       │     42      │    7    │
└──────────┴─────────────┴─────────┘
```

### Behavior

- **Non-destructive by default.** Existing translations in PO files are preserved. New entries get empty `msgstr`.
- **Idempotent.** Running extract twice produces the same result.
- **Shared PO files.** If Lingui.js also writes to the same PO files, entries from both extractors coexist. They share the same `msgid` key format (ICU MessageFormat).

## moonbuggy validate

Validates PO files for completeness and correctness without modifying them.

```
moonbuggy validate [options]
```

### Options

| Flag | Description |
|------|-------------|
| `--strict` | Fail (exit code 1) on any missing translation (empty `msgstr`). Without this, missing translations produce warnings. |
| `--locale <locale>` | Validate specific locale(s) only. Can be repeated. |
| `--verbose` | Show detailed validation output. |

### Checks performed

1. **Completeness** — every `msgid` has a non-empty `msgstr`.
2. **Variable consistency** — variables in `msgstr` match those in `msgid`. No missing or extra `{var}` placeholders.
3. **ICU validity** — `msgstr` contains valid ICU MessageFormat syntax.
4. **Plural forms** — plural categories in `msgstr` match CLDR requirements for the target locale.

### Examples

```bash
# Validate all locales (warnings only)
moonbuggy validate

# Strict mode for CI — fail on any missing translation
moonbuggy validate --strict

# Validate only Spanish
moonbuggy validate --locale es --verbose
```

### CI integration

Add to your CI pipeline to catch translation issues before they ship:

```yaml
- name: Validate translations
  run: moonbuggy validate --strict
```

## Configuration

Both commands read `moonbuggy.config.json` from the current directory. See [Configuration](configuration.md) for the file format.

## Exit codes

| Code | Meaning |
|------|---------|
| 0 | Success |
| 1 | Validation failure (with `--strict`) or error |
