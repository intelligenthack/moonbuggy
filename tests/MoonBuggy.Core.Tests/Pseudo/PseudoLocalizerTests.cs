using MoonBuggy.Core.Pseudo;

namespace MoonBuggy.Core.Tests.Pseudo;

public class PseudoLocalizerTests
{
    // 6.1 Accent mapping — ring above \u030A
    [Theory]
    [InlineData('a', "å")]
    [InlineData('u', "ů")]
    [InlineData('A', "Å")]
    [InlineData('U', "Ů")]
    public void Accent_RingAbove(char input, string expected)
    {
        Assert.Equal(expected, PseudoLocalizer.Accent(input));
    }

    // 6.1 Accent mapping — diaeresis \u0308
    [Theory]
    [InlineData('e', "ë")]
    [InlineData('i', "ï")]
    [InlineData('o', "ö")]
    [InlineData('h', "ḧ")]
    [InlineData('w', "ẅ")]
    [InlineData('x', "ẍ")]
    [InlineData('y', "ÿ")]
    [InlineData('E', "Ë")]
    [InlineData('I', "Ï")]
    [InlineData('O', "Ö")]
    [InlineData('H', "Ḧ")]
    [InlineData('W', "Ẅ")]
    [InlineData('X', "Ẍ")]
    [InlineData('Y', "Ÿ")]
    public void Accent_Diaeresis(char input, string expected)
    {
        Assert.Equal(expected, PseudoLocalizer.Accent(input));
    }

    // 6.1 Accent mapping — dot above \u0307
    [Theory]
    [InlineData('b', "ḃ")]
    [InlineData('d', "ḋ")]
    [InlineData('f', "ḟ")]
    [InlineData('B', "Ḃ")]
    [InlineData('D', "Ḋ")]
    [InlineData('F', "Ḟ")]
    [InlineData('Q', "Q̇")]
    public void Accent_DotAbove(char input, string expected)
    {
        Assert.Equal(expected, PseudoLocalizer.Accent(input));
    }

    // 6.1 Accent mapping — tilde \u0303
    [Theory]
    [InlineData('v', "ṽ")]
    [InlineData('V', "Ṽ")]
    public void Accent_Tilde(char input, string expected)
    {
        Assert.Equal(expected, PseudoLocalizer.Accent(input));
    }

    // 6.1 Accent mapping — cedilla \u0327
    [Theory]
    [InlineData('t', "ţ")]
    [InlineData('T', "Ţ")]
    public void Accent_Cedilla(char input, string expected)
    {
        Assert.Equal(expected, PseudoLocalizer.Accent(input));
    }

    // 6.1 Accent mapping — acute accent \u0301 (all other letters)
    [Theory]
    [InlineData('c', "ć")]
    [InlineData('g', "ǵ")]
    [InlineData('l', "ĺ")]
    [InlineData('m', "ḿ")]
    [InlineData('n', "ń")]
    [InlineData('C', "Ć")]
    [InlineData('G', "Ǵ")]
    [InlineData('L', "Ĺ")]
    [InlineData('M', "Ḿ")]
    [InlineData('N', "Ń")]
    public void Accent_AcuteAccent(char input, string expected)
    {
        Assert.Equal(expected, PseudoLocalizer.Accent(input));
    }

    // Non-letter characters return as-is
    [Theory]
    [InlineData('1')]
    [InlineData('!')]
    [InlineData(' ')]
    [InlineData('.')]
    public void Accent_NonLetter_ReturnsUnchanged(char input)
    {
        Assert.Equal(input.ToString(), PseudoLocalizer.Accent(input));
    }

    // 6.2.1 Full word accenting
    [Fact]
    public void Transform_HelloWorld_AccentsAllLetters()
    {
        Assert.Equal("Ḧëĺĺö ẅöŕĺḋ", PseudoLocalizer.Transform("Hello world"));
    }

    // 6.2.2 Digits and punctuation preserved
    [Fact]
    public void Transform_PreservesDigitsAndPunctuation()
    {
        // P→acute, a→ring, g→acute, e→diaeresis
        Assert.Equal("Ṕåǵë 42!", PseudoLocalizer.Transform("Page 42!"));
    }

    // 6.2.3 ICU variables preserved
    [Fact]
    public void Transform_PreservesIcuVariables()
    {
        Assert.Equal("Ḧï {name}", PseudoLocalizer.Transform("Hi {name}"));
    }

    // 6.2.4 Placeholders preserved
    [Fact]
    public void Transform_PreservesPlaceholders()
    {
        // C→acute, l→acute, i→diaeresis, c→acute, k→acute
        Assert.Equal("Ćĺïćḱ <0>ḧëŕë</0>", PseudoLocalizer.Transform("Click <0>here</0>"));
    }

    // ICU plural blocks preserved (variables inside not accented)
    [Fact]
    public void Transform_PreservesIcuPluralSyntax()
    {
        var input = "{count, plural, one {# item} other {# items}}";
        var result = PseudoLocalizer.Transform(input);
        Assert.Contains("{count, plural,", result);
        Assert.Contains("ïţëḿ", result);  // "item" accented
        Assert.Contains("ïţëḿś", result); // "items" accented
    }

    // Empty string
    [Fact]
    public void Transform_EmptyString_ReturnsEmpty()
    {
        Assert.Equal("", PseudoLocalizer.Transform(""));
    }

    // Multiple ICU variables
    [Fact]
    public void Transform_MultipleVariables_AllPreserved()
    {
        Assert.Equal("Ḧëĺĺö {first} åńḋ {last}!", PseudoLocalizer.Transform("Hello {first} and {last}!"));
    }
}
