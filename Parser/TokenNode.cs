namespace CSharpSandbox.Parsing;

public class TokenNode : IParseNode
{
    public PatternRule Rule { get; }
    public Token Token { get; }

    IRule IParseNode.Rule => Rule;

    public NodeType NodeType { get; } = NodeType.Token;

    public string this[Range range] => ToString()[range];

    internal TokenNode(PatternRule rule, Token token)
    {
        Rule = rule;
        Token = token;
    }

    public override string ToString() => Token.ToString();

    public static implicit operator string(TokenNode node) => node.ToString();
}
