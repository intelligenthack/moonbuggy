using MoonBuggy.Core.Icu;
using MoonBuggy.Core.Parsing;

namespace MoonBuggy.Tests;

public class IcuParserTests
{
    [Fact]
    public void PlainText_ReturnsSingleTextNode()
    {
        var nodes = IcuParser.Parse("Hello world");
        var node = Assert.Single(nodes);
        var text = Assert.IsType<IcuTextNode>(node);
        Assert.Equal("Hello world", text.Value);
    }

    [Fact]
    public void SingleVariable_ReturnsTextVariableText()
    {
        var nodes = IcuParser.Parse("Hi {name}!");
        Assert.Equal(3, nodes.Length);
        Assert.Equal(new IcuTextNode("Hi "), nodes[0]);
        Assert.Equal(new IcuVariableNode("name"), nodes[1]);
        Assert.Equal(new IcuTextNode("!"), nodes[2]);
    }

    [Fact]
    public void MultipleVariables_ReturnsFourNodes()
    {
        var nodes = IcuParser.Parse("Page {num} of {total}");
        Assert.Equal(4, nodes.Length);
        Assert.Equal(new IcuTextNode("Page "), nodes[0]);
        Assert.Equal(new IcuVariableNode("num"), nodes[1]);
        Assert.Equal(new IcuTextNode(" of "), nodes[2]);
        Assert.Equal(new IcuVariableNode("total"), nodes[3]);
    }

    [Fact]
    public void TwoFormPlural_ReturnsPluralNodeWithTwoBranches()
    {
        var nodes = IcuParser.Parse("{x, plural, one {# book} other {# books}}");
        var node = Assert.Single(nodes);
        var plural = Assert.IsType<IcuPluralNode>(node);
        Assert.Equal("x", plural.Variable);
        Assert.Equal(2, plural.Branches.Length);

        Assert.Equal("one", plural.Branches[0].Category);
        Assert.Equal(2, plural.Branches[0].Content.Length);
        Assert.IsType<IcuHashNode>(plural.Branches[0].Content[0]);
        Assert.Equal(new IcuTextNode(" book"), plural.Branches[0].Content[1]);

        Assert.Equal("other", plural.Branches[1].Category);
        Assert.Equal(2, plural.Branches[1].Content.Length);
        Assert.IsType<IcuHashNode>(plural.Branches[1].Content[0]);
        Assert.Equal(new IcuTextNode(" books"), plural.Branches[1].Content[1]);
    }

    [Fact]
    public void ThreeFormPlural_WithZeroForm()
    {
        var nodes = IcuParser.Parse("{x, plural, =0 {no books} one {# book} other {# books}}");
        var node = Assert.Single(nodes);
        var plural = Assert.IsType<IcuPluralNode>(node);
        Assert.Equal("x", plural.Variable);
        Assert.Equal(3, plural.Branches.Length);

        Assert.Equal("=0", plural.Branches[0].Category);
        Assert.Equal(new IcuTextNode("no books"), Assert.Single(plural.Branches[0].Content));

        Assert.Equal("one", plural.Branches[1].Category);
        Assert.Equal("other", plural.Branches[2].Category);
    }

    [Fact]
    public void NestedVariableInPlural()
    {
        var nodes = IcuParser.Parse("{x, plural, one {# item for {name}} other {# items for {name}}}");
        var node = Assert.Single(nodes);
        var plural = Assert.IsType<IcuPluralNode>(node);

        // one branch: # + " item for " + {name}
        Assert.Equal(3, plural.Branches[0].Content.Length);
        Assert.IsType<IcuHashNode>(plural.Branches[0].Content[0]);
        Assert.Equal(new IcuTextNode(" item for "), plural.Branches[0].Content[1]);
        Assert.Equal(new IcuVariableNode("name"), plural.Branches[0].Content[2]);

        // other branch: # + " items for " + {name}
        Assert.Equal(3, plural.Branches[1].Content.Length);
        Assert.IsType<IcuHashNode>(plural.Branches[1].Content[0]);
        Assert.Equal(new IcuTextNode(" items for "), plural.Branches[1].Content[1]);
        Assert.Equal(new IcuVariableNode("name"), plural.Branches[1].Content[2]);
    }

    [Fact]
    public void MarkdownPlaceholders_TreatedAsLiteralText()
    {
        var nodes = IcuParser.Parse("Click <0>here</0>");
        // <0> and </0> are literal text from the ICU parser's perspective
        var node = Assert.Single(nodes);
        var text = Assert.IsType<IcuTextNode>(node);
        Assert.Equal("Click <0>here</0>", text.Value);
    }

    [Fact]
    public void RoundTrip_SimpleVariable()
    {
        // MB → ICU → Parse preserves semantics
        var icu = IcuTransformer.ToIcu("Hello $name$!");
        Assert.Equal("Hello {name}!", icu);

        var nodes = IcuParser.Parse(icu);
        Assert.Equal(3, nodes.Length);
        Assert.Equal(new IcuTextNode("Hello "), nodes[0]);
        Assert.Equal(new IcuVariableNode("name"), nodes[1]);
        Assert.Equal(new IcuTextNode("!"), nodes[2]);
    }

    [Fact]
    public void RoundTrip_TwoFormPlural()
    {
        var icu = IcuTransformer.ToIcu("You have $#x# book|#x# books$");
        // ICU output: "You have {x, plural, one {# book} other {# books}}"
        var nodes = IcuParser.Parse(icu);
        Assert.Equal(2, nodes.Length);
        Assert.Equal(new IcuTextNode("You have "), nodes[0]);
        var plural = Assert.IsType<IcuPluralNode>(nodes[1]);
        Assert.Equal("x", plural.Variable);
        Assert.Equal(2, plural.Branches.Length);
    }

    [Fact]
    public void EmptyInput_ReturnsEmptyArray()
    {
        Assert.Empty(IcuParser.Parse(""));
    }

    [Fact]
    public void IcuEscaping_SingleQuoteLiteralCurly()
    {
        // '{' in ICU is escaped as '{'
        var nodes = IcuParser.Parse("Use '{' for braces");
        var node = Assert.Single(nodes);
        var text = Assert.IsType<IcuTextNode>(node);
        Assert.Equal("Use { for braces", text.Value);
    }

    [Fact]
    public void PluralWithSurroundingText()
    {
        var nodes = IcuParser.Parse("You have {x, plural, one {# book} other {# books}} in your cart.");
        Assert.Equal(3, nodes.Length);
        Assert.Equal(new IcuTextNode("You have "), nodes[0]);
        Assert.IsType<IcuPluralNode>(nodes[1]);
        Assert.Equal(new IcuTextNode(" in your cart."), nodes[2]);
    }

    [Fact]
    public void ComplexComposed_VariableAndTwoPluralBlocks()
    {
        var nodes = IcuParser.Parse("Dear {name}, you have {x, plural, one {# item} other {# items}} and {y, plural, one {# message} other {# messages}}.");
        Assert.Equal(7, nodes.Length);
        Assert.Equal(new IcuTextNode("Dear "), nodes[0]);
        Assert.Equal(new IcuVariableNode("name"), nodes[1]);
        Assert.Equal(new IcuTextNode(", you have "), nodes[2]);
        var plural1 = Assert.IsType<IcuPluralNode>(nodes[3]);
        Assert.Equal("x", plural1.Variable);
        Assert.Equal(new IcuTextNode(" and "), nodes[4]);
        var plural2 = Assert.IsType<IcuPluralNode>(nodes[5]);
        Assert.Equal("y", plural2.Variable);
        Assert.Equal(new IcuTextNode("."), nodes[6]);
    }
}
