using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpSandbox.Parser
{
    public static class Parser
    {
        public static TParser Generate<TParser, TResult>(string grammar, string rootName, Func<IMetaParser, TParser> cstor)
            where TParser : Parser<TResult>
        {
            var metaParser = MetaParser.Get<TParser, TResult>(rootName, cstor);

            return metaParser.Parse(grammar);
        }
    }

    internal static class MetaParser
    {
        public static MetaParser<TParser, TResult> Get<TParser, TResult>(string rootName, Func<IMetaParser, TParser> cstor)
            where TParser : Parser<TResult>
            => new(rootName, cstor);
        public static MetaParser<TParser, TResult> Get<TParser, TResult>(Func<IMetaParser, TParser> cstor)
            where TParser : Parser<TResult>
            => new("lexicon", cstor);
    }

    internal class MetaParser<TParser, TResult> : Parser<TParser>, IMetaParser
        where TParser : Parser<TResult>
    {
        private readonly Dictionary<INamedRule, Func<IParseNode, RuleSegment>> _directory = new();
        private readonly Dictionary<Type, Func<IRule, TokenList, IParseNode?>> _typeRules = new();
        private readonly Func<IMetaParser, TParser> _cstor;

        internal MetaParser(string rootName, Func<IMetaParser, TParser> cstor)
            : base(rootName)
        {
            _cstor = cstor;

            AddTypeRule<NamedRule>((IRule rule, TokenList tokens) =>
            {
                var self = (NamedRule)rule;
                var tempTokens = tokens.Fork();
                var temp = Parse(self.Rule, tempTokens);
                if (temp != null)
                {
                    tokens.Merge(tempTokens);
                    return new ParseNode(self.Rule, temp);
                }

                return null;
            });

            AddTypeRule<NameRule>((IRule rule, TokenList tokens) =>
            {
                var self = (NameRule)rule;
                return Parse(self.Rule, tokens);
            });


            AddTypeRule<PatternRule>((IRule rule, TokenList tokens) =>
            {
                var self = (PatternRule)rule;
                var first = tokens.First();
                if (first.Pattern == self.Pattern)
                {
                    tokens.Cursor++;
                    return new TokenNode(self, first);
                }

                return null;
            });


            AddTypeRule<RuleSegment>((IRule rule, TokenList tokens) => ParseRuleSegment((RuleSegment)rule, tokens));
        }

        protected override IParseNode? ParseRuleSegment(RuleSegment rule, TokenList tokens)
        {
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
                                tempTokens.Cursor = 0;
                            }
                        }
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
                        if (temp == null)
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
                        if (temp != null)
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
                                return null;
                            }
                            else
                            {
                                break;
                            }
                        }

                        tokens.Merge(tempTokens);
                        return new ParseNode(rule, nodes.ToArray());
                    }
            }

            return null;
        }

        private void AddTypeRule<TRule>(Func<IRule, TokenList, IParseNode?> rule) where TRule : IRule => _typeRules.Add(typeof(TRule), rule);

        internal RuleSegment And(params IRule[] rules) => RuleSegment.And(this, rules);

        internal RuleSegment Or(params IRule[] rules) => RuleSegment.Or(this, rules);

        internal RuleSegment Not(IRule rule) => RuleSegment.Not(this, rule);

        internal RuleSegment Option(IRule rule) => RuleSegment.Option(this, rule);

        internal RuleSegment RepeatRange(IRule rule, int? minimum = null, int? maximum = null) => RuleSegment.RepeatRange(this, rule, minimum, maximum);

        internal RuleSegment Repeat0(IRule rule) => RuleSegment.RepeatRange(this, rule, 0);

        internal RuleSegment Repeat1(IRule rule) => RuleSegment.RepeatRange(this, rule, 1);

        public override TParser Parse(string grammar)
        {
            var parser = _cstor(this);

            var parseTree = Parse(Root, grammar) as ParseNode ?? throw new Exception();

            var tokens = parseTree.Get(0, 0, 0) as ParseNode ?? throw new Exception();

            foreach (var token in tokens.Children)
            {
                var pnode = token as ParseNode ?? throw new Exception();

                var stmt = pnode.Get(0) as ParseNode ?? throw new Exception();

                var name = (stmt.Get(0) as TokenNode ?? throw new Exception())
                    .Token.Lexeme;

                var value = stmt.Get(2, 0) as TokenNode ?? throw new Exception();

                var valueLex = value.Token.Lexeme;

                valueLex = valueLex[1..^1];
                switch (value.Rule.Name)
                {
                    case "literal":
                        parser.DefineLiteral(name, valueLex);
                        break;
                    case "pattern":
                        parser.DefinePattern(name, valueLex);
                        break;
                    default:
                        throw new Exception();
                }
            }

            var rules = parseTree.Get(0, 1, 0) as ParseNode ?? throw new Exception();

            foreach (var rule in rules.Children)
            {
                var pnode = rule as ParseNode ?? throw new Exception();

                var stmt = pnode.Get(0) as ParseNode ?? throw new Exception();

                var name = (stmt.Get(0) as TokenNode ?? throw new Exception())
                    .Token.Lexeme;

                var value = stmt.Get(2) as ParseNode ?? throw new Exception();
                var segment = ResolveRuleSegment(value);

                parser.DefineRule(name, segment);
            }

            return parser;
        }

        IParseNode? Parse<TRule>(TRule rule, string input)
            where TRule : IRule
            => _typeRules[typeof(TRule)](rule, Tokenize(input));

        IParseNode? Parse<TRule>(TRule rule, TokenList tokens)
            where TRule : IRule
            => _typeRules[typeof(TRule)](rule, tokens);

        IParseNode? Parse(string ruleName, string input)
            => Parse(GetRule(ruleName) ?? throw new Exception(), Tokenize(input));

        public void ParseRule(IParser parser, IParseNode? node)
        {
            var pnode = node as ParseNode ?? throw new Exception();

            var stmt = pnode.Get(0) as ParseNode ?? throw new Exception();
            var name = (stmt.Get(0) as TokenNode ?? throw new Exception())
                .Token.Lexeme;
            var value = stmt.Get(2) as ParseNode ?? throw new Exception();
            var rule = ResolveRuleSegment(value);

            parser.DefineRule(name, rule);
        }

        public IRule ParseRule(IParser parser, string ruleName, string input)
        {
            var namedRule = GetRule(ruleName) as NamedRule ?? throw new Exception();

            var parseTree = Parse(namedRule, input) as ParseNode ?? throw new Exception();

            var ruleSegment = ResolveRuleSegment(parseTree);

            var result = parser.DefineRule(ruleName, ruleSegment);

            return result;
        }

        public void DirectSyntax(string name, Func<IParseNode, RuleSegment> mapping)
        {
            var rule = GetRule(name) ?? throw new Exception();

            _directory.Add(rule, mapping);
        }

        public RuleSegment Translate(INamedRule rule, IParseNode node) => _directory[rule](node);

        public RuleSegment ResolveRuleSegment(IParseNode node)
        {
            if (node.Rule is NamedRule named)
            {
                return Translate(named, node);
            }
            else if (node.Rule is NameRule lazy)
            {
                return Translate(lazy.Rule, node);
            }
            else if (node.Rule is PatternRule pattern)
            {
                return Translate(pattern, node);
            }
            else
            {
                throw new Exception();
            }
        }
    }

    public abstract class Parser<TResult> : IParser<TResult>
    {
        internal readonly Dictionary<string, PatternRule> _patternRules = new();
        internal readonly Dictionary<string, INamedRule> _rules = new();
        internal readonly Dictionary<string, NameRule> _lazyRules = new();

        private NamedRule? _root;

        public string RootName { get; }

        protected internal IMetaParser? MetaParser { get; }

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

        internal Parser(string rootName)
        {
            RootName = rootName;
        }

        public Parser(IMetaParser metaParser, string rootName)
            : this(rootName)
        {
            MetaParser = metaParser;
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
            if (MetaParser == null)
            {
                throw new InvalidOperationException();
            }

            var segment = MetaParser.ParseRule(this, "baseExpr2", rule) as RuleSegment ?? throw new Exception();
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

        public INamedRule? GetRule(string name)
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
    }
}
