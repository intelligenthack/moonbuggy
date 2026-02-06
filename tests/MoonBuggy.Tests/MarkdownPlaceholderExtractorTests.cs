using MoonBuggy.Core.Markdown;

namespace MoonBuggy.Tests;

public class MarkdownPlaceholderExtractorTests
{
    [Fact]
    public void Extract_Bold()
    {
        var result = MarkdownPlaceholderExtractor.Extract("Click **here** to continue");

        Assert.Equal("Click <0>here</0> to continue", result.Text);
        Assert.Single(result.Mappings);
        Assert.Equal("<strong>", result.Mappings[0].OpenTag);
        Assert.Equal("</strong>", result.Mappings[0].CloseTag);
        Assert.Equal(0, result.Mappings[0].Index);
        Assert.Equal(1, result.NextIndex);
    }

    [Fact]
    public void Extract_Emphasis()
    {
        var result = MarkdownPlaceholderExtractor.Extract("Read *this* carefully");

        Assert.Equal("Read <0>this</0> carefully", result.Text);
        Assert.Single(result.Mappings);
        Assert.Equal("<em>", result.Mappings[0].OpenTag);
    }

    [Fact]
    public void Extract_Code()
    {
        var result = MarkdownPlaceholderExtractor.Extract("See the `code` example");

        Assert.Equal("See the <0>code</0> example", result.Text);
        Assert.Single(result.Mappings);
        Assert.Equal("<code>", result.Mappings[0].OpenTag);
        Assert.Equal("</code>", result.Mappings[0].CloseTag);
    }

    [Fact]
    public void Extract_Link()
    {
        var result = MarkdownPlaceholderExtractor.Extract("Click [here]({url})");

        Assert.Equal("Click <0>here</0>", result.Text);
        Assert.Single(result.Mappings);
        Assert.Equal("<a href=\"{url}\">", result.Mappings[0].OpenTag);
        Assert.Equal("</a>", result.Mappings[0].CloseTag);
    }

    [Fact]
    public void Extract_Multiple()
    {
        var result = MarkdownPlaceholderExtractor.Extract(
            "Read **this** and click [here]({url})");

        Assert.Equal("Read <0>this</0> and click <1>here</1>", result.Text);
        Assert.Equal(2, result.Mappings.Count);
        Assert.Equal("<strong>", result.Mappings[0].OpenTag);
        Assert.Equal("<a href=\"{url}\">", result.Mappings[1].OpenTag);
    }

    [Fact]
    public void Extract_Nested()
    {
        var result = MarkdownPlaceholderExtractor.Extract(
            "Click **[here]({url})** to continue");

        Assert.Equal("Click <0><1>here</1></0> to continue", result.Text);
        Assert.Equal(2, result.Mappings.Count);
        Assert.Equal("<strong>", result.Mappings[0].OpenTag);
        Assert.Equal("<a href=\"{url}\">", result.Mappings[1].OpenTag);
    }

    [Fact]
    public void Extract_VariableInFormatting()
    {
        var result = MarkdownPlaceholderExtractor.Extract("Hello **{name}**!");

        Assert.Equal("Hello <0>{name}</0>!", result.Text);
        Assert.Single(result.Mappings);
        Assert.Equal("<strong>", result.Mappings[0].OpenTag);
    }
}
