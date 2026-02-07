using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using MoonBuggy.SourceGenerator;

namespace MoonBuggy.SourceGenerator.Tests;

public class GeneratorSetupTests
{
    [Fact]
    public void Generator_RunsWithoutError_OnEmptySource()
    {
        var source = @"
namespace TestApp
{
    public class Program
    {
        public static void Main() { }
    }
}";
        var (diagnostics, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void DiagnosticDescriptors_AllExist()
    {
        Assert.Equal("MB0001", Diagnostics.NonConstantMessage.Id);
        Assert.Equal("MB0002", Diagnostics.MissingArgProperty.Id);
        Assert.Equal("MB0003", Diagnostics.ExtraArgProperty.Id);
        Assert.Equal("MB0004", Diagnostics.PoFileNotFound.Id);
        Assert.Equal("MB0005", Diagnostics.MalformedMbSyntax.Id);
        Assert.Equal("MB0006", Diagnostics.BadMarkdownOutput.Id);
        Assert.Equal("MB0007", Diagnostics.EmptyMessage.Id);
        Assert.Equal("MB0008", Diagnostics.NonConstantContext.Id);
    }

    [Fact]
    public void DiagnosticSeverities_AreCorrect()
    {
        Assert.Equal(DiagnosticSeverity.Error, Diagnostics.NonConstantMessage.DefaultSeverity);
        Assert.Equal(DiagnosticSeverity.Error, Diagnostics.MissingArgProperty.DefaultSeverity);
        Assert.Equal(DiagnosticSeverity.Warning, Diagnostics.ExtraArgProperty.DefaultSeverity);
        Assert.Equal(DiagnosticSeverity.Warning, Diagnostics.PoFileNotFound.DefaultSeverity);
        Assert.Equal(DiagnosticSeverity.Error, Diagnostics.MalformedMbSyntax.DefaultSeverity);
        Assert.Equal(DiagnosticSeverity.Warning, Diagnostics.BadMarkdownOutput.DefaultSeverity);
        Assert.Equal(DiagnosticSeverity.Error, Diagnostics.EmptyMessage.DefaultSeverity);
        Assert.Equal(DiagnosticSeverity.Error, Diagnostics.NonConstantContext.DefaultSeverity);
    }

    [Fact]
    public void Generator_IgnoresNonTranslateMethods()
    {
        var source = @"
namespace TestApp
{
    public class Program
    {
        public static void Main()
        {
            SomeMethod(""hello"");
        }
        static void SomeMethod(string s) { }
    }
}";
        var (diagnostics, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Empty(generatedTrees);
    }
}
