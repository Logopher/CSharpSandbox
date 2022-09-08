using System.Diagnostics.CodeAnalysis;

namespace CSharpSandbox.Parsing;

public sealed class Token : IParseNode
{
    public Pattern Rule { get; }

    IRule IParseNode.Rule => Rule;

    public string Lexeme { get; }

    private readonly Dictionary<IRule, Match> _matchingRules = new();

    public NodeType NodeType { get; } = NodeType.Token;

    public string this[Range range] => ToString()[range];

    internal Token(Pattern pattern, string lexeme)
    {
        Rule = pattern;
        Lexeme = lexeme;
    }

    internal bool TryGetCachedMatch(IRule rule, [NotNullWhen(true)] out Match? match) => _matchingRules.TryGetValue(rule, out match);

    internal void AddMatchingRule(IRule rule, IParseNode node, int count) => _matchingRules.Add(rule, new(node, count));

    public override string ToString() => Lexeme;

    public static implicit operator string(Token token) => token.ToString();

    internal class Match
    {
        public IParseNode Node { get; }

        public int Index { get; }

        public Match(IParseNode node, int index)
        {
            Node = node;
            Index = index;
        }
    }
}