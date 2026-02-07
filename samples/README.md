# MoonBuggy Sample

A minimal ASP.NET Razor Pages app demonstrating MoonBuggy's i18n features.

## Using MoonBuggy in your own project

### 1. Project configuration

Add to your `.csproj`:

```xml
<PropertyGroup>
  <!-- Enable the interceptors feature for the MoonBuggy namespace -->
  <Features>$(Features);InterceptorsNamespaces=MoonBuggy.Generated</Features>

  <!-- Required for Razor Pages/MVC: use the legacy Razor pipeline so the
       MoonBuggy source generator can see _t()/_m() calls in .cshtml files -->
  <UseRazorSourceGenerator>false</UseRazorSourceGenerator>
</PropertyGroup>

<ItemGroup>
  <!-- Reference the MoonBuggy NuGet packages -->
  <PackageReference Include="intelligenthack.MoonBuggy" Version="..." />

  <!-- Register PO files as additional files for the source generator -->
  <AdditionalFiles Include="locales\**\*.po" />
</ItemGroup>
```

### 2. InterceptsLocation polyfill

Add this file to your project (the NuGet package will provide this automatically in a future version):

```csharp
// InterceptsLocationAttribute.cs
#pragma warning disable CS9113
namespace System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class InterceptsLocationAttribute(int version, string data) : Attribute;
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
# Build the main solution first (the sample references built DLLs)
dotnet build

# Then build and run the sample
dotnet run --project samples/MoonBuggy.Sample --urls http://+:5050
```

Then open http://localhost:5050 in your browser.

> **Note:** This sample references MoonBuggy via direct DLL references
> (`<Reference>` and `<Analyzer>` items). In a real project you'd use the
> NuGet packages instead.

## Technical notes

- **Legacy Razor pipeline:** `<UseRazorSourceGenerator>false</UseRazorSourceGenerator>` forces the legacy Razor compilation pipeline, which emits `.cshtml.g.cs` files as a pre-build step. This makes `_t()`/`_m()` calls in Razor views visible to the MoonBuggy source generator. The modern Razor source generator runs in the same compilation pass as MoonBuggy's generator, so they can't see each other's output.
- **Interceptors feature flag:** The `<Features>` property passes `/features:InterceptorsNamespaces=MoonBuggy.Generated` directly to the compiler. The standard `<InterceptorsNamespaces>` MSBuild property doesn't reliably propagate when targeting net8.0 with the .NET 10 SDK.
- **InterceptsLocationAttribute:** Required by the Roslyn interceptors feature. The MoonBuggy generator emits `[InterceptsLocation]` attributes but does not define the attribute class itself.

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
