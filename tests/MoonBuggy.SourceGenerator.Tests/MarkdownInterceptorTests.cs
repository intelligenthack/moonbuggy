using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace MoonBuggy.SourceGenerator.Tests;

public class MarkdownInterceptorTests
{
    [Fact]
    public void SimpleBold_EmitsStrongTags()
    {
        var source = @"
using static MoonBuggy.Translate;

public class Program
{
    public static void Main()
    {
        var result = _m(""Click **here** to continue"");
    }
}";
        var (diagnostics, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Single(generatedTrees);

        var generated = generatedTrees[0].GetText().ToString();
        Assert.Contains("<strong>", generated);
        Assert.Contains("</strong>", generated);
        Assert.Contains("HtmlString", generated);
    }

    [Fact]
    public void LinkWithVariable_EmitsAnchorTag()
    {
        var source = @"
using static MoonBuggy.Translate;

public class Program
{
    public static void Main()
    {
        var url = ""https://example.com"";
        var result = _m(""Click [here]($url$)"", new { url });
    }
}";
        var (diagnostics, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Single(generatedTrees);

        var generated = generatedTrees[0].GetText().ToString();
        Assert.Contains("<a href=", generated);
        Assert.Contains("</a>", generated);
        Assert.Contains("HtmlString", generated);
    }

    [Fact]
    public void TranslatedWithPlaceholders_ResolvesCorrectly()
    {
        var source = @"
using static MoonBuggy.Translate;

public class Program
{
    public static void Main()
    {
        var result = _m(""Click **here** to continue"");
    }
}";
        var poContent = @"
msgid ""Click <0>here</0> to continue""
msgstr ""Haz clic <0>aquí</0> para continuar""
";
        var additionalText = new InMemoryAdditionalText("locales/es/messages.po", poContent);
        var (diagnostics, generatedTrees) = GeneratorTestHelper.RunGenerator(source, additionalText);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Single(generatedTrees);

        var generated = generatedTrees[0].GetText().ToString();
        // Spanish: resolved <0> to <strong>
        Assert.Contains("aquí", generated);
        Assert.Contains("<strong>", generated);
    }

    [Fact]
    public void MarkdownWithVariable_EmitsVariableInHtml()
    {
        var source = @"
using static MoonBuggy.Translate;

public class Program
{
    public static void Main()
    {
        var name = ""Alice"";
        var result = _m(""Hello **$name$**!"", new { name });
    }
}";
        var (diagnostics, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Single(generatedTrees);

        var generated = generatedTrees[0].GetText().ToString();
        Assert.Contains("<strong>", generated);
        Assert.Contains("__args.name", generated);
        Assert.Contains("HtmlString", generated);
    }

    [Fact]
    public void ContextWithMarkdown()
    {
        var source = @"
using static MoonBuggy.Translate;

public class Program
{
    public static void Main()
    {
        var result = _m(""Click **here**"", context: ""navigation"");
    }
}";
        var poContent = @"
msgctxt ""navigation""
msgid ""Click <0>here</0>""
msgstr ""Haz clic <0>aquí</0>""
";
        var additionalText = new InMemoryAdditionalText("locales/es/messages.po", poContent);
        var (diagnostics, generatedTrees) = GeneratorTestHelper.RunGenerator(source, additionalText);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Single(generatedTrees);

        var generated = generatedTrees[0].GetText().ToString();
        Assert.Contains("aquí", generated);
    }

    [Fact]
    public void FallbackOnEmptyTranslation_ReturnsSourceHtml()
    {
        var source = @"
using static MoonBuggy.Translate;

public class Program
{
    public static void Main()
    {
        var result = _m(""Click **here** to continue"");
    }
}";
        var poContent = @"
msgid ""Click <0>here</0> to continue""
msgstr """"
";
        var additionalText = new InMemoryAdditionalText("locales/es/messages.po", poContent);
        var (diagnostics, generatedTrees) = GeneratorTestHelper.RunGenerator(source, additionalText);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Single(generatedTrees);

        var generated = generatedTrees[0].GetText().ToString();
        // Source HTML still present
        Assert.Contains("<strong>", generated);
        Assert.Contains("here", generated);
    }

    [Fact]
    public void MultipleMdConstructs_BoldAndEmphasis()
    {
        var source = @"
using static MoonBuggy.Translate;

public class Program
{
    public static void Main()
    {
        var result = _m(""This is **bold** and *italic*"");
    }
}";
        var (diagnostics, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Single(generatedTrees);

        var generated = generatedTrees[0].GetText().ToString();
        Assert.Contains("<strong>", generated);
        Assert.Contains("<em>", generated);
    }
}
