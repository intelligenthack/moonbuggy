using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using MoonBuggy.SourceGenerator;

namespace MoonBuggy.SourceGenerator.Tests;

public class AnalyzerTests
{
    private static async Task<ImmutableArray<Diagnostic>> RunAnalyzer(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
        };

        var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        references.Add(MetadataReference.CreateFromFile(
            Path.Combine(runtimeDir, "System.Runtime.dll")));
        references.Add(MetadataReference.CreateFromFile(typeof(MoonBuggy.Translate).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(
            typeof(Microsoft.AspNetCore.Html.IHtmlContent).Assembly.Location));

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.ConsoleApplication));

        var analyzer = new MoonBuggyAnalyzer();
        var compilationWithAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));

        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
        return diagnostics;
    }

    // --- MB0001: Non-constant first argument ---

    [Fact]
    public async Task Reports_MB0001_ForNonConstantMessage()
    {
        var source = @"
using static MoonBuggy.Translate;
class C {
    static string GetMsg() => ""hi"";
    static void M() { _t(GetMsg()); }
}";
        var diagnostics = await RunAnalyzer(source);
        Assert.Contains(diagnostics, d => d.Id == "MB0001");
    }

    [Fact]
    public async Task NoError_ForConstantMessage()
    {
        var source = @"
using static MoonBuggy.Translate;
class C {
    static void M() { _t(""Hello""); }
}";
        var diagnostics = await RunAnalyzer(source);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
    }

    // --- MB0002: Missing arg property ---

    [Fact]
    public async Task Reports_MB0002_ForMissingArgProperty()
    {
        var source = @"
using static MoonBuggy.Translate;
class C {
    static void M() { _t(""Hello $name$!""); }
}";
        var diagnostics = await RunAnalyzer(source);
        Assert.Contains(diagnostics, d => d.Id == "MB0002");
    }

    // --- MB0003: Extra arg property ---

    [Fact]
    public async Task Reports_MB0003_ForExtraArgProperty()
    {
        var source = @"
using static MoonBuggy.Translate;
class C {
    static void M() { _t(""Hello"", new { name = ""x"" }); }
}";
        var diagnostics = await RunAnalyzer(source);
        Assert.Contains(diagnostics, d => d.Id == "MB0003");
    }

    // --- MB0005: Malformed MB syntax ---

    [Fact]
    public async Task Reports_MB0005_ForMalformedSyntax()
    {
        var source = @"
using static MoonBuggy.Translate;
class C {
    static void M() { _t(""$unclosed""); }
}";
        var diagnostics = await RunAnalyzer(source);
        Assert.Contains(diagnostics, d => d.Id == "MB0005");
    }

    // --- MB0007: Empty message ---

    [Fact]
    public async Task Reports_MB0007_ForEmptyMessage()
    {
        var source = @"
using static MoonBuggy.Translate;
class C {
    static void M() { _t(""""); }
}";
        var diagnostics = await RunAnalyzer(source);
        Assert.Contains(diagnostics, d => d.Id == "MB0007");
    }

    // --- MB0008: Non-constant context ---

    [Fact]
    public async Task Reports_MB0008_ForNonConstantContext()
    {
        var source = @"
using static MoonBuggy.Translate;
class C {
    static string GetCtx() => ""btn"";
    static void M() { _t(""Save"", context: GetCtx()); }
}";
        var diagnostics = await RunAnalyzer(source);
        Assert.Contains(diagnostics, d => d.Id == "MB0008");
    }

    // --- MB0009: Plural selector not integer type ---

    [Fact]
    public async Task Reports_MB0009_ForDoublePluralSelector()
    {
        var source = @"
using static MoonBuggy.Translate;
class C {
    static void M() { _t(""$#x# item|#x# items$"", new { x = 1.5 }); }
}";
        var diagnostics = await RunAnalyzer(source);
        Assert.Contains(diagnostics, d => d.Id == "MB0009");
    }

    [Fact]
    public async Task Reports_MB0009_ForFloatPluralSelector()
    {
        var source = @"
using static MoonBuggy.Translate;
class C {
    static void M() { _t(""$#x# item|#x# items$"", new { x = 1.5f }); }
}";
        var diagnostics = await RunAnalyzer(source);
        Assert.Contains(diagnostics, d => d.Id == "MB0009");
    }

    [Fact]
    public async Task Reports_MB0009_ForDecimalPluralSelector()
    {
        var source = @"
using static MoonBuggy.Translate;
class C {
    static void M() { _t(""$#x# item|#x# items$"", new { x = 1.5m }); }
}";
        var diagnostics = await RunAnalyzer(source);
        Assert.Contains(diagnostics, d => d.Id == "MB0009");
    }

    [Fact]
    public async Task NoError_ForIntPluralSelector()
    {
        var source = @"
using static MoonBuggy.Translate;
class C {
    static void M() { _t(""$#x# item|#x# items$"", new { x = 1 }); }
}";
        var diagnostics = await RunAnalyzer(source);
        Assert.DoesNotContain(diagnostics, d => d.Id == "MB0009");
    }

    [Fact]
    public async Task NoError_ForLongPluralSelector()
    {
        var source = @"
using static MoonBuggy.Translate;
class C {
    static void M() { _t(""$#x# item|#x# items$"", new { x = 1L }); }
}";
        var diagnostics = await RunAnalyzer(source);
        Assert.DoesNotContain(diagnostics, d => d.Id == "MB0009");
    }

    [Fact]
    public async Task NoError_ForBytePluralSelector()
    {
        var source = @"
using static MoonBuggy.Translate;
class C {
    static void M() { _t(""$#x# item|#x# items$"", new { x = (byte)1 }); }
}";
        var diagnostics = await RunAnalyzer(source);
        Assert.DoesNotContain(diagnostics, d => d.Id == "MB0009");
    }

    // --- Analyzer does NOT report PO-dependent diagnostics ---

    [Fact]
    public async Task DoesNotReport_MB0004()
    {
        var source = @"
using static MoonBuggy.Translate;
class C {
    static void M() { _t(""Hello""); }
}";
        var diagnostics = await RunAnalyzer(source);
        Assert.DoesNotContain(diagnostics, d => d.Id == "MB0004");
    }

    [Fact]
    public async Task DoesNotReport_MB0006()
    {
        var source = @"
using static MoonBuggy.Translate;
class C {
    static void M() { _m(""Click **here**""); }
}";
        var diagnostics = await RunAnalyzer(source);
        Assert.DoesNotContain(diagnostics, d => d.Id == "MB0006");
    }

    // --- _m() works too ---

    [Fact]
    public async Task Reports_MB0001_ForNonConstant_M_Call()
    {
        var source = @"
using static MoonBuggy.Translate;
class C {
    static string GetMsg() => ""hi"";
    static void M() { _m(GetMsg()); }
}";
        var diagnostics = await RunAnalyzer(source);
        Assert.Contains(diagnostics, d => d.Id == "MB0001");
    }
}
