using System.IO;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;

namespace MoonBuggy.Tests;

public class TranslatedStringTests
{
    [Fact]
    public void SingleSegment_ToString_ReturnsValue()
    {
        var ts = new TranslatedString("hello");
        Assert.Equal("hello", ts.ToString());
    }

    [Fact]
    public void MultiSegment_ToString_ConcatenatesParts()
    {
        var ts = new TranslatedString(
            new string?[] { "a", "b" },
            new bool[] { false, false });
        Assert.Equal("ab", ts.ToString());
    }

    [Fact]
    public void ImplicitToString_Works()
    {
        string s = new TranslatedString("hello");
        Assert.Equal("hello", s);
    }

    [Fact]
    public void WriteTo_MultiSegment_EncodesVariableParts()
    {
        var ts = new TranslatedString(
            new string?[] { "<p>", "<script>" },
            new bool[] { false, true });

        var sw = new StringWriter();
        ((IHtmlContent)ts).WriteTo(sw, HtmlEncoder.Default);

        Assert.Equal("<p>&lt;script&gt;", sw.ToString());
    }

    [Fact]
    public void WriteTo_SingleSegment_WritesDirectly()
    {
        var ts = new TranslatedString("hello");

        var sw = new StringWriter();
        ((IHtmlContent)ts).WriteTo(sw, HtmlEncoder.Default);

        Assert.Equal("hello", sw.ToString());
    }

    [Fact]
    public void MultiSegment_WithNullPart_SkipsNull()
    {
        var ts = new TranslatedString(
            new string?[] { "a", null, "b" },
            new bool[] { false, false, false });
        Assert.Equal("ab", ts.ToString());
    }

    [Fact]
    public void Empty_ToString_ReturnsEmpty()
    {
        var ts = new TranslatedString("");
        Assert.Equal("", ts.ToString());
    }

    [Fact]
    public void Default_ToString_ReturnsEmpty()
    {
        var ts = default(TranslatedString);
        Assert.Equal("", ts.ToString());
    }

    [Fact]
    public void WriteTo_MultiSegment_EncodesOnlyFlaggedParts()
    {
        var ts = new TranslatedString(
            new string?[] { "Hello ", "<b>world</b>", "!" },
            new bool[] { false, true, false });

        var sw = new StringWriter();
        ((IHtmlContent)ts).WriteTo(sw, HtmlEncoder.Default);

        Assert.Equal("Hello &lt;b&gt;world&lt;/b&gt;!", sw.ToString());
    }
}
