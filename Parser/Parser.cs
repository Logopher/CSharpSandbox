using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpSandbox.Parsing;

public abstract class Parser<TResult> : IParser
{
    internal readonly Dictionary<string, Pattern> _patternRules = new();
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
        if (tokens.TryGetCachedMatch(rule, out Token.Match? match))
        {
            Logger.LogTrace("RULE {Name} MATCH {Result} {Tokens}", rule.ToString(), "CACHED", tokens.ToString());
            tokens.Reset(match);
            return match.Node;
        }

        switch (rule)
        {
            case LazyNamedRule lazy:
                return Parse(lazy.Rule, tokens);

            case NamedRule namedRule:
                {
                    var tempTokens = tokens.Fork();
                    Logger.LogTrace("RULE {Name} MATCH? {Tokens}", namedRule.Name, tokens.ToString());
                    var temp = Parse(namedRule.Rule, tempTokens);
                    var result = temp != null;
                    Logger.LogTrace("RULE {Name} MATCH {Result} {Tokens}", namedRule.Name, result ? "PASSED" : "FAILED", tokens.ToString());
                    if (result)
                    {
                        var pnode = new ParseNode(namedRule, temp!);
                        tokens[0].AddMatchingRule(namedRule, pnode, tempTokens.Cursor);
                        tempTokens.Merge();
                        return pnode;
                    }

                    return null;
                }

            case Pattern pattern:
                {
                    var tempTokens = tokens.Fork();

                    if (tempTokens.Count == 0)
                    {
                        return null;
                    }

                    var first = tempTokens[0];

                    Logger.LogTrace("PATTERN {Name} MATCH? {Token}", pattern.Name, first);
                    var result = first.Rule == pattern;
                    Logger.LogTrace("PATTERN {Name} MATCH {Result} {Token}", pattern.Name, result ? "PASSED" : "FAILED", first);
                    if (result)
                    {
                        tempTokens.Advance();
                        first.AddMatchingRule(rule, first, tempTokens.Cursor);
                        tempTokens.Merge();
                        return first;
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

    internal Pattern DefinePattern(Pattern pattern)
    {
        _patternRules.Add(pattern.Name, pattern);
        _rules.Add(pattern.Name, pattern);
        return pattern;
    }

    internal Pattern DefineLiteral(string name, string pattern) => DefinePattern(Pattern.FromLiteral(this, name, pattern, Logger));

    public Pattern DefinePattern(string name, string pattern)
    {
        var rule = new Pattern(this, name, pattern, Logger);
        _patternRules.Add(name, rule);
        _rules.Add(name, rule);
        return rule;
    }

    internal NamedRule DefineRule(string name, string rule)
    {
        var namedRule = MetaParser.ParseRuleDefinition(this, E.BaseExpr3, rule);
        _rules.Add(name, namedRule);
        return namedRule;
    }

    internal NamedRule DefineRule(string name, RuleSegment segment)
    {
        var rule = new NamedRule(this, name, segment);
        _rules.Add(name, rule);
        return rule;
    }

    Pattern IParser.DefineLiteral(string name, string pattern) => DefineLiteral(name, pattern);
    NamedRule IParser.DefineRule(string name, string rule) => MetaParser.ParseRuleDefinition(this, name, rule);
    NamedRule IParser.DefineRule(string name, RuleSegment segment) => DefineRule(name, segment);

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
        var result = new List<Token>();
        var length = builder.Length;
        while (0 < builder.Length)
        {
            foreach (var (_, rule) in _patternRules)
            {
                if (rule.TryMatch(builder, out Token? token))
                {
                    result.Add(token);
                    continue;
                }
            }

            if (builder.Length == length)
            {
                throw new Exception();
            }

            length = builder.Length;
        }
        return TokenList.Create(result);
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
        Logger.LogTrace("RULE {Rule} MATCH? {Tokens}", rule, tokens.ToString());
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
                    Logger.LogTrace("RULE {Rule} MATCH {Result} {Tokens}", rule, match ? "PASSED" : "FAILED", tokens.ToString());
                    if (match)
                    {
                        var pnode = new ParseNode(rule, nodes.ToArray());
                        tokens[0].AddMatchingRule(rule, pnode, tempTokens.Cursor);
                        tempTokens.Merge();
                        return pnode;
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
                    Logger.LogTrace("RULE {Rule} MATCH {Result} {Tokens}", rule, match ? "PASSED" : "FAILED", tokens.ToString());
                    if (match)
                    {
                        var pnode = new ParseNode(rule, temp!);
                        tokens[0].AddMatchingRule(rule, pnode, tempTokens.Cursor);
                        tempTokens.Merge();
                        return pnode;
                    }
                }
                break;
            case Operator.Not:
                {
                    var tempTokens = tokens.Fork();
                    var r = rule.Rules.Single();
                    var temp = Parse(r, tempTokens);
                    var match = temp == null;
                    Logger.LogTrace("RULE {Rule} MATCH {Result} {Tokens}", rule, match ? "PASSED" : "FAILED", tokens.ToString());
                    if (match)
                    {
                        var pnode = new ParseNode(rule);
                        if (0 < tokens.Count)
                        {
                            tokens[0].AddMatchingRule(rule, pnode, tempTokens.Cursor);
                        }
                        return pnode;
                    }
                }
                break;
            case Operator.Option:
                {
                    var tempTokens = tokens.Fork();
                    var r = rule.Rules.Single();
                    var temp = Parse(r, tempTokens);
                    var match = temp != null;
                    Logger.LogTrace("RULE {Rule} MATCH {Result} {Tokens}", rule, match ? "PASSED" : "FAILED", tokens.ToString());
                    if (match)
                    {
                        var pnode = new ParseNode(rule, temp!);
                        tokens[0].AddMatchingRule(rule, pnode, tempTokens.Cursor);
                        tempTokens.Merge();
                        return pnode;
                    }
                    else
                    {
                        var pnode = new ParseNode(rule);
                        if (0 < tokens.Count)
                        {
                            tokens[0].AddMatchingRule(rule, pnode, tempTokens.Cursor);
                        }
                        return pnode;
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
                            Logger.LogTrace("RULE {Rule} MATCH {Result} {Tokens}", rule, "FAILED", tokens.ToString());
                            return null;
                        }
                        else
                        {
                            break;
                        }
                    }

                    Logger.LogTrace("RULE {Rule} MATCH {Result} {Tokens}", rule, "PASSED", tokens.ToString());
                    var pnode = new ParseNode(rule, nodes.ToArray());
                    if (0 < tokens.Count)
                    {
                        tokens[0].AddMatchingRule(rule, pnode, tempTokens.Cursor);
                    }
                    tempTokens.Merge();
                    return pnode;
                }
        }

        return null;
    }

    public abstract TResult Parse(ParseNode parseTree);

    public abstract string ToString(INamedRule rule, IParseNode node);
}
