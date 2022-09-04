namespace CSharpSandbox.Parsing;

public class NamedRule : INamedRule
{
    internal readonly IParser _parser;

    public string Name { get; }

    public RuleSegment Rule { get; }

    internal NamedRule(IParser parser, string name, RuleSegment rule)
    {
        _parser = parser;

        Name = name;
        Rule = rule;
    }

    public override string ToString() => Rule.ToString();

    public string ToString(IParseNode node) => _parser.ToString(this, node);
}

public class LazyNamedRule : INamedRule
{
    private readonly IParser _parser;
    private INamedRule? _rule;

    public string Name { get; }

    public INamedRule Rule => _rule ??= _parser.GetRule(Name);

    public LazyNamedRule(IParser parser, string name)
    {
        _parser = parser;
        Name = name;
    }

    public override string ToString() => Rule.ToString();

    public string ToString(IParseNode node) => Rule.ToString(node);
}
