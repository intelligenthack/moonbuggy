namespace MoonBuggy.Core.Parsing;

public abstract record MbToken;

public sealed record TextToken(string Value) : MbToken;

public sealed record VariableToken(string Name) : MbToken;

public sealed record PluralBlockToken(
    string SelectorVariable,
    bool SelectorRendered,
    bool HasZeroForm,
    PluralForm[] Forms
) : MbToken;

public sealed record PluralSelectorRef(string Name) : MbToken;

public sealed record PluralForm(string Category, MbToken[] Content);
