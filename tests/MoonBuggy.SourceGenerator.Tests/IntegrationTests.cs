using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace MoonBuggy.SourceGenerator.Tests;

public class IntegrationTests
{
    [Fact]
    public void NoPoFiles_EmitsSourceLocaleOnly()
    {
        var source = @"
using static MoonBuggy.Translate;

public class Program
{
    public static void Main()
    {
        var result = _t(""Hello world"");
    }
}";
        var (diagnostics, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Single(generatedTrees);

        var generated = generatedTrees[0].GetText().ToString();
        Assert.Contains("\"Hello world\"", generated);
        // No LCID switch since no PO files
        Assert.DoesNotContain("lcid", generated);
    }

    [Fact]
    public void SingleLocale_EmitsLocaleSwitch()
    {
        var source = @"
using static MoonBuggy.Translate;

public class Program
{
    public static void Main()
    {
        var result = _t(""Hello"");
    }
}";
        var poContent = @"
msgid ""Hello""
msgstr ""Hola""
";
        var additionalText = new InMemoryAdditionalText("locales/es/messages.po", poContent);
        var (diagnostics, generatedTrees) = GeneratorTestHelper.RunGenerator(source, additionalText);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Single(generatedTrees);

        var generated = generatedTrees[0].GetText().ToString();
        Assert.Contains("\"Hola\"", generated);
        Assert.Contains("\"Hello\"", generated);
        Assert.Contains("lcid", generated);
    }

    [Fact]
    public void MultipleLocales_EmitsMultiBranchSwitch()
    {
        var source = @"
using static MoonBuggy.Translate;

public class Program
{
    public static void Main()
    {
        var result = _t(""Hello"");
    }
}";
        var esContent = @"
msgid ""Hello""
msgstr ""Hola""
";
        var frContent = @"
msgid ""Hello""
msgstr ""Bonjour""
";
        var esPo = new InMemoryAdditionalText("locales/es/messages.po", esContent);
        var frPo = new InMemoryAdditionalText("locales/fr/messages.po", frContent);
        var (diagnostics, generatedTrees) = GeneratorTestHelper.RunGenerator(source, esPo, frPo);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Single(generatedTrees);

        var generated = generatedTrees[0].GetText().ToString();
        Assert.Contains("\"Hola\"", generated);
        Assert.Contains("\"Bonjour\"", generated);
        Assert.Contains("\"Hello\"", generated);
    }

    [Fact]
    public void MissingTranslation_FallsBackToSourceForThatMessage()
    {
        var source = @"
using static MoonBuggy.Translate;

public class Program
{
    public static void Main()
    {
        var a = _t(""Hello"");
        var b = _t(""Goodbye"");
    }
}";
        var poContent = @"
msgid ""Hello""
msgstr ""Hola""
";
        var additionalText = new InMemoryAdditionalText("locales/es/messages.po", poContent);
        var (diagnostics, generatedTrees) = GeneratorTestHelper.RunGenerator(source, additionalText);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Single(generatedTrees);

        var generated = generatedTrees[0].GetText().ToString();
        // "Hello" has translation
        Assert.Contains("\"Hola\"", generated);
        // "Goodbye" has no translation — only source
        Assert.Contains("\"Goodbye\"", generated);
    }

    [Fact]
    public void MB0002_MissingArgProperty_ReportsError()
    {
        var source = @"
using static MoonBuggy.Translate;

public class Program
{
    public static void Main()
    {
        var result = _t(""Hello $name$!"", new { });
    }
}";
        var (diagnostics, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        // MB0002: Variable 'name' in message has no matching property in args
        Assert.Contains(diagnostics, d => d.Id == "MB0002");
    }

    [Fact]
    public void MB0005_MalformedMbSyntax_ReportsError()
    {
        // Malformed: unclosed $ variable
        var source = @"
using static MoonBuggy.Translate;

public class Program
{
    public static void Main()
    {
        var result = _t(""Hello $name"");
    }
}";
        var (diagnostics, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        Assert.Contains(diagnostics, d => d.Id == "MB0005");
    }

    [Fact]
    public void EndToEnd_MultipleCallSites_MultipleLocales()
    {
        var source = @"
using static MoonBuggy.Translate;

public class Program
{
    public static void Main()
    {
        var name = ""World"";
        var x = 5;
        var a = _t(""Hello $name$!"", new { name });
        var b = _t(""You have $#x# item|#x# items$"", new { x });
    }
}";
        var poContent = @"
msgid ""Hello {name}!""
msgstr ""Hola {name}!""

msgid ""You have {x, plural, one {# item} other {# items}}""
msgstr ""Tienes {x, plural, one {# elemento} other {# elementos}}""
";
        var additionalText = new InMemoryAdditionalText("locales/es/messages.po", poContent);
        var (diagnostics, generatedTrees) = GeneratorTestHelper.RunGenerator(source, additionalText);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Single(generatedTrees);

        var generated = generatedTrees[0].GetText().ToString();
        // Both call sites should have interceptors
        Assert.Contains("Hola", generated);
        Assert.Contains("elemento", generated);
        Assert.Contains("elementos", generated);
    }
}
