using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CSharpSandbox.Common
{
    public enum Operator
    {
        And,
        Or,
        Not,
        Option,
        Repeat,
    }

    public interface IRule
    {
    }

    public interface INamedRule : IRule
    {
        string Name { get; }
    }

    public interface IParseNode
    {
        IRule Rule { get; }
    }

    public abstract class SemanticParser<TResult>
    {
        Dictionary<INamedRule, Func<IParseNode, RuleSegment>> _directory;

        private readonly LexicalParser<SemanticParser<TResult>, TResult> _lexicalParser;
        protected LexicalParser<SemanticParser<TResult>, TResult> LexicalParser => _lexicalParser;

        protected internal INamedRule Root { get; }

        public SemanticParser(LexicalParser<SemanticParser<TResult>, TResult> parser, string rootName)
        {
            Root = parser.GetRule(rootName);

            _lexicalParser = parser;

            _directory = new Dictionary<INamedRule, Func<IParseNode, RuleSegment>>();
        }

        public abstract TResult Parse(IParseNode node);

        protected void DirectSyntax(string name, Func<IParseNode, RuleSegment> mapping) => _directory.Add(_lexicalParser.GetRule(name), mapping);

        protected RuleSegment Translate(INamedRule rule, IParseNode node) => _directory[rule](node);
    }

    public static class LexicalParser
    {
        public static LexicalParser<TSemanticParser, TResult> Create<TSemanticParser, TResult>(
            Func<LexicalParser<TSemanticParser, TResult>, TSemanticParser> cstor,
            string grammar)
            where TSemanticParser : SemanticParser<TResult>
        {
            return new LexicalParser<TSemanticParser, TResult>(cstor, grammar);
        }
    }

    public class LexicalParser<TSemanticParser, TResult>
        where TSemanticParser : SemanticParser<TResult>
    {
        private TSemanticParser? _semanticParser;
        public TSemanticParser SemanticParser => _semanticParser ?? throw new InvalidOperationException();

        internal static RuleSegment And(params IRule[] rules) => RuleSegment.And(rules);

        internal static RuleSegment Or(params IRule[] rules) => RuleSegment.Or(rules);

        internal static RuleSegment Not(IRule rule) => RuleSegment.Not(rule);

        internal static RuleSegment Option(IRule rule) => RuleSegment.Option(rule);

        internal static RuleSegment RepeatRange(IRule rule, int? minimum = null, int? maximum = null) => RuleSegment.RepeatRange(rule, minimum, maximum);

        internal static RuleSegment Repeat0(IRule rule) => RepeatRange(rule, 0);

        internal static RuleSegment Repeat1(IRule rule) => RepeatRange(rule, 1);

        readonly Dictionary<string, PatternRule> _patternRules = new();
        readonly Dictionary<string, INamedRule> _rules = new();
        readonly Dictionary<string, NameRule> _lazyRules = new();
        readonly Dictionary<Type, Func<IRule, TokenList, IParseNode?>> _typeRules = new();

        // This is the bootstrapping constructor. It exists to allow the _metaParser to be constructed.
        internal LexicalParser(Func<LexicalParser<TSemanticParser, TResult>, TSemanticParser> cstor)
        {
            _semanticParser = cstor(this);

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

            AddTypeRule<NamedRule>((IRule rule, TokenList tokens) =>
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


            AddTypeRule<NamedRule>((IRule rule, TokenList tokens) =>
            {
                var self = (RuleSegment)rule;
                switch (self.Operator)
                {
                    case Operator.And:
                        {
                            var tempTokens = tokens.Fork();
                            var match = true;
                            var nodes = new List<IParseNode>();
                            foreach (var r in self.Rules)
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
                                return new ParseNode(self, nodes.ToArray());
                            }
                        }
                        break;
                    case Operator.Or:
                        {
                            var tempTokens = tokens.Fork();
                            var match = false;
                            IParseNode? temp = null;
                            foreach (var r in self.Rules)
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
                                return new ParseNode(self, temp!);
                            }
                        }
                        break;
                    case Operator.Not:
                        {
                            var tempTokens = tokens.Fork();
                            var r = self.Rules.Single();
                            var temp = Parse(r, tempTokens);
                            if (temp == null)
                            {
                                return new ParseNode(self);
                            }
                        }
                        break;
                    case Operator.Option:
                        {
                            var tempTokens = tokens.Fork();
                            var r = self.Rules.Single();
                            var temp = Parse(r, tempTokens);
                            if (temp != null)
                            {
                                tokens.Merge(tempTokens);
                                return new ParseNode(self, temp);
                            }
                            else
                            {
                                return new ParseNode(self);
                            }
                        }
                    case Operator.Repeat:
                        {
                            var tempTokens = tokens.Fork();
                            var r = self.Rules.Single();
                            var nodes = new List<IParseNode>();

                            var repeat = (RepeatRule)self;
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
                            return new ParseNode(self, nodes.ToArray());
                        }
                }

                return null;
            });
        }

        public LexicalParser(Func<LexicalParser<TSemanticParser, TResult>, TSemanticParser> cstor, string grammar)
            : this(cstor)
        {
            var tokens = Tokenize(grammar);
            var node = Parse(SemanticParser.Root, tokens);
            if (node != null)
            {
                SemanticParser.Parse(node);
            }
        }

        internal void AddTypeRule<TRule>(Func<IRule, TokenList, IParseNode?> rule) where TRule : IRule => _typeRules.Add(typeof(TRule), rule);

        internal IParseNode? Parse<TRule>(TRule rule, TokenList tokens)
            where TRule : IRule
            => _typeRules[typeof(TRule)](rule, tokens);

        internal IParseNode? Parse(string ruleName, string input)
            => Parse(GetRule(ruleName), Tokenize(input));

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

        public void DefineLiteral(string name, string literal) => DefinePattern(name, Pattern.FromLiteral(literal));

        public void DefinePattern(string name, string pattern) => DefinePattern(name, new Pattern(pattern));

        internal PatternRule DefinePattern(string name, Pattern pattern)
        {
            var rule = new PatternRule(name, pattern);
            _patternRules.Add(name, rule);
            _rules.Add(name, rule);
            return rule;
        }

        public void DefineRule(string name, string rule)
        {
            var n = Parse("baseExpr2", rule);
            if (n != null)
            {
                if (n is not ParseNode node)
                {
                    throw new Exception();
                }

                node.ToString();
            }
        }

        internal NamedRule DefineRule(string name, RuleSegment segment)
        {
            var rule = new NamedRule(name, segment);
            _rules.Add(name, rule);
            return rule;
        }

        internal PatternRule GetPattern(string name)
        {
            if (!_patternRules.TryGetValue(name, out PatternRule? pattern))
            {
                throw new KeyNotFoundException();
            }

            return pattern;
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

        public INamedRule GetRule(string name)
        {
            if (!_rules.TryGetValue(name, out INamedRule? rule))
            {
                throw new KeyNotFoundException();
            }
            return rule;
        }

        public void Dispose()
        {
            _semanticParser = null;
        }

        internal class NameRule : INamedRule
        {
            private readonly LexicalParser<TSemanticParser, TResult> _parser;
            private INamedRule? _rule;

            public string Name { get; }

            public INamedRule Rule => _rule ??= _parser.GetRule(Name);

            public NameRule(LexicalParser<TSemanticParser, TResult> parser, string name)
            {
                _parser = parser;
                Name = name;
            }
        }
    }

    internal class BootstrapSemanticParser<TResult> : SemanticParser<LexicalParser<SemanticParser<TResult>, TResult>>
    {
        private readonly LexicalParser<SemanticParser<TResult>, TResult> _constructedParser;

        public BootstrapSemanticParser(
            LexicalParser<SemanticParser<LexicalParser<SemanticParser<TResult>, TResult>>, LexicalParser<SemanticParser<TResult>, TResult>> lexicalParser,
            LexicalParser<SemanticParser<TResult>, TResult> constructedParser)
            : base(lexicalParser, "lexicon")
        {
            _constructedParser = constructedParser;

            DirectSyntax("and", ParseAnd);
            DirectSyntax("or", ParseOr);
            DirectSyntax("not", ParseNot);
            DirectSyntax("option", ParseOption);
            DirectSyntax("parens", ParseParens);
            DirectSyntax("repeat0", ParseRepeat0);
            DirectSyntax("repeat1", ParseRepeat1);
            DirectSyntax("repeateRange", ParseRepeatRange);
        }

        RuleSegment ParseAnd(IParseNode node)
        {
            var pnode = (ParseNode)node;
            return RuleSegment.And(pnode.Children.Select(ParseRuleSegment).ToArray());
        }

        RuleSegment ParseOr(IParseNode node)
        {
            var pnode = (ParseNode)node;
            return RuleSegment.Or(pnode.Children.Select(ParseRuleSegment).ToArray());
        }

        RuleSegment ParseNot(IParseNode node)
        {
            var pnode = (ParseNode)node;
            return RuleSegment.Not(ParseRuleSegment(pnode.Children.Single()));
        }

        RuleSegment ParseOption(IParseNode node)
        {
            var pnode = (ParseNode)node;
            return RuleSegment.Option(ParseRuleSegment(pnode.Children.Single()));
        }

        RuleSegment ParseParens(IParseNode node)
        {
            var pnode = (ParseNode)node;
            return ParseRuleSegment(pnode.Children.Single());
        }

        RuleSegment ParseRepeat0(IParseNode node)
        {
            var pnode = (ParseNode)node;
            return RuleSegment.Repeat0(ParseRuleSegment(pnode.Children[0]));
        }

        RuleSegment ParseRepeat1(IParseNode node)
        {
            var pnode = (ParseNode)node;
            return RuleSegment.Repeat1(ParseRuleSegment(pnode.Children[0]));
        }

        RuleSegment ParseRepeatRange(IParseNode node)
        {
            var pnode = (ParseNode)node;
            var inner = ParseRuleSegment(pnode.Get(0, 0));

            var range = (ParseNode)pnode.Get(0, 1, 0);
            var (_, commaIndex) = range.Children
                .Select((n, i) => (n, i))
                .First(tup => tup.n is TokenNode t && t.Token.Lexeme == ",");

            var minNode = (TokenNode)range.Children[commaIndex - 1];
            int? min = null;
            if (minNode.Token.Lexeme != "{")
            {
                min = int.Parse(minNode.Token.Lexeme);
            }

            var maxNode = (TokenNode)range.Children[commaIndex + 1];
            int? max = null;
            if (maxNode.Token.Lexeme != "}")
            {
                max = int.Parse(maxNode.Token.Lexeme);
            }
            return RuleSegment.RepeatRange(inner, min, max);
        }

        protected internal RuleSegment ParseRuleSegment(IParseNode node)
        {
            if (node.Rule is NamedRule named)
            {
                return Translate(named, node);
            }
            else if (node.Rule is LexicalParser<SemanticParser<TResult>, TResult>.NameRule lazy)
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

        public void ParseToken(IParseNode node)
        {
            var pnode = (ParseNode)node;

            var stmt = (ParseNode)pnode.Get(0);
            var name = ((TokenNode)stmt.Get(0)).Token.Lexeme;
            var value = (TokenNode)stmt.Get(2, 0);
            var valueLex = value.Token.Lexeme;
            valueLex = valueLex[1..(valueLex.Length - 1)];
            switch (value.Rule.Name)
            {
                case "literal":
                    _constructedParser.DefineLiteral(name, valueLex);
                    break;
                case "pattern":
                    _constructedParser.DefinePattern(name, valueLex);
                    break;
                default:
                    throw new Exception();
            }
        }

        public void ParseRule(IParseNode node)
        {
            var pnode = (ParseNode)node;

            var stmt = (ParseNode)pnode.Get(0);
            var name = ((TokenNode)stmt.Get(0)).Token.Lexeme;
            var value = ParseRuleSegment(stmt.Get(2));

            _constructedParser.DefineRule(name, value);
        }

        public override LexicalParser<SemanticParser<TResult>, TResult> Parse(IParseNode node)
        {
            var pnode = (ParseNode)node;
            var tokens = (ParseNode)pnode.Get(0, 0, 0);
            var rules = (ParseNode)pnode.Get(0, 1, 0);

            foreach (var token in tokens.Children)
            {
                ParseToken(token);
            }

            foreach (var rule in rules.Children)
            {
                ParseRule(rule);
            }

            return _constructedParser;
        }
    }

    internal class BootstrapLexicalParser<TResult> : LexicalParser<SemanticParser<LexicalParser<SemanticParser<TResult>, TResult>>, LexicalParser<SemanticParser<TResult>, TResult>>
    {
        // This constructor replaces the `string grammar` argument to the LexiconSemanticParser constructor with
        // a series of C# instructions. We can't pass a grammar until we can parse a grammar, which is the job of
        // the _metaParser. The grammar which the _metaParser is designed to parse is a modified
        // EBNF (Extended Backus–Naur Form), and the C# below closely mimics the resulting syntax.
        //
        // Eventually we should be able to produce a string describing the compiled grammar,
        // which can be inserted into this comment.
        internal BootstrapLexicalParser(
            Func<LexicalParser<SemanticParser<LexicalParser<SemanticParser<TResult>, TResult>>, LexicalParser<SemanticParser<TResult>, TResult>>, SemanticParser<LexicalParser<SemanticParser<TResult>, TResult>>> cstor)
            : base(cstor)
        {
            // lazy, as in ZZZZZ
            INamedRule Z(string name) => GetLazyRule(name);

            // literals
            PatternRule L(string name, string s) => DefinePattern(name, Pattern.FromLiteral(s));

            // patterns
            PatternRule P(string name, string s) => DefinePattern(name, new Pattern(s));

            // rules
            NamedRule R(string name, RuleSegment rule) => DefineRule(name, rule);

            var literal = P("literal", @"""(?:\""|[^""])+""");
            var pattern = P("pattern", @"/(?:\/|[^/])+/");
            var name = P("name", "[a-zA-Z_][a-zA-Z0-9_]+");
            var posInt = P("posInt", "[0-9]+");

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
            var andExpr = R("andExpr", RepeatRange(Z("baseExpr2"), minimum: 2));
            var orExpr = R("orExpr", And(Z("baseExpr2"), Repeat1(And(pipe, Z("baseExpr2")))));
            var operExpr = R("operExpr", Or(notExpr, andExpr, orExpr, repeatRange, repeat0, repeat1));
            var baseExpr2 = R("baseExpr2", Or(operExpr, baseExpr));
            var parenExpr = R("parenExpr", And(lParen, baseExpr2, rParen));

            var token = R("token", And(name, assmt, Or(literal, pattern), stmtEnd));
            var rule = R("rule", And(name, assmt, baseExpr2, stmtEnd));

            var tokenSection = R("tokenSection", RepeatRange(token));
            var ruleSection = R("ruleSection", RepeatRange(rule));

            var lexicon = R("lexicon", And(tokenSection, ruleSection));
        }

        public LexicalParser<SemanticParser<TResult>, TResult> Parse(string grammar)
        {
            var tokens = Tokenize(grammar);
            var node = Parse(SemanticParser.Root, tokens);
            if (node != null)
            {
                return SemanticParser.Parse(node);
            }

            throw new Exception();
        }
    }

    public class BootstrapParser<TResult>
    {
        private readonly LexicalParser<SemanticParser<TResult>, TResult> _constructedParser;
        private readonly BootstrapLexicalParser<TResult> _lexicalParser;
        private readonly BootstrapSemanticParser<TResult> _semanticParser;

        public BootstrapParser(LexicalParser<SemanticParser<TResult>, TResult> parser)
        {
            _constructedParser = parser;
            _lexicalParser = new BootstrapLexicalParser<TResult>(lp => new BootstrapSemanticParser<TResult>(lp, parser));
            _semanticParser = (BootstrapSemanticParser<TResult>)_lexicalParser.SemanticParser;
        }
        public LexicalParser<SemanticParser<TResult>, TResult> Parse(string grammar) => _lexicalParser.Parse(grammar);
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

    internal class PatternRule : INamedRule
    {
        public string Name { get; }
        public Pattern Pattern { get; }

        public PatternRule(string name, Pattern pattern)
        {
            Name = name;
            Pattern = pattern;
        }
    }

    public class RuleSegment : IRule
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

        public static RuleSegment RepeatRange(IRule rule, int? minimum = null, int? maximum = null) => new RepeatRule(rule, minimum, maximum);

        public static RuleSegment Repeat0(IRule rule) => RepeatRange(rule, 0);

        public static RuleSegment Repeat1(IRule rule) => RepeatRange(rule, 1);
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

    internal class NamedRule : INamedRule
    {
        public string Name { get; }

        public RuleSegment Rule { get; }

        public NamedRule(string name, RuleSegment rule)
        {
            Name = name;
            Rule = rule;
        }
    }

    internal class TokenNode : IParseNode
    {
        public PatternRule Rule { get; }
        public Token Token { get; }

        IRule IParseNode.Rule => Rule;

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

        public IParseNode Get(params int[] path)
        {
            if (1 < path.Length)
            {
                return ((ParseNode)Children[path[0]]).Get(path[1..]);
            }
            else
            {
                return Children[path[0]];
            }
        }
    }
}