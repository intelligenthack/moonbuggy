using MoonBuggy.CldrGen.Plural;

namespace MoonBuggy.CldrGen.Tests.Plural;

public class CSharpPluralEmitterTests
{
    [Fact]
    public void EmitCondition_SimpleEquality_ProducesCorrectCode()
    {
        // n = 1 → "n == 1"
        var expr = Parse("n = 1");
        var code = CSharpPluralEmitter.EmitCondition(expr);
        Assert.Equal("n == 1", code);
    }

    [Fact]
    public void EmitCondition_Range_ProducesCorrectCode()
    {
        // n % 100 = 3..10 → "n % 100 >= 3 && n % 100 <= 10"
        var expr = Parse("n % 100 = 3..10");
        var code = CSharpPluralEmitter.EmitCondition(expr);
        Assert.Equal("n % 100 >= 3 && n % 100 <= 10", code);
    }

    [Fact]
    public void EmitCondition_NegatedRange_ProducesCorrectCode()
    {
        // n % 100 != 12..14 → "(n % 100 < 12 || n % 100 > 14)"
        var expr = Parse("n % 100 != 12..14");
        var code = CSharpPluralEmitter.EmitCondition(expr);
        Assert.Equal("(n % 100 < 12 || n % 100 > 14)", code);
    }

    [Fact]
    public void EmitCondition_AndChain_ProducesCorrectCode()
    {
        // Already simplified: n % 10 = 2..4 and n % 100 != 12..14
        var expr = Parse("n % 10 = 2..4 and n % 100 != 12..14");
        var code = CSharpPluralEmitter.EmitCondition(expr);
        Assert.Equal("n % 10 >= 2 && n % 10 <= 4 && (n % 100 < 12 || n % 100 > 14)", code);
    }

    [Fact]
    public void EmitCondition_CommaValues_ProducesCorrectCode()
    {
        // n = 0,1 → "(n == 0 || n == 1)"
        var expr = Parse("n = 0,1");
        var code = CSharpPluralEmitter.EmitCondition(expr);
        Assert.Equal("(n == 0 || n == 1)", code);
    }

    [Fact]
    public void EmitCondition_NegatedCommaValues_ProducesCorrectCode()
    {
        // n % 100 != 11,71,91
        var expr = Parse("n % 100 != 11,71,91");
        var code = CSharpPluralEmitter.EmitCondition(expr);
        Assert.Equal("(n % 100 != 11 && n % 100 != 71 && n % 100 != 91)", code);
    }

    [Fact]
    public void EmitCondition_OrBranches_ProducesCorrectCode()
    {
        // n % 10 = 0 or n % 10 = 5..9
        var expr = Parse("n % 10 = 0 or n % 10 = 5..9");
        var code = CSharpPluralEmitter.EmitCondition(expr);
        Assert.Equal("n % 10 == 0 || n % 10 >= 5 && n % 10 <= 9", code);
    }

    [Fact]
    public void EmitCondition_OrWithMultipleAndChains_Parenthesizes()
    {
        // "n % 10 = 1 and n % 100 != 11 or n % 10 = 2 and n % 100 != 12"
        var expr = Parse("n % 10 = 1 and n % 100 != 11 or n % 10 = 2 and n % 100 != 12");
        var code = CSharpPluralEmitter.EmitCondition(expr);
        Assert.Equal("(n % 10 == 1 && n % 100 != 11) || (n % 10 == 2 && n % 100 != 12)", code);
    }

    [Fact]
    public void EmitCondition_Null_ReturnsNull()
    {
        Assert.Null(CSharpPluralEmitter.EmitCondition(null));
    }

    [Fact]
    public void EmitCondition_MixedCommaAndRange_ProducesCorrectCode()
    {
        // n % 10 = 3..4,9 → "(n % 10 >= 3 && n % 10 <= 4 || n % 10 == 9)"
        var expr = Parse("n % 10 = 3..4,9");
        var code = CSharpPluralEmitter.EmitCondition(expr);
        Assert.Equal("((n % 10 >= 3 && n % 10 <= 4) || n % 10 == 9)", code);
    }

    // Helper: parse + simplify for emitter tests
    private static OrExpr? Parse(string condition)
    {
        var ast = CldrRuleParser.Parse(condition);
        return IntegerSimplifier.Simplify(ast) ?? ast;
    }
}
