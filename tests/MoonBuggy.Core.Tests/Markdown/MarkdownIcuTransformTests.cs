using MoonBuggy.Core.Markdown;

namespace MoonBuggy.Core.Tests.Markdown;

public class MarkdownIcuTransformTests
{
    [Fact]
    public void ToIcu_SimpleBold()
    {
        var result = MarkdownPlaceholderExtractor.ToIcuWithMarkdown(
            "Click **here** to continue");

        Assert.Equal("Click <0>here</0> to continue", result);
    }

    [Fact]
    public void ToIcu_LinkWithVariable()
    {
        var result = MarkdownPlaceholderExtractor.ToIcuWithMarkdown(
            "Click [here]($url$)");

        Assert.Equal("Click <0>here</0>", result);
    }

    [Fact]
    public void ToIcu_NestedBoldLink()
    {
        var result = MarkdownPlaceholderExtractor.ToIcuWithMarkdown(
            "Click **[here]($url$)** to continue");

        Assert.Equal("Click <0><1>here</1></0> to continue", result);
    }

    [Fact]
    public void ToIcu_VariableInText()
    {
        var result = MarkdownPlaceholderExtractor.ToIcuWithMarkdown(
            "Hello **$name$**!");

        Assert.Equal("Hello <0>{name}</0>!", result);
    }

    [Fact]
    public void ToIcu_PluralWithMarkdown()
    {
        var result = MarkdownPlaceholderExtractor.ToIcuWithMarkdown(
            @"You have $#x=0#no **new** items|**#x#** new item|**#x#** new items$");

        Assert.Equal(
            "You have {x, plural, =0 {no <0>new</0> items} one {<1>#</1> new item} other {<2>#</2> new items}}",
            result);
    }

    [Fact]
    public void ToIcu_ComposedPluralMarkdown()
    {
        var result = MarkdownPlaceholderExtractor.ToIcuWithMarkdown(
            @"Hi **$name$**, you have $#x=0#no *new* items|*#x#* new item|*#x#* new items$");

        Assert.Equal(
            "Hi <0>{name}</0>, you have {x, plural, =0 {no <1>new</1> items} one {<2>#</2> new item} other {<3>#</3> new items}}",
            result);
    }

    [Fact]
    public void ToIcu_PlainTextPassesThrough()
    {
        var result = MarkdownPlaceholderExtractor.ToIcuWithMarkdown(
            "No markdown here");

        Assert.Equal("No markdown here", result);
    }
}
