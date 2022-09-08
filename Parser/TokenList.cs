using CSharpSandbox.Common;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace CSharpSandbox.Parsing;

public sealed class TokenList : IReadOnlyList<Token>
{
    public static TokenList Create(IEnumerable<Token> tokens) => new(tokens.ToArray());
    public static TokenList Create(params Token[] tokens) => Create(tokens);

    readonly TokenList? _parent;

    readonly IReadOnlyList<Token> _tokens;

    public int Cursor { get; private set; }

    public int Count => _tokens.Count - Cursor;

    public Token this[int index] => _tokens[index + Cursor];

    public IEnumerable<Token> this[Range range]
    {
        get
        {
            var start = range.Start.GetOffset(Count) + Cursor;
            var end = range.End.GetOffset(Count);
            return _tokens.Skip(start).Take(end - start);
        }
    }

    private TokenList(TokenList parent)
    {
        _parent = parent;
        _tokens = parent._tokens;

        Cursor = parent.Cursor;
    }
    private TokenList(IReadOnlyList<Token> tokens) => _tokens = tokens.ToList();

    public override string ToString() => string.Join(Mundane.EmptyString, this);

    public void Advance(int count = 1)
    {
        if (count <= 0)
        {
            throw new InvalidOperationException();
        }

        Cursor += count;
    }

    public TokenList Fork() => new(this);

    public void Merge()
    {
        if (_parent == null)
        {
            throw new InvalidOperationException();
        }

        if (Cursor < _parent.Cursor)
        {
            throw new Exception();
        }

        _parent.Cursor = Cursor;
    }

    public IEnumerator<Token> GetEnumerator() => _tokens.Skip(Cursor).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    internal bool TryGetCachedMatch(IRule rule, [NotNullWhen(true)] out Token.Match? match)
    {
        if (Count == 0)
        {
            match = null;
            return false;
        }

        return this[0].TryGetCachedMatch(rule, out match);
    }

    internal void Reset(Token.Match match) => Cursor = match.Index;
}
