using MoonBuggy.CldrGen.Plural;

namespace MoonBuggy.CldrGen.Tests.Plural;

public class IntegerSimplifierTests
{
    [Fact]
    public void Simplify_VEquals0_DropsFromAnd()
    {
        // "v = 0" is always true for integers → relation dropped, branch becomes empty
        var expr = CldrRuleParser.Parse("v = 0");
        var result = IntegerSimplifier.Simplify(expr);

        // Result is an OrExpr with one branch that has no relations (unconditional)
        Assert.NotNull(result);
        Assert.Single(result!.Branches);
        Assert.Empty(result.Branches[0].Relations);
    }

    [Fact]
    public void Simplify_VNotEquals0_KillsBranch()
    {
        // "v != 0" is always false for integers → dead branch
        var expr = CldrRuleParser.Parse("v != 0");
        var result = IntegerSimplifier.Simplify(expr);

        Assert.Null(result); // entire rule is dead
    }

    [Fact]
    public void Simplify_IReplacedWithN()
    {
        // "i = 1" → "n = 1"
        var expr = CldrRuleParser.Parse("i = 1");
        var result = IntegerSimplifier.Simplify(expr);

        Assert.NotNull(result);
        var rel = result!.Branches[0].Relations[0];
        Assert.Equal("n", rel.Operand);
    }

    [Fact]
    public void Simplify_ENotEquals0To5_IsDead()
    {
        // "e != 0..5" → false (0 is in range 0..5, so != fails)
        var expr = CldrRuleParser.Parse("e != 0..5");
        var result = IntegerSimplifier.Simplify(expr);

        Assert.Null(result); // dead
    }

    [Fact]
    public void Simplify_FullRule_IEquals1AndVEquals0()
    {
        // "i = 1 and v = 0" → n == 1 (v=0 dropped, i→n)
        var expr = CldrRuleParser.Parse("i = 1 and v = 0");
        var result = IntegerSimplifier.Simplify(expr);

        Assert.NotNull(result);
        Assert.Single(result!.Branches);
        Assert.Single(result.Branches[0].Relations);
        var rel = result.Branches[0].Relations[0];
        Assert.Equal("n", rel.Operand);
        Assert.Single(rel.Ranges);
        Assert.Equal(1, rel.Ranges[0].Low);
    }

    [Fact]
    public void Simplify_FullRule_NEquals1OrTNotEquals0AndI01()
    {
        // "n = 1 or t != 0 and i = 0,1"
        // Branch 1: n = 1 → kept as-is
        // Branch 2: t != 0 → dead (t=0 for int) → kills entire AND chain
        // Result: just n = 1
        var expr = CldrRuleParser.Parse("n = 1 or t != 0 and i = 0,1");
        var result = IntegerSimplifier.Simplify(expr);

        Assert.NotNull(result);
        Assert.Single(result!.Branches); // only first branch survives
        Assert.Single(result.Branches[0].Relations);
        Assert.Equal("n", result.Branches[0].Relations[0].Operand);
    }

    [Fact]
    public void Simplify_FEquals0_AlwaysTrue()
    {
        // "f = 0" → always true for integers → empty branch
        var expr = CldrRuleParser.Parse("f = 0");
        var result = IntegerSimplifier.Simplify(expr);
        Assert.NotNull(result);
        Assert.Single(result!.Branches);
        Assert.Empty(result.Branches[0].Relations);
    }

    [Fact]
    public void Simplify_FNotEquals0_AlwaysFalse()
    {
        // "f != 0" → always false for integers
        var expr = CldrRuleParser.Parse("f != 0");
        var result = IntegerSimplifier.Simplify(expr);
        Assert.Null(result); // dead
    }

    [Fact]
    public void Simplify_VEquals0AndIModEquals_SimplifiesCorrectly()
    {
        // "v = 0 and i % 10 = 1 and i % 100 != 11" (Bosnian-like)
        // → n % 10 == 1 and n % 100 != 11
        var expr = CldrRuleParser.Parse("v = 0 and i % 10 = 1 and i % 100 != 11");
        var result = IntegerSimplifier.Simplify(expr);

        Assert.NotNull(result);
        Assert.Single(result!.Branches);
        Assert.Equal(2, result.Branches[0].Relations.Count); // v=0 dropped

        Assert.Equal("n", result.Branches[0].Relations[0].Operand);
        Assert.Equal(10, result.Branches[0].Relations[0].Modulus);
        Assert.Equal("n", result.Branches[0].Relations[1].Operand);
        Assert.Equal(100, result.Branches[0].Relations[1].Modulus);
        Assert.True(result.Branches[0].Relations[1].Negated);
    }

    [Fact]
    public void Simplify_OrWithMixedLiveAndDead()
    {
        // "v = 0 and i % 10 = 1 or v != 0 and f % 10 = 1"
        // Branch 1: v=0 (drop) and i%10=1 → n%10=1 (live)
        // Branch 2: v!=0 (dead) → kill entire branch
        // Result: single branch n%10=1
        var expr = CldrRuleParser.Parse("v = 0 and i % 10 = 1 or v != 0 and f % 10 = 1");
        var result = IntegerSimplifier.Simplify(expr);

        Assert.NotNull(result);
        Assert.Single(result!.Branches);
        Assert.Single(result.Branches[0].Relations);
        Assert.Equal("n", result.Branches[0].Relations[0].Operand);
        Assert.Equal(10, result.Branches[0].Relations[0].Modulus);
    }

    [Fact]
    public void Simplify_Null_ReturnsNull()
    {
        // null input (unconditional "other") stays null
        Assert.Null(IntegerSimplifier.Simplify(null));
    }

    [Fact]
    public void Simplify_CatalanManyRule_DeadBecauseOfE()
    {
        // "e = 0 and i != 0 and i % 1000000 = 0 and v = 0 or e != 0..5"
        // Branch 1: e=0 (true) and i!=0 → n!=0, i%1000000=0 → n%1000000=0, v=0 (true) → live
        // Branch 2: e != 0..5 → false (0 is in 0..5) → dead
        var expr = CldrRuleParser.Parse("e = 0 and i != 0 and i % 1000000 = 0 and v = 0 or e != 0..5");
        var result = IntegerSimplifier.Simplify(expr);

        Assert.NotNull(result);
        Assert.Single(result!.Branches); // only first branch survives
        Assert.Equal(2, result.Branches[0].Relations.Count); // e=0, v=0 dropped
    }

    [Fact]
    public void Simplify_CebOne_Complex()
    {
        // "v = 0 and i = 1,2,3 or v = 0 and i % 10 != 4,6,9 or v != 0 and f % 10 != 4,6,9"
        // Branch 1: v=0 (drop) and i=1,2,3 → n=0,1,2,3 → live (i→n)
        // Branch 2: v=0 (drop) and i%10!=4,6,9 → n%10!=4,6,9 → live
        // Branch 3: v!=0 (dead) → kill
        var expr = CldrRuleParser.Parse("v = 0 and i = 1,2,3 or v = 0 and i % 10 != 4,6,9 or v != 0 and f % 10 != 4,6,9");
        var result = IntegerSimplifier.Simplify(expr);

        Assert.NotNull(result);
        Assert.Equal(2, result!.Branches.Count); // branches 1 and 2 survive
    }
}
