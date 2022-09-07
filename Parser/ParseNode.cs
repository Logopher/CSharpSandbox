using System.Collections;

namespace CSharpSandbox.Parsing;

public class ParseNode : IParseNode, IReadOnlyList<IParseNode>
{
    public IRule Rule { get; }
    public IReadOnlyList<IParseNode> Children { get; }

    public NodeType NodeType { get; } = NodeType.Rule;

    public int Count => Children.Count;

    public IParseNode this[int index] => Children[index];

    public IEnumerable<IParseNode> this[Range range]
    {
        get
        {
            var start = range.Start.GetOffset(Count);
            var end = range.End.GetOffset(Count);
            return Children.Skip(start).Take(end);
        }
    }

    internal ParseNode(RuleSegment rule, params IParseNode[] nodes)
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

    public TResult Expand<T0, T1, T2, T3, T4, T5, T6, TResult>(Func<T0?, T1?, T2?, T3?, T4?, T5?, T6?, IParseNode[], TResult> func)
        where T0 : IParseNode
        where T1 : IParseNode
        where T2 : IParseNode
        where T3 : IParseNode
        where T4 : IParseNode
        => Expand(Array.Empty<int>(), func);
    public TResult Expand<T0, T1, T2, T3, T4, T5, T6, TResult>(int[] path, Func<T0?, T1?, T2?, T3?, T4?, T5?, T6?, IParseNode[], TResult> func)
        where T0 : IParseNode
        where T1 : IParseNode
        where T2 : IParseNode
        where T3 : IParseNode
        where T4 : IParseNode
    {
        var root = 0 < path.Length
            ? Get(path[0], path[1..]) as ParseNode ?? throw new Exception()
            : this;

        return func(
            (T0?)(0 < root.Children.Count ? root.Get(0) : null),
            (T1?)(1 < root.Children.Count ? root.Get(1) : null),
            (T2?)(2 < root.Children.Count ? root.Get(2) : null),
            (T3?)(3 < root.Children.Count ? root.Get(3) : null),
            (T4?)(4 < root.Children.Count ? root.Get(4) : null),
            (T5?)(5 < root.Children.Count ? root.Get(5) : null),
            (T6?)(6 < root.Children.Count ? root.Get(6) : null),
            root.Children.Skip(7).ToArray());
    }

    public void Expand<T0, T1, T2, T3, T4, T5, T6>(Action<T0?, T1?, T2?, T3?, T4?, T5?, T6?, IParseNode[]> action)
        where T0 : IParseNode
        where T1 : IParseNode
        where T2 : IParseNode
        where T3 : IParseNode
        where T4 : IParseNode
        => Expand(Array.Empty<int>(), action);
    public void Expand<T0, T1, T2, T3, T4, T5, T6>(int[] path, Action<T0?, T1?, T2?, T3?, T4?, T5?, T6?, IParseNode[]> action)
        where T0 : IParseNode
        where T1 : IParseNode
        where T2 : IParseNode
        where T3 : IParseNode
        where T4 : IParseNode
    {
        var root = 0 < path.Length
            ? Get(path[0], path[1..]) as ParseNode ?? throw new Exception()
            : this;

        action(
            (T0?)(0 < root.Children.Count ? root.Get(0) : null),
            (T1?)(1 < root.Children.Count ? root.Get(1) : null),
            (T2?)(2 < root.Children.Count ? root.Get(2) : null),
            (T3?)(3 < root.Children.Count ? root.Get(3) : null),
            (T4?)(4 < root.Children.Count ? root.Get(4) : null),
            (T5?)(5 < root.Children.Count ? root.Get(5) : null),
            (T6?)(6 < root.Children.Count ? root.Get(6) : null),
            root.Children.Skip(7).ToArray());
    }

    internal IList<T> ToList<T>(params int[] path)
        where T : class, IParseNode
    {
        var root = 0 < path.Length
            ? (ParseNode)Get(path[0], path[1..])
            : this;

        return new[] { (T)root.Get(0) }
            .Concat((root.Get(1) as ParseNode ?? throw new Exception())
                .Select(n => (T)((ParseNode)n).Get(1)))
            .ToArray();
    }

    public IParseNode Get(int first, params int[] rest)
    {
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

            return pnode.Get(rest[0], rest[1..]);
        }
        else
        {
            return child;
        }
    }

    public override string ToString() => Rule.ToString(this);

    public IEnumerator<IParseNode> GetEnumerator() => Children.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
