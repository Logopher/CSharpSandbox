using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;

namespace CSharpSandbox.Parsing;

public sealed class Pattern
{
    public Regex Regexp { get; }

    public static Pattern FromLiteral(string s) => new(Regex.Escape(s));

    public Pattern(string regexp)
    {
        //regexp = Regex.Replace(regexp, @"\\(.)", "$1");
        Regexp = new Regex($@"^\s*({regexp})\s*");
    }

    public bool TryMatch(StringBuilder input, [NotNullWhen(true)] out Token? token)
    {
        token = null;

        var match = Regexp.Match(input.ToString());
        if (match?.Success ?? false)
        {
            var text = match.Groups[1].Value;
            input.Remove(0, match.Length);
            token = new(this, text);
            return true;
        }

        return false;
    }
}
