using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpSandbox.Parser
{
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
}
