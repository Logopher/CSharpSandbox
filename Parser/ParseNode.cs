﻿namespace CSharpSandbox.Parsing;

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
        var first = path[0];
        var rest = path[1..];
        if (Children.Count <= first)
        {
            throw new Exception();
        }

        var child = Children[first];
        if (0 < rest.Length)
        {
            if (child is not ParseNode pnode)
            {
                throw new Exception();
            }

            return pnode.Get(rest);
        }
        else
        {
            return child;
        }
    }
}
