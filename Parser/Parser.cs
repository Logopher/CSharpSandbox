using CSharpSandbox.Common;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpSandbox.Parsing;

public abstract class Parser<TResult> : IParser
{
    static readonly ILogger CurrentLogger = Toolbox.LoggerFactory.CreateLogger<Parser<TResult>>();

    private readonly IMetaParser? _metaParser;

    private NamedRule? _rootRule;
    private IRule? _currentRule;
    private IParseNode? _recentNode;

    private int _index;
    internal readonly Dictionary<string, Pattern> _patternRules = new();
    internal readonly Dictionary<string, INamedRule> _rules = new();
    internal readonly Dictionary<string, LazyNamedRule> _lazyRules = new();

    public Type ResultType => typeof(TResult);

    public bool IsParsing { get; private set; }

    public string RootRuleName { get; }

    public IParseNode? RecentNode => _recentNode;

    public IRule CurrentRule => _currentRule ?? throw new Exception();

    protected internal IMetaParser MetaParser => _metaParser ?? throw new Exception();

    internal NamedRule RootRule
    {
        get
        {
            if (_rootRule == null)
            {
                var namedRule = GetRule(RootRuleName) as NamedRule ?? throw new InvalidOperationException();

                _rootRule = namedRule;
            }

            return _rootRule;
        }
    }

    public ParseError ParseError { get; private set; }

    public Parser(IMetaParser metaParser, string rootName)
    {
        _metaParser = metaParser;
        RootRuleName = rootName;
    }

    internal Parser(string rootName)
    {
        _metaParser = this as IMetaParser ?? throw new Exception();
        RootRuleName = rootName;
    }

    protected IParseNode? Parse(string ruleName, string input)
        => Parse(GetRule(ruleName), input);

    protected IParseNode? Parse(IRule rule, string input)
    {
        using var tokens = Tokenize(input);
        var parseTree = Parse(rule, tokens);

        if (0 < tokens.Count)
        {
            CurrentLogger.LogError("Parse failed. Some tokens not consumed: {Tokens}", tokens);
            return null;
        }

        return parseTree;
    }

    protected IParseNode? Parse(IRule rule, TokenList tokens)
    {
        _currentRule = rule;

        IParseNode? resultNode = null;
        using var tempTokens = tokens.Fork();

        if (tempTokens.TryGetCachedMatch(rule, out Token.Match? match))
        {
            CurrentLogger.LogTrace("RULE {Name} MATCH {Result} {Tokens}", rule.ToString(), "CACHED", tokens.ToString());
            tempTokens.Reset(match);
            tempTokens.Merge();
            resultNode = match.Node;
        }
        else
        {
            var logPrefix = rule switch
            {
                Pattern => "PATTERN",
                LazyNamedRule or NamedRule => "RULE",
                RepeatRule or RuleSegment => "SEGMENT",
                _ => throw new Exception(),
            };

            // By the end of the switch, `node` should be non-null, unless the match failed.

            CurrentLogger.LogTrace("{Prefix} {Name} MATCH? {Tokens}", logPrefix, rule.ToString(), tokens.ToString());
            switch (rule)
            {
                case LazyNamedRule lazy:
                    resultNode = Parse(lazy.Rule, tempTokens);
                    break;
                case Pattern:
                    {
                        var temp = tempTokens.FirstOrDefault();
                        if (temp?.Rule == rule)
                        {
                            resultNode = temp;
                            tempTokens.Advance();
                        }
                    }
                    break;
                case NamedRule:
                    {
                        var namedRule = (NamedRule)rule;
                        var temp = Parse(namedRule.Rule, tempTokens);

                        if (temp != null)
                        {
                            resultNode = new ParseNode(namedRule, temp);
                        }
                    }
                    break;
                case RepeatRule repeatRule:
                    resultNode = ParseRuleSegment(repeatRule, tempTokens);
                    break;
                case RuleSegment ruleSegment:
                    resultNode = ParseRuleSegment(ruleSegment, tempTokens);
                    break;
                default:
                    throw new Exception();
            }
            CurrentLogger.LogTrace("{Prefix} {Name} MATCH {Result} {Tokens}", logPrefix, rule.ToString(), resultNode != null ? "PASSED" : "FAILED", tokens.ToString());

            if (resultNode == null)
            {
                ParseError = new ParseError(RecentNode, CurrentRule);
            }
            else
            {
                tokens[0].AddMatchingRule(rule, resultNode, tempTokens.Cursor);
                tempTokens.Merge();
            }
        }

        _recentNode = resultNode;

        return resultNode;
    }

    internal Pattern DefinePattern(Pattern pattern)
    {
        _patternRules.Add(pattern.Name, pattern);
        _rules.Add(pattern.Name, pattern);
        return pattern;
    }

    internal Pattern DefineLiteral(string name, string pattern) => DefinePattern(Pattern.FromLiteral(this, name, pattern, CurrentLogger));

    public Pattern DefinePattern(string name, string pattern)
    {
        var rule = new Pattern(this, name, pattern, CurrentLogger);
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

    public bool TryParse(string input, [NotNullWhen(true)] out TResult? result)
    {
        IsParsing = true;

        CurrentLogger.LogTrace("Parsing: {Input}", input);
        var parseTree = Parse(RootRule, input) as ParseNode;
        CurrentLogger.LogTrace("Parse tree produced: {Tree}", parseTree);

        if (parseTree == null)
        {
            CurrentLogger.LogError("Failed to parse. Nearby node: {Node} Failed rule: {Rule}", ParseError.NearbyNode, ParseError.FailedRule);

            result = default;
            IsParsing = false;
            return false;
        }

        result = Parse(parseTree)!;
        IsParsing = false;
        return true;
    }

    private IParseNode? ParseRuleSegment(RuleSegment rule, TokenList tokens)
    {
        if (tokens.TryGetCachedMatch(rule, out Token.Match? match))
        {
            CurrentLogger.LogTrace("RULE {Name} MATCH {Result} {Tokens}", rule.ToString(), "CACHED", tokens.ToString());
            tokens.Reset(match);
            return match.Node;
        }

        Dictionary<Operator, Func<RuleSegment, TokenList, Tuple<bool, ParseNode?, bool>>> directory = new()
        {
            {
                Operator.And,
                (RuleSegment rule, TokenList tokens) =>
                {
                    var match = true;

                    var nodes = new List<IParseNode>();
                    foreach (var r in rule.Rules)
                    {
                        var temp = Parse(r!, tokens);
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

                    return Tuple.Create(match, match ? new ParseNode(rule, nodes.ToArray()) : null, match);
                }
            },

            {
                Operator.Or,
                (RuleSegment rule, TokenList tokens) =>
                {
                    IParseNode? temp = null;
                    foreach (var r in rule.Rules)
                    {
                        using var tempTokens = tokens.Fork();
                        temp = Parse(r!, tempTokens);
                        if (temp != null)
                        {
                            tempTokens.Merge();
                            return Tuple.Create(true, (ParseNode?)new ParseNode(rule, temp), true);
                        }
                    }

                    return Tuple.Create(false, (ParseNode?)null, false);
                }
            },

            {
                Operator.Not,
                (RuleSegment rule, TokenList tokens) =>
                {
                    using var tempTokens = tokens.Fork();
                    var r = rule.Rules.Single();
                    var temp = Parse(r, tempTokens);
                    var match = temp == null;
                    CurrentLogger.LogTrace("RULE {Rule} MATCH {Result} {Tokens}", rule, match ? "PASSED" : "FAILED", tokens.ToString());
                    if (match)
                    {
                        var pnode = new ParseNode(rule);
                        return Tuple.Create(true, (ParseNode?)pnode, false);
                    }

                    return Tuple.Create(false, (ParseNode?)null, false);
                }
            },

            {
                Operator.Option,
                (RuleSegment rule, TokenList tokens) =>
                {
                    using var tempTokens = tokens.Fork();

                    var r = rule.Rules.Single();
                    var temp = Parse(r, tempTokens);
                    var match = temp != null;
                    CurrentLogger.LogTrace("RULE {Rule} MATCH {Result} {Tokens}", rule, match ? "PASSED" : "FAILED", tokens.ToString());
                    if (match)
                    {
                        var pnode = new ParseNode(rule, temp!);
                        tempTokens.Merge();
                        return Tuple.Create(true, (ParseNode?)pnode, true);
                    }
                    else
                    {
                        var pnode = new ParseNode(rule);
                        return Tuple.Create(true, (ParseNode?)pnode, true);
                    }
                }
            },

            {
                Operator.Repeat,
                (RuleSegment rule, TokenList tokens) =>
                {
                    using var tempTokens = tokens.Fork();
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
                            return Tuple.Create(false, (ParseNode?)null, false);
                        }
                        else
                        {
                            break;
                        }
                    }

                    tempTokens.Merge();
                    var pnode = new ParseNode(rule, nodes.ToArray());
                    return Tuple.Create(true, (ParseNode?)pnode, true);
                }
            }
        };

        CurrentLogger.LogTrace("RULE {Rule} MATCH? {Tokens}", rule, tokens.ToString());

        using var tempTokens = tokens.Fork();
        var result = directory[rule.Operator](rule, tempTokens);
        CurrentLogger.LogTrace("RULE {Rule} MATCH {Result} {Tokens}", rule, result.Item1 ? "PASSED" : "FAILED", tokens.ToString());

        if (result.Item3)
        {
            tempTokens.Merge();
        }

        return result.Item2;
    }

    public abstract TResult Parse(ParseNode parseTree);

    public abstract string ToString(INamedRule rule, IParseNode node);
}
