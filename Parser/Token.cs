using System.Diagnostics.CodeAnalysis;

namespace CSharpSandbox.Parsing;

public sealed class Token : IParseNode
{
    public Pattern Rule { get; }

    IRule IParseNode.Rule => Rule;

    public string Lexeme { get; }

    private readonly Dictionary<IRule, Tuple<IParseNode, int>> _matchingRules = new();

    public NodeType NodeType { get; } = NodeType.Token;

    public string this[Range range] => ToString()[range];

    internal Token(Pattern pattern, string lexeme)
    {
        Rule = pattern;
        Lexeme = lexeme;
    }

    public bool HasMatchingRule(IRule rule, [NotNullWhen(true)] out Tuple<IParseNode, int>? node) => _matchingRules.TryGetValue(rule, out node);

    public void AddMatchingRule(IRule rule, IParseNode node, int count) => _matchingRules.Add(rule, Tuple.Create(node, count));

    public override string ToString() => Lexeme;

    public static implicit operator string(Token token) => token.ToString();
}