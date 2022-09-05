using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpSandbox.Parsing;

public abstract class Parser<TResult> : IParser
{
    internal readonly Dictionary<string, PatternRule> _patternRules = new();
    internal readonly Dictionary<string, INamedRule> _rules = new();
    internal readonly Dictionary<string, LazyNamedRule> _lazyRules = new();

    private NamedRule? _root;
    private readonly IMetaParser? _metaParser;
    protected ILogger Logger { get; }

    public Type ResultType => typeof(TResult);
    public string RootName { get; }

    protected internal IMetaParser MetaParser => _metaParser ?? throw new Exception();

    internal NamedRule Root
    {
        get
        {
            if (_root == null)
            {
                var namedRule = GetRule(RootName) as NamedRule ?? throw new InvalidOperationException();

                _root = namedRule;
            }

            return _root;
        }
    }

    internal Parser(IMetaParser metaParser, string rootName, ILogger logger)
    {
        _metaParser = metaParser;
        Logger = logger;
        RootName = rootName;
    }

    public Parser(IMetaParser metaParser, string rootName)
    {
        _metaParser = metaParser;
        Logger = ((IMetaParser_internal)_metaParser).GetLogger();
        RootName = rootName;
    }

    internal Parser(string rootName, ILogger logger)
    {
        _metaParser = this as IMetaParser ?? throw new Exception();
        Logger = logger;
        RootName = rootName;
    }

    protected IParseNode? Parse<TRule>(TRule rule, string input)
        where TRule : IRule
        => Parse(rule, Tokenize(input));

    protected IParseNode? Parse<TRule>(TRule rule, TokenList tokens)
        where TRule : IRule
    {
        switch (rule)
        {
            case NamedRule namedRule:
                {
                    var tempTokens = tokens.Fork();
                    Logger.LogTrace("rule {Name} match? {Tokens}", namedRule.Name, tokens);
                    var temp = Parse(namedRule.Rule, tempTokens);
                    var result = temp != null;
                    Logger.LogTrace("rule {Name} match {Result} {Tokens}", namedRule.Name, result ? "PASSED" : "FAILED", tokens);
                    if (result)
                    {
                        tokens.Merge(tempTokens);
                        return new ParseNode(namedRule, temp!);
                    }

                    return null;
                }

            case LazyNamedRule lazy:
                return Parse(lazy.Rule, tokens);

            case PatternRule patternRule:
                {
                    if (tokens.Count == 0)
                    {
                        return null;
                    }

                    var first = tokens[0];
                    Logger.LogTrace("pattern {Name} match? {Token}", patternRule.Name, first);
                    var result = first.Pattern == patternRule.Pattern;
                    Logger.LogTrace("pattern {Name} match {Result} {Token}", patternRule.Name, result ? "PASSED" : "FAILED", first);
                    if (first.Pattern == patternRule.Pattern)
                    {
                        tokens.Cursor++;
                        return new TokenNode(patternRule, first);
                    }

                    return null;
                }

            case RepeatRule repeatRule:
                return ParseRuleSegment(repeatRule, tokens);

            case RuleSegment ruleSegment:
                return ParseRuleSegment(ruleSegment, tokens);
            default:
                throw new Exception();
        }
    }
    protected IParseNode? Parse(string ruleName, string input)
        => Parse(GetRule(ruleName), Tokenize(input));

    internal PatternRule DefinePattern(string name, Pattern pattern)
    {
        var rule = new PatternRule(this, name, pattern);
        _patternRules.Add(name, rule);
        _rules.Add(name, rule);
        return rule;
    }

    internal PatternRule DefineLiteral(string name, string pattern) => DefinePattern(name, Pattern.FromLiteral(this, pattern, Logger));

    internal PatternRule DefinePattern(string name, string pattern)
    {
        var rule = new PatternRule(this, name, new Pattern(this, pattern, Logger));
        _patternRules.Add(name, rule);
        _rules.Add(name, rule);
        return rule;
    }

    internal NamedRule DefineRule(string name, string rule)
    {
        var segment = MetaParser.ParseRule(this, E.BaseExpr3, rule) as RuleSegment ?? throw new Exception();
        var namedRule = new NamedRule(this, name, segment);
        _rules.Add(name, namedRule);
        return namedRule;
    }

    internal NamedRule DefineRule(string name, RuleSegment segment)
    {
        var rule = new NamedRule(this, name, segment);
        _rules.Add(name, rule);
        return rule;
    }

    INamedRule IParser.DefineLiteral(string name, string pattern) => DefineLiteral(name, pattern);
    INamedRule IParser.DefinePattern(string name, string pattern) => DefinePattern(name, pattern);
    INamedRule IParser.DefineRule(string name, string rule) => DefinePattern(name, rule);
    INamedRule IParser.DefineRule(string name, RuleSegment segment) => DefineRule(name, segment);

    public INamedRule GetRule(string name)
    {
        if (!_rules.TryGetValue(name, out INamedRule? rule))
        {
            throw new KeyNotFoundException();
        }
        return rule;
    }

    public LazyNamedRule GetLazyRule(string name)
    {
        if (!_lazyRules.TryGetValue(name, out LazyNamedRule? rule))
        {
            rule = new LazyNamedRule(this, name);
            _lazyRules.Add(name, rule);
        }
        return rule;
    }

    public TokenList Tokenize(string input)
    {
        StringBuilder builder = new(input);
        TokenList result = new();
        while (0 < builder.Length)
        {
            foreach (var (_, rule) in _patternRules)
            {
                if (rule.Pattern.TryMatch(builder, out Token? token))
                {
                    result.Add(token);
                    continue;
                }
            }
        }
        return result;
    }

    public TResult Parse(string input)
    {
        Logger.LogTrace("Parsing: {Input}", input);
        var parseTree = Parse(Root, input) as ParseNode ?? throw new Exception();
        Logger.LogTrace("Parse tree produced.");
        return Parse(parseTree);
    }

    private IParseNode? ParseRuleSegment(RuleSegment rule, TokenList tokens)
    {
        Logger.LogTrace("rule {Rule} match? {Tokens}", rule, tokens);
        switch (rule.Operator)
        {
            case Operator.And:
                {
                    var tempTokens = tokens.Fork();
                    var match = true;
                    var nodes = new List<IParseNode>();
                    foreach (var r in rule.Rules)
                    {
                        var temp = Parse(r!, tempTokens);
                        if (temp != null)
                        {
                            nodes.Add(temp);
                        }
                        else
                        {
                            match = false;
                            break;
                        }
                    }
                    Logger.LogTrace("rule {Rule} match {Result} {Tokens}", rule, match ? "PASSED" : "FAILED", tokens);
                    if (match)
                    {
                        tokens.Merge(tempTokens);
                        return new ParseNode(rule, nodes.ToArray());
                    }
                }
                break;
            case Operator.Or:
                {
                    var tempTokens = tokens.Fork();
                    var match = false;
                    IParseNode? temp = null;
                    foreach (var r in rule.Rules)
                    {
                        temp = Parse(r!, tempTokens);
                        if (temp != null)
                        {
                            match = true;
                            break;
                        }
                        else
                        {
                            tempTokens = tokens.Fork();
                        }
                    }
                    Logger.LogTrace("rule {Rule} match {Result} {Tokens}", rule, match ? "PASSED" : "FAILED", tokens);
                    if (match)
                    {
                        tokens.Merge(tempTokens);
                        return new ParseNode(rule, temp!);
                    }
                }
                break;
            case Operator.Not:
                {
                    var tempTokens = tokens.Fork();
                    var r = rule.Rules.Single();
                    var temp = Parse(r, tempTokens);
                    var match = temp == null;
                    Logger.LogTrace("rule {Rule} match {Result} {Tokens}", rule, match ? "PASSED" : "FAILED", tokens);
                    if (match)
                    {
                        return new ParseNode(rule);
                    }
                }
                break;
            case Operator.Option:
                {
                    var tempTokens = tokens.Fork();
                    var r = rule.Rules.Single();
                    var temp = Parse(r, tempTokens);
                    var match = temp != null;
                    Logger.LogTrace("rule {Rule} match {Result} {Tokens}", rule, match ? "PASSED" : "FAILED", tokens);
                    if (match)
                    {
                        tokens.Merge(tempTokens);
                        return new ParseNode(rule, temp);
                    }
                    else
                    {
                        return new ParseNode(rule);
                    }
                }
            case Operator.Repeat:
                {
                    var tempTokens = tokens.Fork();
                    var r = rule.Rules.Single();
                    var nodes = new List<IParseNode>();

                    var repeat = (RepeatRule)rule;
                    var min = repeat.Minimum ?? 0;
                    var max = repeat.Maximum;
                    for (var i = 0; max == null || i < max; i++)
                    {
                        var temp = Parse(r, tempTokens);
                        if (temp != null)
                        {
                            nodes.Add(temp);
                        }
                        else if (i < min)
                        {
                            Logger.LogTrace("rule {Rule} match {Result} {Tokens}", rule, "failed", tokens);
                            return null;
                        }
                        else
                        {
                            break;
                        }
                    }

                    Logger.LogTrace("rule {Rule} match {Result} {Tokens}", rule, "passed", tokens);
                    tokens.Merge(tempTokens);
                    return new ParseNode(rule, nodes.ToArray());
                }
        }

        return null;
    }

    public abstract TResult Parse(ParseNode parseTree);

    public abstract string ToString(INamedRule rule, IParseNode node);
}
