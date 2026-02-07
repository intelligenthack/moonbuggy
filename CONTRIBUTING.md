# Contributing to MoonBuggy

Thank you for your interest in contributing to MoonBuggy!

## How to Contribute

### Reporting Bugs

- Use [GitHub Issues](https://github.com/intelligenthack/moonbuggy/issues) to report bugs.
- Include a minimal reproduction case and the .NET version you're using.

### Suggesting Features

- Open an issue describing the feature and its use case.

### Pull Requests

1. Fork the repo and create your branch from `main`.
2. Make your changes and add tests.
3. Ensure all tests pass: `dotnet test`
4. Submit a pull request.

## Development Setup

### Prerequisites

- .NET 8 SDK or later

### Build and test

```bash
dotnet build    # Build the solution
dotnet test     # Run all tests (~293 tests across 5 projects)
```

### Solution structure

```
src/MoonBuggy/                  # Runtime library (net8.0;net10.0)
src/MoonBuggy.Core/             # Shared internals (netstandard2.0)
src/MoonBuggy.SourceGenerator/  # Roslyn source generator + analyzer (netstandard2.0)
src/MoonBuggy.Cli/              # CLI tool (net8.0;net10.0)
tests/                          # xUnit test projects mirroring src/
build/cldr/                     # CLDR plural rules codegen
```

### Running specific test suites

```bash
dotnet test tests/MoonBuggy.Core.Tests              # Core parsing, ICU, PO, markdown
dotnet test tests/MoonBuggy.SourceGenerator.Tests    # Source generator integration
dotnet test tests/MoonBuggy.Cli.Tests                # CLI integration
dotnet test --filter "FullyQualifiedName~TestName"   # Single test
```

### CLDR plural rules

Plural rule conditions are generated C# code checked into the repo. To regenerate after a CLDR version upgrade:

```bash
dotnet build build/MoonBuggy.CldrGen
dotnet script build/cldr/generate-plural-rules.csx
```

### Key constraints

- **MoonBuggy.Core** and **MoonBuggy.SourceGenerator** must target `netstandard2.0` (Roslyn requirement for analyzers/generators).
- The source generator package must be self-contained: Core and Markdig DLLs are packed into `analyzers/dotnet/cs/`.
- `TreatWarningsAsErrors` is enabled solution-wide.

## Code Style

- Follow existing conventions in the codebase.
- Keep changes focused — one concern per PR.
- The solution uses `LangVersion=latest`, `Nullable=enable`, and `ImplicitUsings=enable`.

## License

By contributing, you agree that your contributions will be licensed under the MIT License.
