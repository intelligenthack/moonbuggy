using System.IO;
using Microsoft.AspNetCore.Html;
using static MoonBuggy.Translate;

namespace MoonBuggy.Tests;

public class TranslateTests
{
    private static string ToHtml(IHtmlContent content)
    {
        using var writer = new StringWriter();
        content.WriteTo(writer, System.Text.Encodings.Web.HtmlEncoder.Default);
        return writer.ToString();
    }

    // 3.1 Simple variables

    [Fact]
    public void T_SimpleVariable_ResolvesFromArgs() // 3.1.1
    {
        var result = _t("Welcome to $name$!", new { name = "Acme" });
        Assert.Equal("Welcome to Acme!", result);
    }

    [Fact]
    public void T_MultipleVariables_ResolvesAll() // 3.1.2
    {
        var result = _t("Page $num$ of $total$", new { num = 2, total = 10 });
        Assert.Equal("Page 2 of 10", result);
    }

    [Fact]
    public void T_NoVariables_ReturnsPlainText() // 3.1.3
    {
        var result = _t("Save changes");
        Assert.Equal("Save changes", result);
    }

    // 3.2 Two-form plurals

    [Fact]
    public void T_TwoFormPlural_RenderedSelector_One() // 3.2.1
    {
        var result = _t("You have $#x# book|#x# books$", new { x = 1 });
        Assert.Equal("You have 1 book", result);
    }

    [Fact]
    public void T_TwoFormPlural_RenderedSelector_Other() // 3.2.2
    {
        var result = _t("You have $#x# book|#x# books$", new { x = 3 });
        Assert.Equal("You have 3 books", result);
    }

    [Fact]
    public void T_TwoFormPlural_HiddenSelector_One() // 3.2.3
    {
        var result = _t("$#~y#just one apple|many apples (#y#)!$", new { y = 1 });
        Assert.Equal("just one apple", result);
    }

    [Fact]
    public void T_TwoFormPlural_HiddenSelector_Other() // 3.2.4
    {
        var result = _t("$#~y#just one apple|many apples (#y#)!$", new { y = 7 });
        Assert.Equal("many apples (7)!", result);
    }

    // 3.3 Three-form plurals

    [Fact]
    public void T_ThreeFormPlural_Zero() // 3.3.1
    {
        var result = _t("You have $#x=0#no books|#x# book|#x# books$", new { x = 0 });
        Assert.Equal("You have no books", result);
    }

    [Fact]
    public void T_ThreeFormPlural_One() // 3.3.2
    {
        var result = _t("You have $#x=0#no books|#x# book|#x# books$", new { x = 1 });
        Assert.Equal("You have 1 book", result);
    }

    [Fact]
    public void T_ThreeFormPlural_Other() // 3.3.3
    {
        var result = _t("You have $#x=0#no books|#x# book|#x# books$", new { x = 5 });
        Assert.Equal("You have 5 books", result);
    }

    [Fact]
    public void T_ThreeFormPlural_HiddenSelector_Zero() // 3.3.4
    {
        var result = _t("$#~x=0#no messages|one new message|#x# new messages$", new { x = 0 });
        Assert.Equal("no messages", result);
    }

    [Fact]
    public void T_ThreeFormPlural_HiddenSelector_One() // 3.3.5
    {
        var result = _t("$#~x=0#no messages|one new message|#x# new messages$", new { x = 1 });
        Assert.Equal("one new message", result);
    }

    [Fact]
    public void T_ThreeFormPlural_HiddenSelector_Other() // 3.3.6
    {
        var result = _t("$#~x=0#no messages|one new message|#x# new messages$", new { x = 5 });
        Assert.Equal("5 new messages", result);
    }

    // 3.4 Composed

    [Fact]
    public void T_Composed_MultipleVariablesAndPlurals_Case1() // 3.4.1
    {
        var result = _t(
            "Hi $name$, you have $#x# book|#x# books$ and $#~y#just one apple|many apples (#y#)!$",
            new { name = "Alice", x = 3, y = 1 });
        Assert.Equal("Hi Alice, you have 3 books and just one apple", result);
    }

    [Fact]
    public void T_Composed_MultipleVariablesAndPlurals_Case2() // 3.4.2
    {
        var result = _t(
            "Hi $name$, you have $#x# book|#x# books$ and $#~y#just one apple|many apples (#y#)!$",
            new { name = "Alice", x = 1, y = 7 });
        Assert.Equal("Hi Alice, you have 1 book and many apples (7)!", result);
    }

    // 4.1 _m() — simple markdown

    [Fact]
    public void M_SimpleMarkdown_RendersHtml() // 4.1.1
    {
        var result = _m("Click **here** to continue");
        Assert.Equal("Click <strong>here</strong> to continue", ToHtml(result));
    }

    // 4.2 _m() — markdown with variables

    [Fact]
    public void M_NestedBoldLink_RendersHtmlWithVariable() // 4.2.1
    {
        var result = _m("Click **[here]($url$)** to continue", new { url = "/page" });
        Assert.Equal("Click <strong><a href=\"/page\">here</a></strong> to continue", ToHtml(result));
    }
}
