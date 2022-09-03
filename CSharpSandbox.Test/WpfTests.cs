using CSharpSandbox.Parser;
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

    internal class WpfTests
    {
        [TestMethod]
        void ParseInputGestures()
        {

        }
    }
}
