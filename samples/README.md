# MoonBuggy Sample

A minimal ASP.NET Razor Pages app demonstrating MoonBuggy's i18n features.

## Using MoonBuggy in your own project

### 1. Install the NuGet packages

```xml
<ItemGroup>
  <PackageReference Include="intelligenthack.MoonBuggy" Version="0.1.0" />
  <PackageReference Include="intelligenthack.MoonBuggy.SourceGenerator" Version="0.1.0" />
</ItemGroup>
```

The source generator package automatically configures interceptors and includes PO files (when `moonbuggy.config.json` exists in your project directory).

### 2. Razor Pages / MVC projects

For Razor Pages or MVC projects, add this to your `.csproj`:

```xml
<PropertyGroup>
  <!-- Use the legacy Razor pipeline so the MoonBuggy source generator
       can see _t()/_m() calls in .cshtml files -->
  <UseRazorSourceGenerator>false</UseRazorSourceGenerator>
</PropertyGroup>
```

### 3. View imports

Add to `Pages/_ViewImports.cshtml` (Razor Pages) or `Views/_ViewImports.cshtml` (MVC):

```cshtml
@using static MoonBuggy.Translate
```

### 4. Locale middleware

Set `I18n.Current.LCID` in your request pipeline — see `Program.cs` for an example using query parameters and cookies.

## Running this sample

```bash
# Pack the NuGet packages into ./artifacts (required once, or after code changes)
dotnet pack -c Release -o ./artifacts

# Then build and run the sample
dotnet run --project samples/MoonBuggy.Sample --urls http://+:5050
```

Then open http://localhost:5050 in your browser.

> **Note:** This sample uses a local NuGet feed (`samples/nuget.config`)
> pointing to `./artifacts`. Run `dotnet pack` before building the sample.

## Technical notes

- **Legacy Razor pipeline:** `<UseRazorSourceGenerator>false</UseRazorSourceGenerator>` forces the legacy Razor compilation pipeline, which emits `.cshtml.g.cs` files as a pre-build step. This makes `_t()`/`_m()` calls in Razor views visible to the MoonBuggy source generator. The modern Razor source generator runs in the same compilation pass as MoonBuggy's generator, so they can't see each other's output.
- **Interceptors feature flag:** Automatically configured by the `intelligenthack.MoonBuggy.SourceGenerator` package via its `.props` file.
- **InterceptsLocationAttribute:** Automatically emitted by the source generator as a `file`-scoped class — no manual polyfill needed.
- **PO file discovery:** Automatically configured by the source generator package's `.targets` file when `moonbuggy.config.json` exists.

## What it demonstrates

### Index page (`/`)

- `_t()` with variables: `$name$`, `$user$`, `$num$`, `$total$`
- `_t()` with plurals: two-form (`one | other`) and three-form (`=0 | one | other`)
- `_m()` with markdown: bold text, links with variable URLs

### Context page (`/Context`)

- Same English text ("Submit", "Post") with different `context` values producing different Spanish translations

### Locale switcher

- Links in the nav bar set `?lang=en` or `?lang=es`
- Middleware reads the query parameter, persists it as a cookie, and sets `I18n.Current.LCID`
- All `_t()` and `_m()` calls on the page render in the selected language

## Locales

Pre-populated PO files for English (`en`) and Spanish (`es`) are in `locales/`.
