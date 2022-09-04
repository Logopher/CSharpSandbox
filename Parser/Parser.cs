using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpSandbox.Parsing;

public static class Parser
{
    public static TParser Generate<TParser, TResult>(string grammar, string rootName, Func<IMetaParser, TParser> cstor)
        where TParser : Parser<TResult>
    {
        var metaParser = MetaParser.Get<TParser, TResult>(cstor);

        return metaParser.Parse(grammar);
    }
}

public abstract class Parser<TResult> : IParser
{
    internal readonly Dictionary<string, PatternRule> _patternRules = new();
    internal readonly Dictionary<string, INamedRule> _rules = new();
    internal readonly Dictionary<string, NameRule> _lazyRules = new();

    private NamedRule? _root;
    private readonly IMetaParser? _metaParser;

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

    public Parser(IMetaParser metaParser, string rootName)
    {
        _metaParser = metaParser;
        RootName = rootName;
    }

    internal Parser(string rootName)
    {
        _metaParser = this as IMetaParser ?? throw new Exception();
        RootName = rootName;
    }

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
        var segment = MetaParser.ParseRule(this, "baseExpr3", rule) as RuleSegment ?? throw new Exception();
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

    internal NameRule GetLazyRule(string name)
    {
        if (!_lazyRules.TryGetValue(name, out NameRule? rule))
        {
            rule = new NameRule(this, name);
            _lazyRules.Add(name, rule);
        }
        return rule;
    }

    internal TokenList Tokenize(string input)
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

    public abstract TResult Parse(string input);

    protected abstract IParseNode? ParseRuleSegment(RuleSegment rule, TokenList tokens);

    public abstract string ToString(INamedRule rule, IParseNode node);
}
