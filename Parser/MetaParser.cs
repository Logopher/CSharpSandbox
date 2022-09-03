﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpSandbox.Parsing
{
    internal static class MetaParser
    {
        public static MetaParser<TParser, TResult> Get<TParser, TResult>(Func<IMetaParser, TParser> cstor)
            where TParser : Parser<TResult>
            => new(cstor);
    }

    internal class MetaParser<TParser, TResult> : Parser<TParser>, IMetaParser
        where TParser : Parser<TResult>
    {
        private readonly Dictionary<INamedRule, Func<IParseNode, RuleSegment>> _directory = new();
        private readonly Dictionary<Type, Func<IRule, TokenList, IParseNode?>> _typeRules = new();
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
            PatternRule L(string name, string s) => DefinePattern(name, Pattern.FromLiteral(s));

            // patterns
            PatternRule P(string name, string s) => DefinePattern(name, new Pattern(s));

            // rules
            INamedRule R(string name, RuleSegment rule)
            {
                DefineRule(name, rule);
                return GetRule(name);
            }

            var literal = P("literal", @"""(?:\\""|[^""])+""");
            var pattern = P("pattern", @"/(?:\\/|[^/])+/");
            var name = P("name", "[a-zA-Z_][a-zA-Z0-9_]+");
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

            var baseExpr = R("baseExpr", Or(name, Z("parenExpr")));
            var range = R("range", Or(And(lCurly, posInt, comma, Option(posInt), rCurly), And(lCurly, comma, posInt, rCurly)));
            var repeatRange = R("repeatRange", And(baseExpr, range));
            var repeat0 = R("repeat0", And(baseExpr, asterisk));
            var repeat1 = R("repeat1", And(baseExpr, plus));
            var option = R("option", And(baseExpr, query));
            var notExpr = R("notExpr", And(excl, baseExpr));
            var andExpr = R("andExpr", RepeatRange(Z("baseExpr2"), minimum: 2));
            var orExpr = R("orExpr", And(Z("baseExpr2"), Repeat1(And(pipe, Z("baseExpr2")))));
            var operExpr = R("operExpr", Or(notExpr, orExpr, repeatRange, repeat0, repeat1));
            var baseExpr2 = R("baseExpr2", Or(operExpr, baseExpr));
            var baseExpr3 = R("baseExpr3", Or(andExpr, baseExpr2));
            R("parenExpr", And(lParen, baseExpr3, rParen));

            var token = R("token", And(name, assmt, Or(literal, pattern), stmtEnd));
            var rule = R("rule", And(name, assmt, baseExpr3, stmtEnd));

            var tokenSection = R("tokenSection", RepeatRange(token));
            var ruleSection = R("ruleSection", RepeatRange(rule));

            R("lexicon", And(tokenSection, ruleSection));
        }

        internal MetaParser(Func<IMetaParser, TParser> cstor)
            : base("lexicon")
        {
            _cstor = cstor;

            ApplyBootsrapGrammar();

            DirectSyntax("andExpr", ResolveAnd);
            DirectSyntax("orExpr", ResolveOr);
            DirectSyntax("notExpr", ResolveNot);
            DirectSyntax("option", ResolveOption);
            DirectSyntax("parenExpr", ResolveParens);
            DirectSyntax("repeat0", ResolveRepeat0);
            DirectSyntax("repeat1", ResolveRepeat1);
            DirectSyntax("repeatRange", ResolveRepeatRange);

            void addTypeRule<TRule>(Func<TRule, TokenList, IParseNode?> rule) where TRule : IRule => _typeRules.Add(typeof(TRule), (r, l) => rule((TRule)r, l));

            addTypeRule((NamedRule self, TokenList tokens) =>
            {
                var tempTokens = tokens.Fork();
                var temp = Parse(self.Rule, tempTokens);
                if (temp != null)
                {
                    tokens.Merge(tempTokens);
                    return new ParseNode(self, temp);
                }

                return null;
            });

            addTypeRule((NameRule self, TokenList tokens) => Parse(self.Rule, tokens));

            addTypeRule((PatternRule self, TokenList tokens) =>
            {
                var first = tokens.First();
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

        RuleSegment ResolveAnd(IParseNode node)
        {
            var pnode = (ParseNode)node;
            return RuleSegment.And(this, pnode.Children.Select(ResolveRuleSegment).ToArray());
        }

        RuleSegment ResolveOr(IParseNode node)
        {
            var pnode = (ParseNode)node;
            return RuleSegment.Or(this, pnode.Children.Select(ResolveRuleSegment).ToArray());
        }

        RuleSegment ResolveNot(IParseNode node)
        {
            var pnode = (ParseNode)node;
            return RuleSegment.Not(this, ResolveRuleSegment(pnode.Children.Single()));
        }

        RuleSegment ResolveOption(IParseNode node)
        {
            var pnode = (ParseNode)node;
            return RuleSegment.Option(this, ResolveRuleSegment(pnode.Children.Single()));
        }

        RuleSegment ResolveParens(IParseNode node)
        {
            var pnode = (ParseNode)node;
            return ResolveRuleSegment(pnode.Children.Single());
        }

        RuleSegment ResolveRepeat0(IParseNode node)
        {
            var pnode = (ParseNode)node;
            return RuleSegment.Repeat0(this, ResolveRuleSegment(pnode.Children[0]));
        }

        RuleSegment ResolveRepeat1(IParseNode node)
        {
            var pnode = (ParseNode)node;
            return RuleSegment.Repeat1(this, ResolveRuleSegment(pnode.Children[0]));
        }

        RuleSegment ResolveRepeatRange(IParseNode node)
        {
            var pnode = (ParseNode)node;
            var inner = pnode.Get(0, 0) as ParseNode ?? throw new Exception();
            var innerRule = ResolveRuleSegment(inner);

            var range = pnode.Get(0, 1, 0) as ParseNode ?? throw new Exception();
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
            return RuleSegment.RepeatRange(this, innerRule, min, max);
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
            => _typeRules[rule.GetType()](rule, Tokenize(input));

        IParseNode? Parse<TRule>(TRule rule, TokenList tokens)
            where TRule : IRule
            => _typeRules[rule.GetType()](rule, tokens);

        IParseNode? Parse(string ruleName, string input)
            => Parse(GetRule(ruleName), Tokenize(input));

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
            var rule = GetRule(name);

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
}
