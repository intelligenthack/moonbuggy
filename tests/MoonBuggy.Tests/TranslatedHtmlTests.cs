using System.IO;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;

namespace MoonBuggy.Tests;

public class TranslatedHtmlTests
{
    [Fact]
    public void SingleSegment_WriteTo_WritesDirectly()
    {
        var th = new TranslatedHtml("<p>hi</p>");

        var sw = new StringWriter();
        ((IHtmlContent)th).WriteTo(sw, HtmlEncoder.Default);

        Assert.Equal("<p>hi</p>", sw.ToString());
    }

    [Fact]
    public void MultiSegment_WriteTo_WritesAllParts()
    {
        var th = new TranslatedHtml(
            new string?[] { "<p>", "Alice", "</p>" });

        var sw = new StringWriter();
        ((IHtmlContent)th).WriteTo(sw, HtmlEncoder.Default);

        Assert.Equal("<p>Alice</p>", sw.ToString());
    }

    [Fact]
    public void MultiSegment_NoEncoding_HtmlPassesThrough()
    {
        var th = new TranslatedHtml(
            new string?[] { "<strong>", "<em>bold</em>", "</strong>" });

        var sw = new StringWriter();
        ((IHtmlContent)th).WriteTo(sw, HtmlEncoder.Default);

        Assert.Equal("<strong><em>bold</em></strong>", sw.ToString());
    }

    [Fact]
    public void SingleSegment_ToString_ReturnsValue()
    {
        var th = new TranslatedHtml("<p>hi</p>");
        Assert.Equal("<p>hi</p>", th.ToString());
    }

    [Fact]
    public void MultiSegment_ToString_ConcatenatesParts()
    {
        var th = new TranslatedHtml(
            new string?[] { "<p>", "hello", "</p>" });
        Assert.Equal("<p>hello</p>", th.ToString());
    }

    [Fact]
    public void MultiSegment_WithNullPart_SkipsNull()
    {
        var th = new TranslatedHtml(
            new string?[] { "<p>", null, "</p>" });

        var sw = new StringWriter();
        ((IHtmlContent)th).WriteTo(sw, HtmlEncoder.Default);

        Assert.Equal("<p></p>", sw.ToString());
    }

    [Fact]
    public void Empty_WriteTo_WritesEmpty()
    {
        var th = new TranslatedHtml("");

        var sw = new StringWriter();
        ((IHtmlContent)th).WriteTo(sw, HtmlEncoder.Default);

        Assert.Equal("", sw.ToString());
    }
}
