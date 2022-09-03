using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpSandbox.Parser
{
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

        public IParseNode? Get(params int[] path)
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
