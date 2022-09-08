using System.Collections;

namespace CSharpSandbox.Parsing;

public class ParseNode : IParseNode, IReadOnlyList<IParseNode>
{
    /// <summary>
    /// The rule which produced this node.
    /// </summary>
    public IRule Rule { get; }

    /// <summary>
    /// A read-only list of child nodes.
    /// </summary>
    public IReadOnlyList<IParseNode> Children { get; }

    /// <summary>
    /// The kind of node, also the kind of rule which produced it. 
    /// </summary>
    public NodeType NodeType { get; }

    /// <summary>
    /// The number of children this node has.
    /// </summary>
    public int Count => Children.Count;

    /// <summary>
    /// Gets a child node of this node.
    /// </summary>
    /// <param name="index">An index.</param>
    /// <returns>A child node of this node.</returns>
    public IParseNode this[int index] => Children[index];

    /// <summary>
    /// Gets a sub-sequence of nodes. This allows prettier syntax than the equivalent Linq (partially shown in the implementation).
    /// </summary>
    /// <param name="range">A <see cref="Range"/>.</param>
    /// <returns>A sub-sequence of nodes.</returns>
    public IEnumerable<IParseNode> this[Range range]
    {
        get
        {
            var start = range.Start.GetOffset(Count);
            var end = range.End.GetOffset(Count);
            return this.Skip(start).Take(end - start);
        }
    }

    /// <summary>
    /// Constructs a parse node representing that some matched text was parsed into a hierarchy according to a specific rule.
    /// </summary>
    /// <param name="rule">A rule specified as a portion of a larger rule.</param>
    /// <param name="nodes">A sequence of parse nodes representing text which conforms to <paramref name="rule"/>.</param>
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

        NodeType = NodeType.Segment;
        Rule = rule;
        Children = nodes;
    }

    /// <summary>
    /// Constructs a parse node representing that some matched text was parsed according to a rule by a specific name. <paramref name="node"/>
    /// represents the text which was matched, and the rule describing valid inputs, with no awareness that the rule is named.
    /// </summary>
    /// <param name="rule">A rule specified, and named, in a grammar.</param>
    /// <param name="node">A parse node representing text which conforms to <paramref name="rule"/>.</param>
    internal ParseNode(NamedRule rule, IParseNode node)
    {
        NodeType = NodeType.Rule;
        Rule = rule;
        Children = new[] { node };
    }


    /// <summary>
    /// Expands a node, exposing its children to a callback. As many as 7 nodes will each be cast to the corresponding type 
    /// (<typeparamref name="T0"/>...<typeparamref name="T6"/>). Subsequent nodes will be provided in the <see cref="IParseNode[]"/>
    /// at the end of the parameter list.
    /// 
    /// (Additional overloads are planned to allow numbers of nodes other than 7. Because this is a one-size-fits-all method, the callback's
    /// parameters are all nullable, but the null-forgiving operator (<code>node!</code>) is safe as long as you know that position has a node.
    /// If a node of an incompatible type is in that position, an exception will be thrown before the callback is invoked.)
    /// </summary>
    /// <typeparam name="T0"><see cref="IParseNode"/> or a derived type.</typeparam>
    /// <typeparam name="T1"><see cref="IParseNode"/> or a derived type.</typeparam>
    /// <typeparam name="T2"><see cref="IParseNode"/> or a derived type.</typeparam>
    /// <typeparam name="T3"><see cref="IParseNode"/> or a derived type.</typeparam>
    /// <typeparam name="T4"><see cref="IParseNode"/> or a derived type.</typeparam>
    /// <typeparam name="T5"><see cref="IParseNode"/> or a derived type.</typeparam>
    /// <typeparam name="T6"><see cref="IParseNode"/> or a derived type.</typeparam>
    /// <param name="func">A callback to process this node's children.</param>
    /// <returns>The value returned from <paramref name="func"/>.</returns>
    /// <exception cref="InvalidCastException">If any of the necessary nodes are not <see cref="ParseNode"/>s (see <see cref="Get(int, int[])"/>)
    /// or any node is not of the corresponding specified type.</exception>
    public TResult Expand<T0, T1, T2, T3, T4, T5, T6, TResult>(Func<T0?, T1?, T2?, T3?, T4?, T5?, T6?, IParseNode[], TResult> func)
        where T0 : IParseNode
        where T1 : IParseNode
        where T2 : IParseNode
        where T3 : IParseNode
        where T4 : IParseNode
        => Expand(Array.Empty<int>(), func);

    /// <summary>
    /// Expands a node, exposing its children to a callback. As many as 7 nodes will each be cast to the corresponding type 
    /// (<typeparamref name="T0"/>...<typeparamref name="T6"/>). Subsequent nodes will be provided in the <see cref="IParseNode[]"/>
    /// at the end of the parameter list.
    /// 
    /// The <paramref name="path"/> is used to locate some node deeper in the hierarchy which will be expanded instead of this node.
    /// 
    /// (Additional overloads are planned to allow numbers of nodes other than 7. Because this is a one-size-fits-all method, the callback's
    /// parameters are all nullable, but the null-forgiving operator (<code>node!</code>) is safe as long as you know that position has a node.
    /// If a node of an incompatible type is in that position, an exception will be thrown before the callback is invoked.)
    /// </summary>
    /// <typeparam name="T0"><see cref="IParseNode"/> or a derived type.</typeparam>
    /// <typeparam name="T1"><see cref="IParseNode"/> or a derived type.</typeparam>
    /// <typeparam name="T2"><see cref="IParseNode"/> or a derived type.</typeparam>
    /// <typeparam name="T3"><see cref="IParseNode"/> or a derived type.</typeparam>
    /// <typeparam name="T4"><see cref="IParseNode"/> or a derived type.</typeparam>
    /// <typeparam name="T5"><see cref="IParseNode"/> or a derived type.</typeparam>
    /// <typeparam name="T6"><see cref="IParseNode"/> or a derived type.</typeparam>
    /// <param name="path">A sequence of indexes which serve as an address of a node. See <see cref="Get(int, int[])"/>.</param>
    /// <param name="func">A callback to process this node's children.</param>
    /// <returns>The value returned from <paramref name="func"/>.</returns>
    /// <exception cref="InvalidCastException">If any of the necessary nodes are not <see cref="ParseNode"/>s (see <see cref="Get(int, int[])"/>)
    /// or any node is not of the corresponding specified type.</exception>
    public TResult Expand<T0, T1, T2, T3, T4, T5, T6, TResult>(int[] path, Func<T0?, T1?, T2?, T3?, T4?, T5?, T6?, IParseNode[], TResult> func)
        where T0 : IParseNode
        where T1 : IParseNode
        where T2 : IParseNode
        where T3 : IParseNode
        where T4 : IParseNode
    {
        var root = 0 < path.Length
            ? (ParseNode)Get(path[0], path[1..])
            : this;

        return func(
            (T0?)(0 < root.Count ? root[0] : null),
            (T1?)(1 < root.Count ? root[1] : null),
            (T2?)(2 < root.Count ? root[2] : null),
            (T3?)(3 < root.Count ? root[3] : null),
            (T4?)(4 < root.Count ? root[4] : null),
            (T5?)(5 < root.Count ? root[5] : null),
            (T6?)(6 < root.Count ? root[6] : null),
            root[7..].ToArray());
    }

    /// <summary>
    /// Expands a node, exposing its children to a callback. As many as 7 nodes will each be cast to the corresponding type 
    /// (<typeparamref name="T0"/>...<typeparamref name="T6"/>). Subsequent nodes will be provided in the <see cref="IParseNode[]"/>
    /// at the end of the parameter list.
    /// 
    /// (Additional overloads are planned to allow numbers of nodes lower than 7. Because this is a one-size-fits-all method, the callback's
    /// parameters are all nullable, but the null-forgiving operator (<code>node!</code>) is safe as long as you know that position has a node.
    /// If a node of an incompatible type is in that position, an exception will be thrown before the callback is invoked.)
    /// </summary>
    /// <typeparam name="T0"><see cref="IParseNode"/> or a derived type.</typeparam>
    /// <typeparam name="T1"><see cref="IParseNode"/> or a derived type.</typeparam>
    /// <typeparam name="T2"><see cref="IParseNode"/> or a derived type.</typeparam>
    /// <typeparam name="T3"><see cref="IParseNode"/> or a derived type.</typeparam>
    /// <typeparam name="T4"><see cref="IParseNode"/> or a derived type.</typeparam>
    /// <typeparam name="T5"><see cref="IParseNode"/> or a derived type.</typeparam>
    /// <typeparam name="T6"><see cref="IParseNode"/> or a derived type.</typeparam>
    /// <param name="action">A callback to process this node's children.</param>
    /// <exception cref="InvalidCastException">If any of the necessary nodes are not <see cref="ParseNode"/>s (see <see cref="Get(int, int[])"/>)
    /// or any node is not of the corresponding specified type.</exception>
    public void Expand<T0, T1, T2, T3, T4, T5, T6>(Action<T0?, T1?, T2?, T3?, T4?, T5?, T6?, IParseNode[]> action)
        where T0 : IParseNode
        where T1 : IParseNode
        where T2 : IParseNode
        where T3 : IParseNode
        where T4 : IParseNode
        => Expand(Array.Empty<int>(), action);

    /// <summary>
    /// Expands a node, exposing its children to a callback. As many as 7 nodes will each be cast to the corresponding type 
    /// (<typeparamref name="T0"/>...<typeparamref name="T6"/>). Subsequent nodes will be provided in the <see cref="IParseNode[]"/>
    /// at the end of the parameter list.
    /// 
    /// The <paramref name="path"/> is used to locate some node deeper in the hierarchy which will be expanded instead of this node.
    /// 
    /// (Additional overloads are planned to allow numbers of nodes other than 7. Because this is a one-size-fits-all method, the callback's
    /// parameters are all nullable, but the null-forgiving operator (<code>node!</code>) is safe as long as you know that position has a node.
    /// If a node of an incompatible type is in that position, an exception will be thrown before the callback is invoked.)
    /// </summary>
    /// <typeparam name="T0"><see cref="IParseNode"/> or a derived type.</typeparam>
    /// <typeparam name="T1"><see cref="IParseNode"/> or a derived type.</typeparam>
    /// <typeparam name="T2"><see cref="IParseNode"/> or a derived type.</typeparam>
    /// <typeparam name="T3"><see cref="IParseNode"/> or a derived type.</typeparam>
    /// <typeparam name="T4"><see cref="IParseNode"/> or a derived type.</typeparam>
    /// <typeparam name="T5"><see cref="IParseNode"/> or a derived type.</typeparam>
    /// <typeparam name="T6"><see cref="IParseNode"/> or a derived type.</typeparam>
    /// <param name="path">A sequence of indexes which serve as an address of a node. See <see cref="Get(int, int[])"/>.</param>
    /// <param name="action">A callback to process this node's children.</param>
    /// <exception cref="InvalidCastException">If any of the necessary nodes are not <see cref="ParseNode"/>s (see <see cref="Get(int, int[])"/>)
    /// or any node is not of the corresponding specified type.</exception>
    public void Expand<T0, T1, T2, T3, T4, T5, T6>(int[] path, Action<T0?, T1?, T2?, T3?, T4?, T5?, T6?, IParseNode[]> action)
        where T0 : IParseNode
        where T1 : IParseNode
        where T2 : IParseNode
        where T3 : IParseNode
        where T4 : IParseNode
    {
        var root = 0 < path.Length
            ? (ParseNode)Get(path[0], path[1..])
            : this;

        action(
            (T0?)(0 < root.Count ? root[0] : null),
            (T1?)(1 < root.Count ? root[1] : null),
            (T2?)(2 < root.Count ? root[2] : null),
            (T3?)(3 < root.Count ? root[3] : null),
            (T4?)(4 < root.Count ? root[4] : null),
            (T5?)(5 < root.Count ? root[5] : null),
            (T6?)(6 < root.Count ? root[6] : null),
            root[7..].ToArray());
    }

    /// <summary>
    /// <para>
    /// Given a node hierarchy shaped like this:
    /// 
    /// <code>
    /// parent
    ///     child0
    ///     repeater
    ///         container1
    ///             separator
    ///             child1
    ///         container2
    ///             separator
    ///             child2
    ///         ...
    ///         containerN
    ///             separator
    ///             childN
    /// </code>
    /// 
    /// returns an <see cref="IList{T}"/> containing [child0, child1, child2, ..., childN].
    /// </para>
    /// 
    /// <para>
    /// If child1 ... childN are not offset within their containers by 1 (the second element of each container), the overload
    /// <see cref="ToList{T}(int[]?, int)"/> may handle that case. If there is no container and the children are immediately inside the
    /// repeater, no overload is available for that case.
    /// </para>
    /// 
    /// </summary>
    /// <typeparam name="T">Either <see cref="IParseNode"/> or one of its implementing classes.
    /// The nodes will be cast to the specified type, which may produce an exception.</typeparam>
    /// <param name="path">A sequence of indexes which serve as an address of a node. See <see cref="Get(int, int[])"/>.</param>
    /// <returns>A list of the located children.</returns>
    /// <exception cref="InvalidCastException">If any of the necessary nodes (e.g., repeater and container) are not <see cref="ParseNode"/>s
    /// or the targeted nodes are not all of the specified type <typeparamref name="T"/>.</exception>
    public IList<T> ToList<T>(params int[] path)
        where T : class, IParseNode
        => ToList<T>(path, 1);

    /// <summary>
    /// 
    /// <para>
    /// An overload of <see cref="ToList{T}(int[])"/> which allows for a slightly different shape of a node hierarchy. For example,
    /// <code>ToList(null, 3)</code> would expect this shape instead:
    /// 
    /// <code>
    /// parent
    ///     child0
    ///     repeater
    ///         container1
    ///             separator
    ///             separator
    ///             separator
    ///             child1
    ///         container2
    ///             separator
    ///             separator
    ///             separator
    ///             child2
    ///         ...
    ///         containerN
    ///             separator
    ///             separator
    ///             separator
    ///             childN
    /// </code>
    /// 
    /// The <paramref name="restOffset"/> is effectively how many separators come before the child.
    /// </para>
    /// 
    /// </summary>
    /// <typeparam name="T">Either <see cref="IParseNode"/> or one of its implementing classes.
    /// The nodes will be cast to the specified type, which may produce an exception.</typeparam>
    /// <param name="path">A sequence of indexes which serve as an address of a node. Defaults to an empty array.
    /// See <see cref="Get(int, int[])"/>.</param>
    /// <param name="restOffset">The offset of a child within its container. Defaults to 1.</param>
    /// <returns>A list of the located children.</returns>
    /// <exception cref="InvalidCastException">If any of the necessary nodes (e.g., repeater and container) are not <see cref="ParseNode"/>s
    /// or the targeted nodes are not all of the specified type <typeparamref name="T"/>.</exception>
    public IList<T> ToList<T>(int[]? path, int restOffset)
        where T : class, IParseNode
    {
        var pathPrime = path ?? Array.Empty<int>();

        var parent = 0 < pathPrime.Length
            ? (ParseNode)Get(pathPrime[0], pathPrime[1..])
            : this;

        var child1 = (T)parent[0];

        var repeater = (ParseNode)parent[1];

        return new[] { child1 }
            .Concat(repeater.Select(n =>
            {
                var container = (ParseNode)n;
                return (T)container[restOffset];
            }))
            .ToArray();
    }

    /// <summary>
    /// <para>
    /// Locates a node by a sequence of indexes. At least one index (<paramref name="first"/>) must be provided; if 0 indexes were allowed,
    /// the method would uselessly locate this node, the object on which the method was invoked.
    /// </para>
    /// 
    /// <para>
    /// This method is equivalent to <code>Children[first].Children[rest[0]].Children[rest[1]]...Children[rest.Last()]</code> with casts and error
    /// handling added.
    /// </para>
    /// </summary>
    /// <param name="first">An index in the <see cref="Children"/> of the initial <see cref="ParseNode"/>.</param>
    /// <param name="rest">A series of indexes to retrieve successively deeper nodes, each from the <see cref="Children"/>
    /// of the node retrieved by the previous index.</param>
    /// <returns></returns>
    /// <exception cref="Exception">If any index is out of bounds for the corresponding parent node.</exception>
    /// <exception cref="InvalidCastException">If any of the necessary nodes (e.g., repeater and container) are not <see cref="ParseNode"/>s.
    /// </exception>
    public IParseNode Get(int first, params int[] rest)
    {
        if (Count <= first || first < 0)
        {
            throw new Exception();
        }

        var child = this[first];

        return 0 < rest.Length
            ? ((ParseNode)child).Get(rest[0], rest[1..])
            : child;
    }

    /// <summary>
    /// Converts this node into a string representation. This should be equal to the section of text which was originally parsed
    /// to produce this node.
    /// </summary>
    /// <returns>A string representation of this node.</returns>
    public override string ToString() => Rule.ToString(this);

    public IEnumerator<IParseNode> GetEnumerator() => Children.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
