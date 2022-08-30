﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CSharpSandbox.Common
{
    public class Parser
    {
        static readonly Parser _metaParser = new();

        static Parser()
        {
            // lazy, as in ZZZZZ
            static INamedRule Z(string name) => _metaParser.GetLazyRule(name);

            // literals
            static PatternRule L(string name, string s) => _metaParser.DefinePattern(name, Pattern.FromLiteral(s));

            // patterns
            static PatternRule P(string name, string s) => _metaParser.DefinePattern(name, new Pattern(s));

            // rules
            static INamedRule R(string name, RuleSegment rule) => _metaParser.DefineRule(name, rule);

            var literal = P("literal", @"""(?:\""|[^""])+""");
            var pattern = P("pattern", @"/(\/|[^/])+/");
            var name = P("name", "[a-zA-Z_][a-zA-Z0-9_]+");
            var posInt = P("name", "[0-9]+");

            var assmt = L("assmt", "=");
            var comma = L("stmtEnd", ",");
            var stmtEnd = L("stmtEnd", ";");
            var amp = L("amp", "&");
            var pipe = L("pipe", "|");
            var excl = L("excl", "!");
            var lParen = L("lParen", "(");
            var rParen = L("rParen", ")");
            var asterisk = L("asterisk", "*");
            var plus = L("plus", "+");
            var lCurly = L("lRange", "{");
            var rCurly = L("rRange", "}");

            var baseExpr = R("baseExpr", Or(name, Z("parenExpr")));
            var range = R("range", Or(And(lCurly, posInt, comma, Option(posInt), rCurly), And(lCurly, comma, posInt, rCurly)));
            var repeatRange = R("repeatRange", And(baseExpr, range));
            var repeat0 = R("repeat0", And(baseExpr, asterisk));
            var repeat1 = R("repeat1", And(baseExpr, plus));
            var notExpr = R("notExpr", And(excl, baseExpr));
            var andExpr = R("andExpr", Repeat(Z("baseExpr2"), minimum: 2));
            var orExpr = R("orExpr", And(Z("baseExpr2"), Repeat1(And(pipe, Z("baseExpr2")))));
            var operExpr = R("operExpr", Or(notExpr, andExpr, orExpr, repeatRange, repeat0, repeat1));
            var baseExpr2 = R("baseExpr2", Or(operExpr, baseExpr));
            var parenExpr = R("parenExpr", And(lParen, baseExpr2, rParen));

            var token = R("token", And(name, assmt, Or(literal, pattern), stmtEnd));
            var rule = R("rule", And(name, assmt, baseExpr2, stmtEnd));

            var tokenSection = R("tokenSection", Repeat(token));
            var ruleSection = R("ruleSection", Repeat(rule));

            var lexicon = R("lexicon", And(tokenSection, ruleSection));
        }

        private static RuleSegment And(params IRule[] rules) => RuleSegment.And(rules);

        private static RuleSegment Or(params IRule[] rules) => RuleSegment.Or(rules);

        private static RuleSegment Not(IRule rule) => RuleSegment.Not(rule);

        private static RuleSegment Option(IRule rule) => RuleSegment.Option(rule);

        private static RuleSegment Repeat(IRule rule, int? minimum = null, int? maximum = null) => RuleSegment.Repeat(rule, minimum, maximum);

        private static RuleSegment Repeat0(IRule rule) => Repeat(rule, 0);

        private static RuleSegment Repeat1(IRule rule) => Repeat(rule, 1);

        readonly Dictionary<string, PatternRule> _patternRules = new();
        readonly Dictionary<string, INamedRule> _rules = new();
        readonly Dictionary<string, NameRule> _lazyRules = new();


        public Parser()
        {
        }

        private TokenList Tokenize(string input)
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

        private bool TryParse(string ruleName, string input, [NotNullWhen(true)] out IParseNode? node)
        {
            var tokens = Tokenize(input);

            return GetRule(ruleName).TryParse(tokens, out node);
        }

        public void DefineLiteral(string name, string literal) => DefinePattern(name, Pattern.FromLiteral(literal));

        public void DefinePattern(string name, string pattern) => DefinePattern(name, new Pattern(pattern));

        private PatternRule DefinePattern(string name, Pattern pattern)
        {
            var rule = new PatternRule(name, pattern);
            _patternRules.Add(name, rule);
            _rules.Add(name, rule);
            return rule;
        }

        public void DefineRule(string name, string rule)
        {
            if (_metaParser.TryParse("baseExpr2", rule, out IParseNode? n))
            {
                if (n is not ParseNode node)
                {
                    throw new Exception();
                }

                node.ToString();
            }
        }

        private NamedRule DefineRule(string name, RuleSegment segment)
        {
            var rule = new NamedRule(name, segment);
            _rules.Add(name, rule);
            return rule;
        }

        private PatternRule GetPattern(string name)
        {
            if (!_patternRules.TryGetValue(name, out PatternRule? pattern))
            {
                throw new KeyNotFoundException();
            }

            return pattern;
        }

        private NameRule GetLazyRule(string name)
        {
            if (!_lazyRules.TryGetValue(name, out NameRule? rule))
            {
                rule = new NameRule(this, name);
                _lazyRules.Add(name, rule);
            }
            return rule;
        }

        private INamedRule GetRule(string name)
        {
            if (!_rules.TryGetValue(name, out INamedRule? rule))
            {
                throw new KeyNotFoundException();
            }
            return rule;
        }

        internal class Pattern
        {
            public Regex Regex { get; }

            public static Pattern FromLiteral(string s) => new(Regex.Escape(s));

            public Pattern(string regex)
            {
                Regex = new Regex($@"^\s*({regex})\s*");
            }

            public bool TryMatch(StringBuilder input, [NotNullWhen(true)] out Token? token)
            {
                token = null;

                var match = Regex.Match(input.ToString());
                if (match?.Success ?? false)
                {
                    var text = match.Groups[1].Value;
                    input.Remove(0, text.Length);
                    token = new(this, text);
                    return true;
                }

                return false;
            }
        }

        internal class Token
        {
            public Pattern Pattern { get; }

            public string Lexeme { get; }

            public Token(Pattern pattern, string lexeme)
            {
                Pattern = pattern;
                Lexeme = lexeme;
            }
        }

        internal class TokenList : IList<Token>
        {
            readonly List<Token> _tokens = new();

            public int Cursor { get; set; }

            public void Commit()
            {
                _tokens.RemoveRange(0, Cursor);
                Cursor = 0;
            }

            public TokenList Fork() => new(Range(Cursor));

            public void Merge(TokenList tokens)
            {
                if (tokens.Cursor < Cursor)
                {
                    throw new Exception();
                }

                Cursor = tokens.Cursor;
            }

            private IEnumerable<Token> Range(int index, int count)
            {
                if (count < index)
                {
                    throw new IndexOutOfRangeException();
                }

                for (var i = index; i < count; i++)
                {
                    yield return _tokens[i];
                }
            }

            private IEnumerable<Token> Range(int index) => Range(index, Count - index);

            public IEnumerator<Token> GetEnumerator()
            {
                for (var i = Cursor; i < Count; i++)
                {
                    yield return _tokens[i];
                }
            }

            public TokenList()
            {
            }

            public TokenList(IEnumerable<Token> tokens)
            {
                _tokens = tokens.ToList();
            }

            public TokenList(params Token[] tokens)
                : this((IEnumerable<Token>)tokens)
            {
            }

            #region Everything else is forwarded to _tokens.
            public Token this[int index]
            {
                get => _tokens[index];
                set => _tokens[index] = value;
            }

            public int Count => _tokens.Count;

            public void Add(Token item) => _tokens.Add(item);

            public void AddRange(IEnumerable<Token> collection) => _tokens.AddRange(collection);

            bool ICollection<Token>.IsReadOnly => false;

            void ICollection<Token>.Clear() => _tokens.Clear();

            bool ICollection<Token>.Contains(Token item) => _tokens.Contains(item);

            void ICollection<Token>.CopyTo(Token[] array, int arrayIndex) => _tokens.CopyTo(array, arrayIndex);

            int IList<Token>.IndexOf(Token item) => _tokens.IndexOf(item);

            void IList<Token>.Insert(int index, Token item) => _tokens.Insert(index, item);

            bool ICollection<Token>.Remove(Token item) => _tokens.Remove(item);

            void IList<Token>.RemoveAt(int index) => _tokens.RemoveAt(index);

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            #endregion
        }

        internal interface IRule
        {
            bool TryParse(TokenList tokens, [NotNullWhen(true)] out IParseNode? node);
        }

        internal enum Operator
        {
            And,
            Or,
            Not,
            Option,
            Repeat,
        }

        internal class PatternRule : INamedRule
        {
            public string Name { get; }
            public Pattern Pattern { get; }

            public PatternRule(string name, Pattern pattern)
            {
                Name = name;
                Pattern = pattern;
            }

            public bool TryParse(TokenList tokens, [NotNullWhen(true)] out IParseNode? node)
            {
                var first = tokens.First();
                if (first.Pattern == Pattern)
                {
                    tokens.Cursor++;
                    node = new TokenNode(this, first);
                    return true;
                }

                node = null;
                return false;
            }
        }

        internal class RuleSegment : IRule
        {
            public IReadOnlyList<IRule> Rules { get; }

            public Operator Operator { get; }

            protected RuleSegment(Operator oper, params IRule[] rules)
            {
                Operator = oper;
                Rules = rules;
            }

            public static RuleSegment And(params IRule[] rules) => new(Operator.And, rules);

            public static RuleSegment Or(params IRule[] rules) => new(Operator.Or, rules);

            public static RuleSegment Not(IRule rule) => new(Operator.Not, rule);

            public static RuleSegment Option(IRule rule) => new(Operator.Option, rule);

            public static RuleSegment Repeat(IRule rule, int? minimum = null, int? maximum = null) => new RepeatRule(rule, minimum, maximum);

            public bool TryParse(TokenList tokens, [NotNullWhen(true)] out IParseNode? node)
            {
                switch (Operator)
                {
                    case Operator.And:
                        {
                            var tempTokens = tokens.Fork();
                            var match = true;
                            var nodes = new List<IParseNode>();
                            foreach (var rule in Rules)
                            {
                                if (rule!.TryParse(tempTokens, out IParseNode? temp))
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
                                node = new ParseNode(this, nodes.ToArray());
                                return true;
                            }
                        }
                        break;
                    case Operator.Or:
                        {
                            var tempTokens = tokens.Fork();
                            var match = false;
                            IParseNode? temp = null;
                            foreach (var rule in Rules)
                            {
                                if (rule!.TryParse(tempTokens, out temp))
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
                                node = new ParseNode(this, temp);
                                return true;
                            }
                        }
                        break;
                    case Operator.Not:
                        {
                            var tempTokens = tokens.Fork();
                            var rule = Rules.Single();
                            if (!rule.TryParse(tempTokens, out IParseNode? _))
                            {
                                node = new ParseNode(this);
                                return true;
                            }
                        }
                        break;
                    case Operator.Option:
                        {
                            var tempTokens = tokens.Fork();
                            var rule = Rules.Single();
                            if (rule.TryParse(tempTokens, out IParseNode? temp))
                            {
                                tokens.Merge(tempTokens);
                                node = new ParseNode(this, temp);
                                return true;
                            }
                            else
                            {
                                node = new ParseNode(this);
                                return true;
                            }
                        }
                    case Operator.Repeat:
                        {
                            var tempTokens = tokens.Fork();
                            var rule = Rules.Single();
                            var nodes = new List<IParseNode>();

                            var repeat = (RepeatRule)this;
                            var min = repeat.Minimum ?? 0;
                            var max = repeat.Maximum;
                            for (var i = 0; i < max; i++)
                            {
                                if (rule.TryParse(tempTokens, out IParseNode? temp))
                                {
                                    nodes.Add(temp);
                                }
                                else if (i < min)
                                {
                                    node = null;
                                    return false;
                                }
                                else
                                {
                                    break;
                                }
                            }

                            tokens.Merge(tempTokens);
                            node = new ParseNode(this, nodes.ToArray());
                            return true;
                        }
                }

                node = null;
                return false;
            }
        }

        internal class RepeatRule : RuleSegment
        {
            public int? Minimum { get; }

            public int? Maximum { get; }

            public RepeatRule(IRule rule, int? minimum, int? maximum)
                : base(Operator.Repeat, rule)
            {
                Minimum = minimum;
                Maximum = maximum;
            }
        }

        internal interface INamedRule : IRule
        {
            string Name { get; }
        }

        internal class NamedRule : INamedRule
        {
            public string Name { get; }

            public RuleSegment Rule { get; }

            public NamedRule(string name, RuleSegment rule)
            {
                Name = name;
                Rule = rule;
            }

            public bool TryParse(TokenList tokens, [NotNullWhen(true)] out IParseNode? node)
            {
                var tempTokens = tokens.Fork();
                if (Rule.TryParse(tempTokens, out IParseNode? temp))
                {
                    tokens.Merge(tempTokens);
                    node = new ParseNode(Rule, temp);
                    return true;
                }

                node = null;
                return false;
            }
        }

        internal class NameRule : INamedRule
        {
            private readonly Parser _parser;
            private INamedRule? _rule;

            public string Name { get; }

            public INamedRule Rule => _rule ??= _parser.GetRule(Name);

            public NameRule(Parser parser, string name)
            {
                _parser = parser;
                Name = name;
            }

            public bool TryParse(TokenList tokens, [NotNullWhen(true)] out IParseNode? node) => Rule.TryParse(tokens, out node);
        }

        internal interface IParseNode
        {

        }

        internal class TokenNode : IParseNode
        {
            public PatternRule Rule { get; }
            public Token Token { get; }

            public TokenNode(PatternRule rule, Token token)
            {
                Rule = rule;
                Token = token;
            }
        }

        internal class ParseNode : IParseNode
        {
            public IRule Rule { get; }
            public IReadOnlyList<IParseNode> Children { get; }

            public ParseNode(RuleSegment rule, params IParseNode[] nodes)
            {
                var nodeCount = nodes.Length;

                static void assert(bool condition, string? message = null)
                {
                    if (!condition)
                    {
                        throw message == null ? new Exception() : new Exception(message);
                    }
                }

                switch (rule.Operator)
                {
                    case Operator.And:
                        assert(nodeCount == rule.Rules.Count);
                        break;
                    case Operator.Or:
                        assert(nodeCount == 1);
                        break;
                    case Operator.Not:
                        assert(nodeCount == 0);
                        break;
                }

                Rule = rule;
                Children = nodes;
            }

            public ParseNode(NamedRule rule, IParseNode node)
            {
                Rule = rule;
                Children = new[] { node };
            }
        }
    }
}
