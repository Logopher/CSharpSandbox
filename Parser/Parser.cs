﻿using Microsoft.Extensions.Logging;
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
    private readonly Dictionary<Type, Func<IRule, TokenList, IParseNode?>> _typeRules = new();

    private NamedRule? _root;
    private readonly IMetaParser? _metaParser;
    private readonly ILogger _logger;

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
        _logger = logger;
        RootName = rootName;

        Init();
    }

    public Parser(IMetaParser metaParser, string rootName)
    {
        _metaParser = metaParser;
        _logger = ((IMetaParser_internal)_metaParser).GetLogger();
        RootName = rootName;

        Init();
    }

    internal Parser(string rootName, ILogger logger)
    {
        _metaParser = this as IMetaParser ?? throw new Exception();
        _logger = logger;
        RootName = rootName;

        Init();
    }

    private void Init()
    {
        void addTypeRule<TRule>(Func<TRule, TokenList, IParseNode?> rule) where TRule : IRule => _typeRules.Add(typeof(TRule), (r, l) => rule((TRule)r, l));

        addTypeRule((NamedRule self, TokenList tokens) =>
        {
            var tempTokens = tokens.Fork();
            _logger.LogTrace("rule {Name} match? {Tokens}", self.Name, tokens);
            var temp = Parse(self.Rule, tempTokens);
            var result = temp != null;
            _logger.LogTrace("rule {Name} match {Result} {Tokens}", self.Name, result ? "passed" : "failed", tokens);
            if (result)
            {
                tokens.Merge(tempTokens);
                return new ParseNode(self, temp!);
            }

            return null;
        });

        addTypeRule((LazyNamedRule self, TokenList tokens) => Parse(self.Rule, tokens));

        addTypeRule((PatternRule self, TokenList tokens) =>
        {
            if(tokens.Count == 0)
            {
                return null;
            }

            var first = tokens[0];
            _logger.LogTrace("pattern {Name} match? {Token}", self.Name, first);
            var result = first.Pattern == self.Pattern;
            _logger.LogTrace("pattern {Name} match {Result} {Token}", self.Name, result ? "passed" : "failed", first);
            if (first.Pattern == self.Pattern)
            {
                tokens.Cursor++;
                return new TokenNode(self, first);
            }

            return null;
        });

        addTypeRule((RuleSegment self, TokenList tokens) => ParseRuleSegment(self, tokens));

        addTypeRule((RepeatRule self, TokenList tokens) => ParseRuleSegment(self, tokens));
    }

    protected IParseNode? Parse<TRule>(TRule rule, string input)
        where TRule : IRule
        => Parse(rule, Tokenize(input));

    protected IParseNode? Parse<TRule>(TRule rule, TokenList tokens)
        where TRule : IRule
        => _typeRules[rule.GetType()](rule, tokens);

    protected IParseNode? Parse(string ruleName, string input)
        => Parse(GetRule(ruleName), Tokenize(input));

    internal PatternRule DefinePattern(string name, Pattern pattern)
    {
        var rule = new PatternRule(this, name, pattern);
        _patternRules.Add(name, rule);
        _rules.Add(name, rule);
        return rule;
    }

    internal PatternRule DefineLiteral(string name, string pattern) => DefinePattern(name, Pattern.FromLiteral(pattern));

    internal PatternRule DefinePattern(string name, string pattern)
    {
        var rule = new PatternRule(this, name, new Pattern(pattern));
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
        _logger.LogTrace("Parsing: {Input}", input);
        var parseTree = Parse(Root, input) as ParseNode ?? throw new Exception();
        _logger.LogTrace("Parse tree produced.");
        return Parse(parseTree);
    }

    private IParseNode? ParseRuleSegment(RuleSegment rule, TokenList tokens)
    {
        _logger.LogTrace("rule {Rule} match? {Tokens}", rule, tokens);
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
                    _logger.LogTrace("rule {Rule} match {Result} {Tokens}", rule, match ? "passed" : "failed", tokens);
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
                    _logger.LogTrace("rule {Rule} match {Result} {Tokens}", rule, match ? "passed" : "failed", tokens);
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
                    _logger.LogTrace("rule {Rule} match {Result} {Tokens}", rule, match ? "passed" : "failed", tokens);
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
                    _logger.LogTrace("rule {Rule} match {Result} {Tokens}", rule, match ? "passed" : "failed", tokens);
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
                            _logger.LogTrace("rule {Rule} match {Result} {Tokens}", rule, "failed", tokens);
                            return null;
                        }
                        else
                        {
                            break;
                        }
                    }

                    _logger.LogTrace("rule {Rule} match {Result} {Tokens}", rule, "passed", tokens);
                    tokens.Merge(tempTokens);
                    return new ParseNode(rule, nodes.ToArray());
                }
        }

        return null;
    }

    public abstract TResult Parse(ParseNode parseTree);

    public abstract string ToString(INamedRule rule, IParseNode node);
}
