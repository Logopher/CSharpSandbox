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

    public override string ToString() => Name;

    public string ToString(IParseNode node) => _parser.ToString(this, node);
}

public class LazyNamedRule : INamedRule
{
    private readonly IParser _parser;
    private readonly Lazy<INamedRule> _lazy;

    public string Name { get; }

    public INamedRule Rule => _lazy.Value;

    public LazyNamedRule(IParser parser, string name)
    {
        _parser = parser;
        Name = name;
        _lazy = new Lazy<INamedRule>(() => parser.GetRule(name));
    }

    public override string ToString() => Rule.ToString();

    public string ToString(IParseNode node) => Rule.ToString(node);
}
