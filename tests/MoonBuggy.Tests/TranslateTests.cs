using static MoonBuggy.Translate;

namespace MoonBuggy.Tests;

public class TranslateTests
{
    [Fact]
    public void T_ThrowsInvalidOperationException_WhenCalledWithoutSourceGenerator()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => _t("Hello"));
        Assert.Contains("source generator", ex.Message);
    }

    [Fact]
    public void M_ThrowsInvalidOperationException_WhenCalledWithoutSourceGenerator()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => _m("Hello"));
        Assert.Contains("source generator", ex.Message);
    }
}
