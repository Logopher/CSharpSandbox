using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpSandbox.Parser
{
    internal class NamedRule : INamedRule
    {
        internal readonly IParser _parser;

        public string Name { get; }

        public RuleSegment Rule { get; }

        public NamedRule(IParser parser, string name, RuleSegment rule)
        {
            _parser = parser;

            Name = name;
            Rule = rule;
        }
    }

    internal class NameRule : INamedRule
    {
        private readonly IParser _parser;
        private INamedRule? _rule;

        public string Name { get; }

        public INamedRule Rule => _rule ??= _parser.GetRule(Name) ?? throw new Exception();

        public NameRule(IParser parser, string name)
        {
            _parser = parser;
            Name = name;
        }
    }
}
