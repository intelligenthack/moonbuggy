using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using MoonBuggy.SourceGenerator;

namespace MoonBuggy.SourceGenerator.Tests;

internal static class GeneratorTestHelper
{
    public static (ImmutableArray<Diagnostic> Diagnostics, SyntaxTree[] GeneratedTrees) RunGenerator(
        string source, params AdditionalText[] additionalTexts)
    {
        return RunGenerator(source, pseudoLocale: false, additionalTexts: additionalTexts);
    }

    public static (ImmutableArray<Diagnostic> Diagnostics, SyntaxTree[] GeneratedTrees) RunGenerator(
        string source, bool pseudoLocale, params AdditionalText[] additionalTexts)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
        };

        // Add runtime assembly references
        var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        references.Add(MetadataReference.CreateFromFile(
            Path.Combine(runtimeDir, "System.Runtime.dll")));

        // Add MoonBuggy runtime reference
        references.Add(MetadataReference.CreateFromFile(typeof(MoonBuggy.Translate).Assembly.Location));

        // Add ASP.NET HTML abstractions
        references.Add(MetadataReference.CreateFromFile(
            typeof(Microsoft.AspNetCore.Html.IHtmlContent).Assembly.Location));

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.ConsoleApplication));

        var optionsProvider = new TestAnalyzerConfigOptionsProvider(pseudoLocale);

        var generator = new MoonBuggyGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
                generators: new ISourceGenerator[] { generator.AsSourceGenerator() },
                additionalTexts: ImmutableArray.Create(additionalTexts),
                optionsProvider: optionsProvider);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        var runResult = driver.GetRunResult();
        var generatedTrees = runResult.GeneratedTrees.ToArray();

        return (diagnostics, generatedTrees);
    }
}

internal sealed class TestAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
{
    private readonly TestGlobalOptions _globalOptions;

    public TestAnalyzerConfigOptionsProvider(bool pseudoLocale)
    {
        _globalOptions = new TestGlobalOptions(pseudoLocale);
    }

    public override AnalyzerConfigOptions GlobalOptions => _globalOptions;

    public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => TestEmptyOptions.Instance;
    public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => TestEmptyOptions.Instance;
}

internal sealed class TestGlobalOptions : AnalyzerConfigOptions
{
    private readonly Dictionary<string, string> _options;

    public TestGlobalOptions(bool pseudoLocale)
    {
        _options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (pseudoLocale)
            _options["build_property.MoonBuggyPseudoLocale"] = "true";
    }

    public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
    {
        return _options.TryGetValue(key, out value);
    }
}

internal sealed class TestEmptyOptions : AnalyzerConfigOptions
{
    public static readonly TestEmptyOptions Instance = new();

    public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
    {
        value = null;
        return false;
    }
}
