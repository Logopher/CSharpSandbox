using CSharpSandbox.Parsing;
using CSharpSandbox.Wpf.Gestures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

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

        public override string ToString(INamedRule rule, IParseNode node)
        {
            var pnode = node as ParseNode ?? throw new Exception();

            switch (rule.Name)
            {
                case "gesture":
                    var chords = (pnode.Get(0) as ParseNode ?? throw new Exception())
                        .Children;

                    return string.Join(" ", chords);
                case "chord":
                    var modifiers = (pnode.Get(0) as ParseNode ?? throw new Exception())
                        .Children.Select(n =>
                        {
                            var mod = (n as ParseNode ?? throw new Exception())
                                .Get(0) as TokenNode ?? throw new Exception();

                            switch (mod.Token.Lexeme)
                            {
                                case "Ctrl":
                                    return ModifierKeys.Control;
                                case "Alt":
                                    return ModifierKeys.Alt;
                                case "Shift":
                                    return ModifierKeys.Shift;
                                case "Windows":
                                    return ModifierKeys.Windows;
                                default:
                                    throw new Exception();
                            }
                        });

                    var key = (pnode.Get(1) as TokenNode ?? throw new Exception())
                        .Token.Lexeme;

                    return $"{string.Join("+", modifiers)}+{key}";
                default:
                    throw new Exception();
            }
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
