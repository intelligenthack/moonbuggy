using MoonBuggy.CldrGen.Plural;

namespace MoonBuggy.CldrGen.Tests.Plural;

public class CldrRuleParserTests
{
    [Fact]
    public void Parse_SimpleEquality_ProducesCorrectAst()
    {
        // "n = 1" → single OR branch, single AND relation, equality
        var result = CldrRuleParser.Parse("n = 1");

        Assert.NotNull(result);
        Assert.Single(result!.Branches);
        Assert.Single(result.Branches[0].Relations);

        var rel = result.Branches[0].Relations[0];
        Assert.Equal("n", rel.Operand);
        Assert.Null(rel.Modulus);
        Assert.False(rel.Negated);
        Assert.Single(rel.Ranges);
        Assert.Equal(1, rel.Ranges[0].Low);
        Assert.True(rel.Ranges[0].IsSingle);
    }

    [Fact]
    public void Parse_ModuloWithRange_ProducesCorrectAst()
    {
        // "n % 100 = 3..10"
        var result = CldrRuleParser.Parse("n % 100 = 3..10");

        Assert.NotNull(result);
        var rel = result!.Branches[0].Relations[0];
        Assert.Equal("n", rel.Operand);
        Assert.Equal(100, rel.Modulus);
        Assert.False(rel.Negated);
        Assert.Single(rel.Ranges);
        Assert.Equal(3, rel.Ranges[0].Low);
        Assert.Equal(10, rel.Ranges[0].High);
        Assert.False(rel.Ranges[0].IsSingle);
    }

    [Fact]
    public void Parse_ModuloRangeAndExclusion_ProducesCorrectAst()
    {
        // "n % 10 = 2..4 and n % 100 != 12..14"
        var result = CldrRuleParser.Parse("n % 10 = 2..4 and n % 100 != 12..14");

        Assert.NotNull(result);
        Assert.Single(result!.Branches); // single AND chain
        Assert.Equal(2, result.Branches[0].Relations.Count);

        var rel1 = result.Branches[0].Relations[0];
        Assert.Equal(10, rel1.Modulus);
        Assert.False(rel1.Negated);
        Assert.Equal(2, rel1.Ranges[0].Low);
        Assert.Equal(4, rel1.Ranges[0].High);

        var rel2 = result.Branches[0].Relations[1];
        Assert.Equal(100, rel2.Modulus);
        Assert.True(rel2.Negated);
        Assert.Equal(12, rel2.Ranges[0].Low);
        Assert.Equal(14, rel2.Ranges[0].High);
    }

    [Fact]
    public void Parse_CommaValues_ProducesCorrectAst()
    {
        // "i = 0,1"
        var result = CldrRuleParser.Parse("i = 0,1");

        Assert.NotNull(result);
        var rel = result!.Branches[0].Relations[0];
        Assert.Equal("i", rel.Operand);
        Assert.Equal(2, rel.Ranges.Count);
        Assert.Equal(0, rel.Ranges[0].Low);
        Assert.Equal(1, rel.Ranges[1].Low);
    }

    [Fact]
    public void Parse_OrWithMultipleAndChains_ProducesCorrectAst()
    {
        // "v = 0 and i % 10 = 1 or v != 0 and f % 10 = 1"
        var result = CldrRuleParser.Parse("v = 0 and i % 10 = 1 or v != 0 and f % 10 = 1");

        Assert.NotNull(result);
        Assert.Equal(2, result!.Branches.Count);

        // First branch: v = 0 and i % 10 = 1
        Assert.Equal(2, result.Branches[0].Relations.Count);
        Assert.Equal("v", result.Branches[0].Relations[0].Operand);
        Assert.Equal("i", result.Branches[0].Relations[1].Operand);
        Assert.Equal(10, result.Branches[0].Relations[1].Modulus);

        // Second branch: v != 0 and f % 10 = 1
        Assert.Equal(2, result.Branches[1].Relations.Count);
        Assert.Equal("v", result.Branches[1].Relations[0].Operand);
        Assert.True(result.Branches[1].Relations[0].Negated);
        Assert.Equal("f", result.Branches[1].Relations[1].Operand);
    }

    [Fact]
    public void Parse_EmptyCondition_ReturnsNull()
    {
        // Empty string = unconditional ("other")
        Assert.Null(CldrRuleParser.Parse(""));
        Assert.Null(CldrRuleParser.Parse("   "));
    }

    [Fact]
    public void Parse_ConditionWithSamples_IgnoresSamples()
    {
        // The condition part is before @integer/@decimal
        var result = CldrRuleParser.Parse("n = 1 @integer 1 @decimal 1.0, 1.00");

        Assert.NotNull(result);
        var rel = result!.Branches[0].Relations[0];
        Assert.Equal("n", rel.Operand);
        Assert.Single(rel.Ranges);
        Assert.Equal(1, rel.Ranges[0].Low);
    }

    [Fact]
    public void Parse_NotEqualWithCommaAndRange_ProducesCorrectAst()
    {
        // "n % 100 != 11,71,91" — Breton language pattern
        var result = CldrRuleParser.Parse("n % 100 != 11,71,91");

        Assert.NotNull(result);
        var rel = result!.Branches[0].Relations[0];
        Assert.True(rel.Negated);
        Assert.Equal(3, rel.Ranges.Count);
        Assert.Equal(11, rel.Ranges[0].Low);
        Assert.Equal(71, rel.Ranges[1].Low);
        Assert.Equal(91, rel.Ranges[2].Low);
    }

    [Fact]
    public void Parse_CommaWithRanges_ProducesCorrectAst()
    {
        // "n % 10 = 3..4,9 and n % 100 != 10..19,70..79,90..99"
        var result = CldrRuleParser.Parse("n % 10 = 3..4,9 and n % 100 != 10..19,70..79,90..99");

        Assert.NotNull(result);
        var rel1 = result!.Branches[0].Relations[0];
        Assert.Equal(2, rel1.Ranges.Count); // 3..4 and 9
        Assert.Equal(3, rel1.Ranges[0].Low);
        Assert.Equal(4, rel1.Ranges[0].High);
        Assert.Equal(9, rel1.Ranges[1].Low);
        Assert.True(rel1.Ranges[1].IsSingle);

        var rel2 = result.Branches[0].Relations[1];
        Assert.True(rel2.Negated);
        Assert.Equal(3, rel2.Ranges.Count); // 10..19, 70..79, 90..99
    }

    [Fact]
    public void Parse_RangeEquality_ProducesCorrectAst()
    {
        // "n = 0..1"
        var result = CldrRuleParser.Parse("n = 0..1");

        Assert.NotNull(result);
        var rel = result!.Branches[0].Relations[0];
        Assert.Equal(0, rel.Ranges[0].Low);
        Assert.Equal(1, rel.Ranges[0].High);
    }
}
