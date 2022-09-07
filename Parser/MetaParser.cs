using CSharpSandbox.Common;
using Microsoft.Extensions.Logging;

namespace CSharpSandbox.Parsing;

public interface IMetaParserFactory
{
    MetaParser<TParser, TResult> Create<TParser, TResult>(Func<IMetaParser, TParser> cstor)
        where TParser : Parser<TResult>;
}

public class MetaParserFactory : IMetaParserFactory
{
    private readonly ILogger<MetaParserFactory> _logger;

    public MetaParserFactory(ILogger<MetaParserFactory> logger)
    {
        _logger = logger;
    }

    public MetaParser<TParser, TResult> Create<TParser, TResult>(Func<IMetaParser, TParser> cstor)
        where TParser : Parser<TResult>
    {
        return new MetaParser<TParser, TResult>(cstor, _logger);
    }
}

public class MetaParser<TParser, TResult> : Parser<TParser>, IMetaParser_internal
    where TParser : Parser<TResult>
{
    private readonly Dictionary<INamedRule, Func<IParser, IParseNode, IRule>> _directory = new();
    private readonly Func<IMetaParser, TParser> _cstor;

    // This method is equivalent to Parser.Generate's `string grammar` argument, and also what makes the other work.
    // We can't pass a grammar until we can parse a grammar, which is the job of the MetaParser.
    // The grammar which the MetaParser is designed to parse is a modified EBNF (Extended Backus–Naur Form),
    // and the C# below closely mimics the resulting syntax.
    //
    // Eventually we should be able to produce a string describing the compiled grammar,
    // which can be inserted into this comment.
    internal void ApplyBootsrapGrammar()
    {
        // lazy, as in ZZZZZ
        INamedRule Z(string name) => GetLazyRule(name);

        // literals
        PatternRule L(string name, string s) => DefinePattern(name, Pattern.FromLiteral(this, s, Logger));

        // patterns
        PatternRule P(string name, string s) => DefinePattern(name, new Pattern(this, s, Logger));

        // rules
        INamedRule R(string name, RuleSegment rule)
        {
            DefineRule(name, rule);
            return GetRule(name);
        }

        // space
        var S = P(E.S, @"\s+");

        var literal = P(E.Literal, @"""(?:\\""|[^""])+""");
        var pattern = P(E.Pattern, @"/(?:\\/|[^/])+/");
        var name = P(E.Name, "[a-zA-Z_][a-zA-Z0-9_]*");
        var posInt = P("posInt", "[0-9]+");

        var assmt = L("assmt", "=");
        var comma = L("comma", ",");
        var stmtEnd = L("stmtEnd", ";");
        //var amp = L("amp", "&");
        var pipe = L("pipe", "|");
        var excl = L("excl", "!");
        var query = L("query", "?");
        var lParen = L("lParen", "(");
        var rParen = L("rParen", ")");
        var asterisk = L("asterisk", "*");
        var plus = L("plus", "+");
        var lCurly = L("lRange", "{");
        var rCurly = L("rRange", "}");

        var baseExpr = R(E.BaseExpr, Or(name, Z(E.ParenExpr)));
        var range = R("range", Or(And(lCurly, Option(S), posInt, Option(S), comma, Option(S), Option(posInt), Option(S), rCurly), And(lCurly, Option(S), Empty(), Empty(), comma, Option(S), posInt, Option(S), rCurly)));
        var repeatRange = R(E.RepeatRange, And(baseExpr, range));
        var repeat0 = R(E.Repeat0, And(baseExpr, asterisk));
        var repeat1 = R(E.Repeat1, And(baseExpr, plus));
        var option = R(E.Option, And(baseExpr, query));
        var notExpr = R(E.NotExpr, And(excl, baseExpr));
        var andExpr = R(E.AndExpr, And(Z(E.BaseExpr3), Repeat1(And(Option(S), Z(E.BaseExpr3)))));
        var orExpr = R(E.OrExpr, And(Z(E.BaseExpr2), Repeat1(And(Option(S), pipe, Option(S), Z(E.BaseExpr2)))));
        var operExpr = R(E.OperExpr, Or(notExpr, repeatRange, repeat0, repeat1, option));
        var baseExpr2 = R(E.BaseExpr2, Or(operExpr, baseExpr));
        var baseExpr3 = R(E.BaseExpr3, Or(orExpr, baseExpr2));
        var baseExpr4 = R(E.BaseExpr4, Or(andExpr, baseExpr3));
        R(E.ParenExpr, And(lParen, Option(S), baseExpr4, Option(S), rParen));

        var token = R(E.PatternRule, And(name, Option(S), assmt, Option(S), Or(literal, pattern), Option(S), stmtEnd));
        var rule = R(E.NamedRule, And(name, Option(S), assmt, Option(S), baseExpr4, Option(S), stmtEnd));

        var tokenSection = R("tokenSection", And(token, Repeat0(And(S, token))));
        var ruleSection = R("ruleSection", And(rule, Repeat0(And(S, rule))));

        R(E.Lexicon, And(Option(S), tokenSection, S, ruleSection, Option(S)));
    }

    internal MetaParser(Func<IMetaParser, TParser> cstor, ILogger logger)
        : base("lexicon", logger)
    {
        _cstor = cstor;

        ApplyBootsrapGrammar();

        DirectSyntax(E.PatternRule, ResolvePatternDeclaration);
        DirectSyntax(E.NamedRule, ResolveRuleDeclaration);
        DirectSyntax(E.Name, ResolveName);

        DirectSyntax(E.AndExpr, ResolveAnd);
        DirectSyntax(E.OrExpr, ResolveOr);
        DirectSyntax(E.NotExpr, ResolveNot);
        DirectSyntax(E.Option, ResolveOption);
        DirectSyntax(E.ParenExpr, ResolveParens);
        DirectSyntax(E.Repeat0, ResolveRepeat0);
        DirectSyntax(E.Repeat1, ResolveRepeat1);
        DirectSyntax(E.RepeatRange, ResolveRepeatRange);

        DirectSyntax(E.OperExpr, ResolveBaseExprN);
        DirectSyntax(E.BaseExpr, ResolveBaseExprN);
        DirectSyntax(E.BaseExpr2, ResolveBaseExprN);
        DirectSyntax(E.BaseExpr3, ResolveBaseExprN);
        DirectSyntax(E.BaseExpr4, ResolveBaseExprN);
    }

    PatternRule ResolvePatternDeclaration(IParser parser, IParseNode node)
        => ((ParseNode)node).Expand(new[] { 0 }, (TokenNode? name, IParseNode? _, TokenNode? assmt, IParseNode? _, ParseNode? valueNode, IParseNode? _, IParseNode? _, IParseNode[] _) =>
    {
        if (assmt!.ToString() != "=")
        {
            throw new Exception();
        }

        var value = (TokenNode)valueNode![0];

        return value.Rule.Name switch
        {
            E.Literal => parser.DefineLiteral(name!, value[1..^1]),
            E.Pattern => parser.DefinePattern(name!, value[1..^1]),
            _ => throw new Exception(),
        };
    });

    NamedRule ResolveRuleDeclaration(IParser parser, IParseNode node)
        => ((ParseNode)node).Expand(new[] { 0 }, (TokenNode? name, IParseNode? _, TokenNode? assmt, IParseNode? _, ParseNode? value, IParseNode? _, IParseNode? _, IParseNode[] _) =>
    {
        if (assmt!.ToString() != "=")
        {
            throw new Exception();
        }

        var rule = (RuleSegment)ResolveRule(parser, value!);

        return parser.DefineRule(name!, rule);
    });

    LazyNamedRule ResolveName(IParser parser, IParseNode node) => parser.GetLazyRule((TokenNode)node);

    IRule ResolveBaseExprN(IParser parser, IParseNode node)
    {
        var child = ((ParseNode)node).Get(0, 0);
        return Translate(parser, (INamedRule)child.Rule, child);
    }

    RuleSegment ResolveAnd(IParser parser, IParseNode node)
    {
        var nodes = ((ParseNode)node).ToList<IParseNode>(0);
        return RuleSegment.And(parser, nodes.Select(n => ResolveRule(parser, n)).ToArray());
    }

    RuleSegment ResolveOr(IParser parser, IParseNode node)
    {
        var nodes = ((ParseNode)node).ToList<IParseNode>(null, 3);
        return RuleSegment.Or(parser, nodes.Select(n => ResolveRule(parser, n)).ToArray());
    }

    RuleSegment ResolveNot(IParser parser, IParseNode node)
    {
        var pnode = (ParseNode)node;
        return RuleSegment.Not(parser, ResolveRule(parser, pnode.Single()));
    }

    RuleSegment ResolveOption(IParser parser, IParseNode node)
    {
        var pnode = (ParseNode)node;
        return RuleSegment.Option(parser, ResolveRule(parser, pnode.Single()));
    }

    IRule ResolveParens(IParser parser, IParseNode node)
    {
        var inner = ((ParseNode)node).Get(0, 2);
        return ResolveRule(parser, inner);
    }

    RuleSegment ResolveRepeat0(IParser parser, IParseNode node)
    {
        var inner = ((ParseNode)node).Get(0, 0);
        return RuleSegment.Repeat0(parser, ResolveRule(parser, inner));
    }

    RuleSegment ResolveRepeat1(IParser parser, IParseNode node)
    {
        var inner = ((ParseNode)node).Get(0, 0);
        return RuleSegment.Repeat1(parser, ResolveRule(parser, inner));
    }

    RuleSegment ResolveRepeatRange(IParser parser, IParseNode node)
        => ((ParseNode)node).Expand((ParseNode? inner, ParseNode? range, IParseNode? _, IParseNode? _, IParseNode? _, IParseNode? _, IParseNode? _, IParseNode[] _) =>
        {
            var innerRule = ResolveRule(parser, inner!);

            return range!.Expand((IParseNode? _, IParseNode? _, IParseNode? minNode, IParseNode? _, TokenNode? commaNode, IParseNode? _, IParseNode? maxNode, IParseNode[] _) =>
            {
                if (commaNode! != ",")
                {
                    throw new Exception();
                }

                int? min = minNode switch
                {
                    TokenNode minNodeT => int.Parse(minNodeT),
                    _ => null,
                };

                int? max = maxNode switch
                {
                    TokenNode maxNodeT => int.Parse(maxNodeT),
                    ParseNode maxNodeP => int.TryParse(maxNodeP.SingleOrDefault() as TokenNode ?? (string?)null, out int v) ? v : null,
                    _ => throw new Exception(),
                };

                return RuleSegment.RepeatRange(this, innerRule, min, max);
            });
        });

    internal RuleSegment And(params IRule[] rules) => RuleSegment.And(this, rules);

    internal RuleSegment Or(params IRule[] rules) => RuleSegment.Or(this, rules);

    internal RuleSegment Not(IRule rule) => RuleSegment.Not(this, rule);

    internal RuleSegment Option(IRule rule) => RuleSegment.Option(this, rule);

    internal RuleSegment Empty() => RuleSegment.Empty(this);

    internal RuleSegment RepeatRange(IRule rule, int? minimum = null, int? maximum = null) => RuleSegment.RepeatRange(this, rule, minimum, maximum);

    internal RuleSegment Repeat0(IRule rule) => RuleSegment.RepeatRange(this, rule, 0);

    internal RuleSegment Repeat1(IRule rule) => RuleSegment.RepeatRange(this, rule, 1);

    public override TParser Parse(ParseNode parseTree)
    {
        var parser = _cstor(this);

        var tokens = parseTree.ToList<ParseNode>(0, 1, 0);

        foreach (var token in tokens)
        {
            ResolvePatternDeclaration(parser, token);
        }

        var rules = parseTree.ToList<ParseNode>(0, 3, 0);

        foreach (var rule in rules)
        {
            ResolveRuleDeclaration(parser, rule);
        }

        // WAKE UP!
        foreach (var lazyRule in _lazyRules.Values)
        {
            // Accessing `Rule` will cause it to be resolved.
            // If it is not available in `_rules`, it will trigger an exception.
            if (lazyRule.Rule == null)
            {
                throw new Exception();
            }

            _lazyRules.Remove(lazyRule.Name);
        }

        return parser;
    }

    public NamedRule ParseRule(IParser parser, string ruleName, string input)
    {
        var namedRule = GetRule(ruleName) as NamedRule ?? throw new Exception();

        var parseTree = Parse(namedRule, input) as ParseNode ?? throw new Exception();

        return ResolveRuleDeclaration(this, parseTree);
    }

    public void DirectSyntax(string name, Func<IParser, IParseNode, IRule> mapping)
    {
        var rule = GetRule(name);

        _directory.Add(rule, mapping);
    }

    public IRule Translate(IParser parser, INamedRule rule, IParseNode node) => _directory[rule](parser, node);

    public IRule ResolveRule(IParser parser, IParseNode node)
    {
        if (node.Rule is NamedRule named)
        {
            return Translate(parser, named, node);
        }
        else if (node.Rule is LazyNamedRule lazy)
        {
            return Translate(parser, lazy.Rule, node);
        }
        else if (node.Rule is PatternRule _)
        {
            throw new Exception();
            //return Translate(parser, pattern, node);
        }
        else
        {
            throw new Exception();
        }
    }

    public override string ToString(INamedRule rule, IParseNode node)
    {
        if (rule is LazyNamedRule nameRule)
        {
            rule = nameRule.Rule;
        }

        switch (rule.Name)
        {
            case E.ParenExpr:
                var inner = ((NamedRule)rule)
                    .Rule.ToString((ParseNode)node);
                return $"({inner})";
            default:
                throw new Exception();
        }
    }

    ILogger IMetaParser_internal.GetLogger() => Logger;
}

internal class E
{
    public const string S = "S";
    public const string Literal = "literal";
    public const string Pattern = "pattern";
    public const string PatternRule = "patternRule";
    public const string NamedRule = "namedRule";
    public const string Name = "name";
    public const string ParenExpr = "parenExpr";
    public const string RepeatRange = "repeatRange";
    public const string Repeat0 = "repeat0";
    public const string Repeat1 = "repeat1";
    public const string Option = "option";
    public const string OperExpr = "operExpr";
    public const string BaseExpr = "baseExpr";
    public const string BaseExpr2 = "baseExpr2";
    public const string BaseExpr3 = "baseExpr3";
    public const string BaseExpr4 = "baseExpr4";
    public const string NotExpr = "notExpr";
    public const string OrExpr = "orExpr";
    public const string AndExpr = "andExpr";
    public const string Lexicon = "lexicon";
}
