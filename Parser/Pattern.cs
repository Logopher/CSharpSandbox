using NLog;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;

namespace CSharpSandbox.Parsing;

public sealed class Pattern : INamedRule
{
    private readonly IParser _parser;
    static readonly Logger CurrentLogger = LogManager.GetCurrentClassLogger();

    public string Name { get; }

    public IReadOnlyList<IRule> Rules { get; } = Array.Empty<IRule>();

    public Regex Regexp { get; }

    internal static Pattern FromLiteral(IParser parser, string name, string s) => new(parser, name, Regex.Escape(s));

    internal Pattern(IParser parser, string name, string regexp)
    {
        _parser = parser;

        Name = name;
        Regexp = new Regex($@"^({regexp})");
    }

    public bool TryMatch(StringBuilder input, [NotNullWhen(true)] out Token? token)
    {
        token = null;

        var match = Regexp.Match(input.ToString());
        CurrentLogger.Trace("PATTERN {Regex} MATCH? {Input}", Regexp, input);
        var result = match?.Success ?? false;
        CurrentLogger.Trace("PATTERN {Regex} MATCH {Result} {Input}", Regexp, result ? "PASSED" : "FAILED", input);
        if (result)
        {
            var text = match!.Groups[1].Value;
            input.Remove(0, match.Length);
            token = new(this, match.Index, text);
            return true;
        }

        return false;
    }

    public override string ToString() => Name;

    public string ToString(IParseNode parseNode) => (Token)parseNode;

    public override int GetHashCode() => Regexp.GetHashCode();

    public override bool Equals(object? obj) => (obj is Pattern p) && Regexp.Equals(p.Regexp);
}
