using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpSandbox.Parser
{
    internal class PatternRule : INamedRule
    {
        internal readonly IParser _parser;
        public string Name { get; }
        public Pattern Pattern { get; }

        public PatternRule(IParser parser, string name, Pattern pattern)
        {
            _parser = parser;

            Name = name;
            Pattern = pattern;
        }
    }
}
