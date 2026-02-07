using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using MoonBuggy.SourceGenerator;

namespace MoonBuggy.SourceGenerator.Tests;

internal static class GeneratorTestHelper
{
    public static (ImmutableArray<Diagnostic> Diagnostics, SyntaxTree[] GeneratedTrees) RunGenerator(
        string source, params AdditionalText[] additionalTexts)
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

        var generator = new MoonBuggyGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator)
            .AddAdditionalTexts(ImmutableArray.Create(additionalTexts));
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        var runResult = driver.GetRunResult();
        var generatedTrees = runResult.GeneratedTrees.ToArray();

        return (diagnostics, generatedTrees);
    }
}
