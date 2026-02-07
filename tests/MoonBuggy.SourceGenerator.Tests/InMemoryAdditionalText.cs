using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace MoonBuggy.SourceGenerator.Tests;

internal class InMemoryAdditionalText : AdditionalText
{
    private readonly string _text;

    public InMemoryAdditionalText(string path, string text)
    {
        Path = path;
        _text = text;
    }

    public override string Path { get; }

    public override SourceText? GetText(CancellationToken cancellationToken = default)
    {
        return SourceText.From(_text);
    }
}
