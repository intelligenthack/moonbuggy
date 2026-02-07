namespace MoonBuggy.SourceGenerator.Tests;

public class PseudoLocaleTests
{
    [Fact]
    public void PseudoLocaleEnabled_EmitsLcid4096Branch()
    {
        var source = @"
using static MoonBuggy.Translate;

public class Program
{
    public static void Main()
    {
        var result = _t(""Save changes"");
    }
}";
        var (diagnostics, generatedTrees) = GeneratorTestHelper.RunGenerator(
            source, pseudoLocale: true);

        Assert.DoesNotContain(diagnostics, d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error);
        Assert.Single(generatedTrees);

        var generated = generatedTrees[0].GetText().ToString();
        // Should contain pseudo-locale branch with LCID 4096
        Assert.Contains("4096", generated);
        Assert.Contains("lcid", generated);
    }

    [Fact]
    public void PseudoLocaleEnabled_EmitsAccentedText()
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
        var (diagnostics, generatedTrees) = GeneratorTestHelper.RunGenerator(
            source, pseudoLocale: true);

        Assert.DoesNotContain(diagnostics, d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error);

        var generated = generatedTrees[0].GetText().ToString();
        // "Hello" → "Ḧëĺĺö"
        Assert.Contains("Ḧëĺĺö", generated);
    }

    [Fact]
    public void PseudoLocaleDisabled_NoLcid4096()
    {
        var source = @"
using static MoonBuggy.Translate;

public class Program
{
    public static void Main()
    {
        var result = _t(""Save changes"");
    }
}";
        var (diagnostics, generatedTrees) = GeneratorTestHelper.RunGenerator(
            source, pseudoLocale: false);

        Assert.DoesNotContain(diagnostics, d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error);
        Assert.Single(generatedTrees);

        var generated = generatedTrees[0].GetText().ToString();
        Assert.DoesNotContain("4096", generated);
    }

    [Fact]
    public void PseudoLocaleDefault_NoLcid4096()
    {
        var source = @"
using static MoonBuggy.Translate;

public class Program
{
    public static void Main()
    {
        var result = _t(""Save changes"");
    }
}";
        // No pseudoLocale parameter = default false
        var (diagnostics, generatedTrees) = GeneratorTestHelper.RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error);
        Assert.Single(generatedTrees);

        var generated = generatedTrees[0].GetText().ToString();
        Assert.DoesNotContain("4096", generated);
    }

    [Fact]
    public void PseudoLocale_PreservesVariables()
    {
        var source = @"
using static MoonBuggy.Translate;

public class Program
{
    public static void Main()
    {
        var name = ""World"";
        var result = _t(""Hi $name$!"", new { name });
    }
}";
        var (diagnostics, generatedTrees) = GeneratorTestHelper.RunGenerator(
            source, pseudoLocale: true);

        Assert.DoesNotContain(diagnostics, d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error);

        var generated = generatedTrees[0].GetText().ToString();
        // Variable should be preserved, text accented
        // "Hi {name}!" → "Ḧï {name}!" → emitted as string.Concat("Ḧï ", args.name, "!")
        Assert.Contains("4096", generated);
        Assert.Contains("__args.name", generated);
    }

    [Fact]
    public void PseudoLocale_WithTranslations_AddsExtraBranch()
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
        var (diagnostics, generatedTrees) = GeneratorTestHelper.RunGenerator(
            source, pseudoLocale: true, additionalTexts: additionalText);

        Assert.DoesNotContain(diagnostics, d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error);

        var generated = generatedTrees[0].GetText().ToString();
        // Should have both Spanish and pseudo branches
        Assert.Contains("Hola", generated);
        Assert.Contains("4096", generated);
        Assert.Contains("Ḧëĺĺö", generated);
    }

    [Fact]
    public void PseudoLocale_MarkdownCall_PreservesPlaceholders()
    {
        var source = @"
using static MoonBuggy.Translate;

public class Program
{
    public static void Main()
    {
        var result = _m(""Click **here**"");
    }
}";
        var (diagnostics, generatedTrees) = GeneratorTestHelper.RunGenerator(
            source, pseudoLocale: true);

        Assert.DoesNotContain(diagnostics, d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error);

        var generated = generatedTrees[0].GetText().ToString();
        // Pseudo branch should contain accented text with HTML tags preserved
        Assert.Contains("4096", generated);
        // "Click <0>here</0>" → IcuTextNode "Click <strong>" + IcuTextNode "here" + IcuTextNode "</strong>"
        // After resolution, text is accented but HTML tags pass through
    }
}
