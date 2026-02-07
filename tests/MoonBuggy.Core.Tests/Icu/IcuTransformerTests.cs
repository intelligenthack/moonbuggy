using MoonBuggy.Core.Icu;

namespace MoonBuggy.Core.Tests.Icu;

public class IcuTransformerTests
{
    [Fact]
    public void Transform_PlainText_ReturnsUnchanged()
    {
        Assert.Equal("Save changes", IcuTransformer.ToIcu("Save changes"));
    }

    [Fact]
    public void Transform_SimpleVariable_ReturnsIcuBraces()
    {
        Assert.Equal("Welcome to {name}!", IcuTransformer.ToIcu("Welcome to $name$!"));
    }

    [Fact]
    public void Transform_MultipleVariables_ReturnsIcuBraces()
    {
        Assert.Equal("Page {num} of {total}", IcuTransformer.ToIcu("Page $num$ of $total$"));
    }

    [Fact]
    public void Transform_VariableOnly_ReturnsIcuBraces()
    {
        Assert.Equal("{name}", IcuTransformer.ToIcu("$name$"));
    }

    // 9.1.1
    [Fact]
    public void Transform_DollarEscape_ProducesLiteralDollar()
    {
        Assert.Equal("Price: $5", IcuTransformer.ToIcu("Price: $$5"));
    }

    // 9.1.2
    [Fact]
    public void Transform_DollarEscape_AroundWord()
    {
        Assert.Equal("$dollar$ sign", IcuTransformer.ToIcu("$$dollar$$ sign"));
    }

    // 9.1.3
    [Fact]
    public void Transform_DollarEscape_AroundName()
    {
        Assert.Equal("$marco$ was here", IcuTransformer.ToIcu("$$marco$$ was here"));
    }

    // 9.1.4
    [Fact]
    public void Transform_DollarEscape_MixedWithVariable()
    {
        Assert.Equal("Say {name}, costs $10", IcuTransformer.ToIcu("Say $name$, costs $$10"));
    }

    // 1.2.1
    [Fact]
    public void Transform_TwoFormPlural_RenderedSelector()
    {
        Assert.Equal(
            "You have {x, plural, one {# book} other {# books}}",
            IcuTransformer.ToIcu("You have $#x# book|#x# books$"));
    }

    // 1.2.2
    [Fact]
    public void Transform_TwoFormPlural_HiddenSelector()
    {
        Assert.Equal(
            "{y, plural, one {just one apple} other {many apples (#)!}}",
            IcuTransformer.ToIcu("$#~y#just one apple|many apples (#y#)!$"));
    }

    // 1.3.1
    [Fact]
    public void Transform_ThreeFormPlural_RenderedSelector()
    {
        Assert.Equal(
            "You have {x, plural, =0 {no books} one {# book} other {# books}}",
            IcuTransformer.ToIcu("You have $#x=0#no books|#x# book|#x# books$"));
    }

    // 1.3.2
    [Fact]
    public void Transform_ThreeFormPlural_HiddenSelector()
    {
        Assert.Equal(
            "{x, plural, =0 {no messages} one {one new message} other {# new messages}}",
            IcuTransformer.ToIcu("$#~x=0#no messages|one new message|#x# new messages$"));
    }

    // 9.2.1
    [Fact]
    public void Transform_HashEscapeInsidePlural()
    {
        Assert.Equal(
            "{x, plural, one {# item (#)} other {# items (#)}}",
            IcuTransformer.ToIcu("$#x# item (##)|#x# items (##)$"));
    }

    // 9.3.1
    [Fact]
    public void Transform_PipeEscapeInsidePlural()
    {
        Assert.Equal(
            "{x, plural, one {# a|b} other {# c|d}}",
            IcuTransformer.ToIcu("$#x# a||b|#x# c||d$"));
    }

    // 1.4.1
    [Fact]
    public void Transform_ComposedVariablesAndMultiplePlurals()
    {
        Assert.Equal(
            "Hi {name}, you have {x, plural, one {# book} other {# books}} and {y, plural, one {just one apple} other {many apples (#)!}}",
            IcuTransformer.ToIcu("Hi $name$, you have $#x# book|#x# books$ and $#~y#just one apple|many apples (#y#)!$"));
    }

    // 10.2.1
    [Fact]
    public void Transform_VariableInsidePluralForms()
    {
        Assert.Equal(
            "{x, plural, one {# book by {author}} other {# books by {author}}}",
            IcuTransformer.ToIcu("$#x# book by $author$|#x# books by $author$$"));
    }

    // 10.1.1 — hash outside plural is literal text
    [Fact]
    public void Transform_HashOutsidePlural_IsLiteralText()
    {
        Assert.Equal("You have #x# items", IcuTransformer.ToIcu("You have #x# items"));
    }

    // 10.4.1 — empty message throws
    [Fact]
    public void Transform_EmptyMessage_Throws()
    {
        Assert.Throws<FormatException>(() => IcuTransformer.ToIcu(""));
    }

    // MB0005 — unmatched dollar throws
    [Fact]
    public void Transform_UnmatchedDollar_Throws()
    {
        Assert.Throws<FormatException>(() => IcuTransformer.ToIcu("Unmatched $dollar"));
    }
}
