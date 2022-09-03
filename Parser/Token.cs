namespace CSharpSandbox.Parsing;

public sealed class Token
{
    public Pattern Pattern { get; }

    public string Lexeme { get; }

    public Token(Pattern pattern, string lexeme)
    {
        Pattern = pattern;
        Lexeme = lexeme;
    }
}