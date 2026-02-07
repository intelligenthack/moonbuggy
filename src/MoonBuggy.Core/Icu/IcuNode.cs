namespace MoonBuggy.Core.Icu;

public abstract record IcuNode;

public sealed record IcuTextNode(string Value) : IcuNode;

public sealed record IcuVariableNode(string Name) : IcuNode;

public sealed record IcuPluralNode(string Variable, IcuPluralBranch[] Branches) : IcuNode;

public sealed record IcuPluralBranch(string Category, IcuNode[] Content);

public sealed record IcuHashNode() : IcuNode;
