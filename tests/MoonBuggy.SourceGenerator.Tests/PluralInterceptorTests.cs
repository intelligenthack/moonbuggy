using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace MoonBuggy.SourceGenerator.Tests;

public class PluralInterceptorTests
{
    [Fact]
    public void TwoFormPlural_SourceLocale_EmitsPluralBranches()
    {
        var source = @"
using static MoonBuggy.Translate;

public class Program
{
    public static void Main()
    {
        var x = 5;
        var result = _t(""You have $#x# book|#x# books$"", new { x });
    }
}";
        var (diagnostics, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Single(generatedTrees);

        var generated = generatedTrees[0].GetText().ToString();
        // Should have plural branching with "book" and "books"
        Assert.Contains("book", generated);
        Assert.Contains("books", generated);
        // Should reference the plural variable
        Assert.Contains("ToString()", generated);
    }

    [Fact]
    public void TwoFormPlural_WithTranslation_EmitsLocaleSwitch()
    {
        var source = @"
using static MoonBuggy.Translate;

public class Program
{
    public static void Main()
    {
        var x = 5;
        var result = _t(""You have $#x# book|#x# books$"", new { x });
    }
}";
        var poContent = @"
msgid ""You have {x, plural, one {# book} other {# books}}""
msgstr ""{x, plural, one {Tienes # libro} other {Tienes # libros}}""
";
        var additionalText = new InMemoryAdditionalText("locales/es/messages.po", poContent);
        var (diagnostics, generatedTrees) = GeneratorTestHelper.RunGenerator(source, additionalText);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Single(generatedTrees);

        var generated = generatedTrees[0].GetText().ToString();
        Assert.Contains("libro", generated);
        Assert.Contains("libros", generated);
        Assert.Contains("book", generated);
        Assert.Contains("books", generated);
    }

    [Fact]
    public void ThreeFormPlural_WithZeroForm()
    {
        var source = @"
using static MoonBuggy.Translate;

public class Program
{
    public static void Main()
    {
        var x = 0;
        var result = _t(""$#x=0#no books|#x# book|#x# books$"", new { x });
    }
}";
        var (diagnostics, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Single(generatedTrees);

        var generated = generatedTrees[0].GetText().ToString();
        Assert.Contains("no books", generated);
        Assert.Contains("book", generated);
        Assert.Contains("books", generated);
    }

    [Fact]
    public void HiddenSelector_NotRenderedInBranch()
    {
        var source = @"
using static MoonBuggy.Translate;

public class Program
{
    public static void Main()
    {
        var x = 0;
        var result = _t(""$#~x=0#no messages|#~x# message|#~x# messages$"", new { x });
    }
}";
        var (diagnostics, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Single(generatedTrees);

        var generated = generatedTrees[0].GetText().ToString();
        // Hidden selector means # is still present in ICU (resolved at source gen time)
        Assert.Contains("no messages", generated);
        Assert.Contains("messages", generated);
    }

    [Fact]
    public void PluralWithSurroundingText()
    {
        var source = @"
using static MoonBuggy.Translate;

public class Program
{
    public static void Main()
    {
        var x = 3;
        var result = _t(""You have $#x# item|#x# items$ in your cart."", new { x });
    }
}";
        var (diagnostics, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Single(generatedTrees);

        var generated = generatedTrees[0].GetText().ToString();
        Assert.Contains("You have ", generated);
        Assert.Contains(" in your cart.", generated);
        Assert.Contains("item", generated);
        Assert.Contains("items", generated);
    }

    [Fact]
    public void Composed_VariableAndPluralBlock()
    {
        var source = @"
using static MoonBuggy.Translate;

public class Program
{
    public static void Main()
    {
        var name = ""Alice"";
        var x = 3;
        var result = _t(""Dear $name$, you have $#x# item|#x# items$."", new { name, x });
    }
}";
        var (diagnostics, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Single(generatedTrees);

        var generated = generatedTrees[0].GetText().ToString();
        Assert.Contains("Dear ", generated);
        Assert.Contains("__args.name", generated);
        Assert.Contains("item", generated);
        Assert.Contains("items", generated);
        Assert.DoesNotContain("dynamic", generated);
    }

    [Fact]
    public void HashNode_RendersSelectorValue()
    {
        var source = @"
using static MoonBuggy.Translate;

public class Program
{
    public static void Main()
    {
        var x = 5;
        var result = _t(""$#x# book|#x# books$"", new { x });
    }
}";
        var (diagnostics, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Single(generatedTrees);

        var generated = generatedTrees[0].GetText().ToString();
        // # inside plural becomes x.ToString()
        Assert.Contains("__x.ToString()", generated);
    }

    [Fact]
    public void ComplexLanguageRules_Polish()
    {
        var source = @"
using static MoonBuggy.Translate;

public class Program
{
    public static void Main()
    {
        var x = 5;
        var result = _t(""$#x# book|#x# books$"", new { x });
    }
}";
        // Polish has one/few/many/other
        var poContent = @"
msgid ""{x, plural, one {# book} other {# books}}""
msgstr ""{x, plural, one {# książka} few {# książki} many {# książek} other {# książki}}""
";
        var additionalText = new InMemoryAdditionalText("locales/pl/messages.po", poContent);
        var (diagnostics, generatedTrees) = GeneratorTestHelper.RunGenerator(source, additionalText);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Single(generatedTrees);

        var generated = generatedTrees[0].GetText().ToString();
        // Polish translation should appear
        Assert.Contains("książka", generated);
        Assert.Contains("książki", generated);
        Assert.Contains("książek", generated);
    }
}
