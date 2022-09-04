using CSharpSandbox.Parsing;
using CSharpSandbox.Wpf.Gestures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpSandbox.Tests
{
    class StimulusParser : Parser<InputGestureTree.Stimulus[]>
    {
        public StimulusParser(IMetaParser metaParser)
            : base(metaParser, "gesture")
        {

        }

        public override InputGestureTree.Stimulus[] Parse(string input)
        {
            throw new NotImplementedException();
        }

        protected override IParseNode? ParseRuleSegment(RuleSegment rule, TokenList tokens)
        {
            throw new NotImplementedException();
        }
    }

    [TestClass]
    public class WpfTests
    {
        [TestMethod]
        public void ParseInputGestures()
        {
            var grammar = @"
modifier = /Ctrl|Alt|Shift|Windows/;
plus = ""+"";
key = /(?:F[1-9][0-9]?|Space|Tab|Enter|[A-Z0-9!@#$%^&.\\`""'~_()[]{}?=+\/*-])/;

chord = (modifier plus)* key;
gesture = chord+;
";

            var parser = Parser.Generate<StimulusParser, InputGestureTree.Stimulus[]>(grammar, "gesture", mp => new StimulusParser(mp));

            var stimuli = parser.Parse("Ctrl+A Alt+B C");
        }
    }
}
