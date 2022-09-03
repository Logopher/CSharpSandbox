using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;

namespace CSharpSandbox.Parser;

public sealed class Pattern
{
    public Regex Regex { get; }

    public static Pattern FromLiteral(string s) => new(Regex.Escape(s));

    public Pattern(string regex)
    {
        Regex = new Regex($@"^\s*({regex})\s*");
    }

    public bool TryMatch(StringBuilder input, [NotNullWhen(true)] out Token? token)
    {
        token = null;

        var match = Regex.Match(input.ToString());
        if (match?.Success ?? false)
        {
            var text = match.Groups[1].Value;
            input.Remove(0, text.Length);
            token = new(this, text);
            return true;
        }

        return false;
    }
}
