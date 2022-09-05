using CSharpSandbox.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpSandbox.Parsing;

public class PatternRule : INamedRule
{
    internal readonly IParser _parser;
    public string Name { get; }
    public Pattern Pattern { get; }

    internal PatternRule(IParser parser, string name, Pattern pattern)
    {
        _parser = parser;

        Name = name;
        Pattern = pattern;
    }

    public override string ToString() => Name;

    public string ToString(IParseNode parseNode) => ((TokenNode)parseNode).ToString();
}
