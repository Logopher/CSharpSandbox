namespace CSharpSandbox.Parser;

internal class TokenNode : IParseNode
{
    public PatternRule Rule { get; }
    public Token Token { get; }

    IRule IParseNode.Rule => Rule;

    public TokenNode(PatternRule rule, Token token)
    {
        Rule = rule;
        Token = token;
    }
}
