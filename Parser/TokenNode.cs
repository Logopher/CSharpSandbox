namespace CSharpSandbox.Parsing;

public class TokenNode : IParseNode
{
    private readonly List<INamedRule> _matchingRules = new List<INamedRule>();

    public PatternRule Rule { get; }
    public Token Token { get; }

    IRule IParseNode.Rule => Rule;

    public NodeType NodeType { get; } = NodeType.Token;

    public string this[Range range] => ToString()[range];

    internal TokenNode(PatternRule rule, Token token)
    {
        Rule = rule;
        Token = token;

        _matchingRules.Add(Rule);
    }

    public bool HasMatchingRule(INamedRule rule) => _matchingRules.Contains(rule);

    public void AddMatchingRule(INamedRule rule) => _matchingRules.Add(rule);

    public override string ToString() => Token.ToString();

    public static implicit operator string(TokenNode node) => node.ToString();
}
