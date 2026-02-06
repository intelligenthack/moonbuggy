using MoonBuggy.Core.Parsing;

namespace MoonBuggy.Tests;

public class MbParserTests
{
    [Fact]
    public void Parse_PlainText_ReturnsSingleTextToken()
    {
        var tokens = MbParser.Parse("Save changes");

        var token = Assert.Single(tokens);
        var text = Assert.IsType<TextToken>(token);
        Assert.Equal("Save changes", text.Value);
    }

    [Fact]
    public void Parse_SimpleVariable_ReturnsTextAndVariableTokens()
    {
        var tokens = MbParser.Parse("Welcome to $name$!");

        Assert.Equal(3, tokens.Length);
        Assert.Equal(new TextToken("Welcome to "), tokens[0]);
        Assert.Equal(new VariableToken("name"), tokens[1]);
        Assert.Equal(new TextToken("!"), tokens[2]);
    }

    [Fact]
    public void Parse_TwoFormPlural_ReturnsPluralBlockToken()
    {
        var tokens = MbParser.Parse("You have $#x# book|#x# books$");

        Assert.Equal(2, tokens.Length);
        Assert.Equal(new TextToken("You have "), tokens[0]);

        var plural = Assert.IsType<PluralBlockToken>(tokens[1]);
        Assert.Equal("x", plural.SelectorVariable);
        Assert.True(plural.SelectorRendered);
        Assert.False(plural.HasZeroForm);
        Assert.Equal(2, plural.Forms.Length);

        Assert.Equal("one", plural.Forms[0].Category);
        Assert.Equal("other", plural.Forms[1].Category);
    }
}
