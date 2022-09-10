﻿using CSharpSandbox.Common;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace CSharpSandbox.Parsing;

public sealed class TokenList : IReadOnlyList<Token>
{
    public static TokenList Create(IEnumerable<Token> tokens) => new(tokens.ToArray());
    public static TokenList Create(params Token[] tokens) => Create(tokens);

    readonly TokenList? _parent;

    readonly IReadOnlyList<Token> _tokens;

    readonly List<TokenList> _forks = new();

    int _cursor;

    public int Cursor
    {
        get => _cursor;
        private set
        {
            if (IsDiscarded)
            {
                throw new InvalidOperationException();
            }

            if(_forks.Any())
            {
                throw new InvalidOperationException();
            }

            _cursor = value;
        }
    }

    public int Count => _tokens.Count - Cursor;

    public bool IsDiscarded { get; private set; }

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

    public TokenList Fork()
    {
        if (IsDiscarded)
        {
            throw new InvalidOperationException();
        }

        // For now, only allow 1 fork.
        if (0 < _forks.Count)
        {
            throw new InvalidOperationException();
        }

        var result = new TokenList(this);
        _forks.Add(result);
        return result;
    }

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

        Discard();

        _parent.Cursor = Cursor;
    }

    public void Discard()
    {
        if (IsDiscarded)
        {
            throw new InvalidOperationException();
        }

        IsDiscarded = true;

        (_parent ?? throw new InvalidOperationException())
            ._forks.Remove(this);
    }

    public IEnumerator<Token> GetEnumerator() => _tokens.Skip(Cursor).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    internal bool TryGetCachedMatch(IRule rule, [NotNullWhen(true)] out Token.Match? match)
    {
        if (IsDiscarded)
        {
            throw new InvalidOperationException();
        }

        if (Count == 0)
        {
            match = null;
            return false;
        }

        return this[0].TryGetCachedMatch(rule, out match);
    }

    internal void Reset(Token.Match match) => Cursor = match.Index;
}
