using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;

namespace CSharpSandbox.Parsing;

public sealed class Pattern
{
    private readonly IParser _parser;
    private readonly ILogger _logger;

    public Regex Regexp { get; }

    internal static Pattern FromLiteral(IParser parser, string s, ILogger logger) => new(parser, Regex.Escape(s), logger);

    internal Pattern(IParser parser, string regexp, ILogger logger)
    {
        _parser = parser;
        _logger = logger;
        Regexp = new Regex($@"^({regexp})");
    }

    public bool TryMatch(StringBuilder input, [NotNullWhen(true)] out Token? token)
    {
        token = null;

        var match = Regexp.Match(input.ToString());
        _logger.LogTrace("pattern {Regex} match? {Input}", Regexp, input);
        var result = match?.Success ?? false;
        _logger.LogTrace("pattern {Regex} match {Result} {Input}", Regexp, result ? "PASSED" : "FAILED", input);
        if (result)
        {
            var text = match!.Groups[1].Value;
            input.Remove(0, match.Length);
            token = new(this, text);
            return true;
        }

        return false;
    }

    public override string ToString() => $"/{Regexp}/";
}
