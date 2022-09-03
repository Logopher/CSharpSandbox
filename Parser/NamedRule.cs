using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpSandbox.Parser
{
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

    internal class NameRule<TResult> : INamedRule
    {
        private readonly IParser<TResult> _parser;
        private INamedRule? _rule;

        public string Name { get; }

        public INamedRule Rule => _rule ??= _parser.GetRule(Name);

        public NameRule(IParser<TResult> parser, string name)
        {
            _parser = parser;
            Name = name;
        }
    }
}
