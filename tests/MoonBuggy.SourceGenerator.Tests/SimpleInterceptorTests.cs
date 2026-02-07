using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace MoonBuggy.SourceGenerator.Tests;

public class SimpleInterceptorTests
{
    [Fact]
    public void NoArgs_SourceLocaleOnly_ReturnsLiteralString()
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
        var (diagnostics, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Single(generatedTrees);

        var generated = generatedTrees[0].GetText().ToString();
        // Interceptor should return the literal string
        Assert.Contains("\"Save changes\"", generated);
        Assert.Contains("InterceptsLocation", generated);
    }

    [Fact]
    public void SimpleVariable_EmitsStringConcat()
    {
        var source = @"
using static MoonBuggy.Translate;

public class Program
{
    public static void Main()
    {
        var name = ""World"";
        var result = _t(""Welcome to $name$!"", new { name });
    }
}";
        var (diagnostics, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Single(generatedTrees);

        var generated = generatedTrees[0].GetText().ToString();
        Assert.Contains("TranslatedString", generated);
        Assert.Contains("\"Welcome to \"", generated);
        Assert.Contains("\"!\"", generated);
        Assert.DoesNotContain("dynamic", generated);
    }

    [Fact]
    public void MultipleVariables_EmitsTranslatedString()
    {
        var source = @"
using static MoonBuggy.Translate;

public class Program
{
    public static void Main()
    {
        var num = 1;
        var total = 10;
        var result = _t(""Page $num$ of $total$"", new { num, total });
    }
}";
        var (diagnostics, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Single(generatedTrees);

        var generated = generatedTrees[0].GetText().ToString();
        Assert.Contains("TranslatedString", generated);
        Assert.Contains("\"Page \"", generated);
        Assert.Contains("\" of \"", generated);
    }

    [Fact]
    public void WithTranslation_EmitsLocaleSwitch()
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
        var poContent = @"
msgid ""Save changes""
msgstr ""Guardar cambios""
";
        var additionalText = new InMemoryAdditionalText("locales/es/messages.po", poContent);
        var (diagnostics, generatedTrees) = GeneratorTestHelper.RunGenerator(source, additionalText);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Single(generatedTrees);

        var generated = generatedTrees[0].GetText().ToString();
        Assert.Contains("\"Guardar cambios\"", generated);
        Assert.Contains("\"Save changes\"", generated);
        // Should have LCID-based locale switch
        Assert.Contains("lcid", generated);
    }

    [Fact]
    public void VariableTranslation_EmitsTranslatedConcat()
    {
        var source = @"
using static MoonBuggy.Translate;

public class Program
{
    public static void Main()
    {
        var name = ""World"";
        var result = _t(""Welcome to $name$!"", new { name });
    }
}";
        var poContent = @"
msgid ""Welcome to {name}!""
msgstr ""Bienvenido a {name}!""
";
        var additionalText = new InMemoryAdditionalText("locales/es/messages.po", poContent);
        var (diagnostics, generatedTrees) = GeneratorTestHelper.RunGenerator(source, additionalText);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Single(generatedTrees);

        var generated = generatedTrees[0].GetText().ToString();
        Assert.Contains("\"Bienvenido a \"", generated);
    }

    [Fact]
    public void EmptyMsgstr_FallsBackToSource()
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
        var poContent = @"
msgid ""Save changes""
msgstr """"
";
        var additionalText = new InMemoryAdditionalText("locales/es/messages.po", poContent);
        var (diagnostics, generatedTrees) = GeneratorTestHelper.RunGenerator(source, additionalText);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Single(generatedTrees);

        var generated = generatedTrees[0].GetText().ToString();
        // Empty translation means no locale branch for that locale/message
        // Source locale always present
        Assert.Contains("\"Save changes\"", generated);
    }

    [Fact]
    public void ContextDisambiguation_TwoEntriesSameMsgid()
    {
        var source = @"
using static MoonBuggy.Translate;

public class Program
{
    public static void Main()
    {
        var a = _t(""Open"", context: ""verb"");
        var b = _t(""Open"", context: ""adjective"");
    }
}";
        var poContent = @"
msgctxt ""verb""
msgid ""Open""
msgstr ""Abrir""

msgctxt ""adjective""
msgid ""Open""
msgstr ""Abierto""
";
        var additionalText = new InMemoryAdditionalText("locales/es/messages.po", poContent);
        var (diagnostics, generatedTrees) = GeneratorTestHelper.RunGenerator(source, additionalText);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Single(generatedTrees);

        var generated = generatedTrees[0].GetText().ToString();
        Assert.Contains("\"Abrir\"", generated);
        Assert.Contains("\"Abierto\"", generated);
    }

    [Fact]
    public void MB0001_NonConstantMessage_ReportsError()
    {
        var source = @"
using static MoonBuggy.Translate;

public class Program
{
    public static void Main()
    {
        var msg = ""hello"";
        var result = _t(msg);
    }
}";
        var (diagnostics, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        Assert.Contains(diagnostics, d => d.Id == "MB0001");
    }

    [Fact]
    public void MB0007_EmptyMessage_ReportsError()
    {
        var source = @"
using static MoonBuggy.Translate;

public class Program
{
    public static void Main()
    {
        var result = _t("""");
    }
}";
        var (diagnostics, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        Assert.Contains(diagnostics, d => d.Id == "MB0007");
    }

    [Fact]
    public void MB0008_NonConstantContext_ReportsError()
    {
        var source = @"
using static MoonBuggy.Translate;

public class Program
{
    public static void Main()
    {
        var ctx = ""verb"";
        var result = _t(""Open"", context: ctx);
    }
}";
        var (diagnostics, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        Assert.Contains(diagnostics, d => d.Id == "MB0008");
    }

    [Fact]
    public void ConstVariable_AcceptedAsMessage()
    {
        var source = @"
using static MoonBuggy.Translate;

public class Program
{
    public static void Main()
    {
        const string s = ""Hello"";
        var result = _t(s);
    }
}";
        var (diagnostics, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Single(generatedTrees);

        var generated = generatedTrees[0].GetText().ToString();
        Assert.Contains("\"Hello\"", generated);
    }

    [Fact]
    public void MarkdownInPlainText_TreatedAsLiteral()
    {
        var source = @"
using static MoonBuggy.Translate;

public class Program
{
    public static void Main()
    {
        var result = _t(""Click **here**"");
    }
}";
        var (diagnostics, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Single(generatedTrees);

        var generated = generatedTrees[0].GetText().ToString();
        // _t() does not process markdown — ** is literal text
        Assert.Contains("Click **here**", generated);
        Assert.DoesNotContain("<strong>", generated);
    }
}
