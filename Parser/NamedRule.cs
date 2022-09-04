namespace CSharpSandbox.Parsing;

internal class NamedRule : INamedRule
{
    internal readonly IParser _parser;

    public string Name { get; }

    public RuleSegment Rule { get; }

    public NamedRule(IParser parser, string name, RuleSegment rule)
    {
        _parser = parser;

        Name = name;
        Rule = rule;
    }

    public override string ToString() => Rule.ToString();

    public string ToString(IParseNode node) => _parser.ToString(this, node);
}

internal class NameRule : INamedRule
{
    private readonly IParser _parser;
    private INamedRule? _rule;

    public string Name { get; }

    public INamedRule Rule => _rule ??= _parser.GetRule(Name);

    public NameRule(IParser parser, string name)
    {
        _parser = parser;
        Name = name;
    }

    public override string ToString() => Rule.ToString();

    public string ToString(IParseNode node) => Rule.ToString(node);
}
