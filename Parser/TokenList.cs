using System.Collections;

namespace CSharpSandbox.Parsing;

public sealed class TokenList : IList<Token>
{
    readonly List<Token> _tokens = new();

    public int Cursor { get; set; }

    public int Count => _tokens.Count - Cursor;

    bool ICollection<Token>.IsReadOnly => false;

    public Token this[int index]
    {
        get => _tokens[index + Cursor];
        set => _tokens[index + Cursor] = value;
    }

    public override string ToString() => string.Join(" ", this);

    int IList<Token>.IndexOf(Token item) => _tokens.IndexOf(item, Cursor) - Cursor;

    void ICollection<Token>.CopyTo(Token[] array, int arrayIndex) => _tokens.CopyTo(Cursor, array, arrayIndex, Count);

    void IList<Token>.Insert(int index, Token item) => _tokens.Insert(index + Cursor, item);

    void IList<Token>.RemoveAt(int index) => _tokens.RemoveAt(index + Cursor);

    bool ICollection<Token>.Contains(Token item) => _tokens.IndexOf(item, Cursor) != -1;

    bool ICollection<Token>.Remove(Token item)
    {
        var index = _tokens.IndexOf(item, Cursor);
        var result = index != -1;
        if (result)
        {
            _tokens.RemoveAt(index);
        }
        return result;
    }

    void ICollection<Token>.Clear()
    {
        _tokens.Clear();
        Cursor = 0;
    }

    public void Commit()
    {
        _tokens.RemoveRange(0, Cursor);
        Cursor = 0;
    }

    public TokenList Fork() => new(this);

    public void Merge(TokenList tokens)
    {
        if (tokens.Cursor < Cursor)
        {
            throw new Exception();
        }

        Cursor = tokens.Cursor;
    }

    private IEnumerable<Token> Range(int index, int count)
    {
        if (count < index)
        {
            throw new IndexOutOfRangeException();
        }

        for (var i = index; i < count; i++)
        {
            yield return _tokens[i];
        }
    }

    private IEnumerable<Token> Range(int index) => Range(index, Count);

    public IEnumerator<Token> GetEnumerator()
    {
        for (var i = Cursor; i < _tokens.Count; i++)
        {
            yield return _tokens[i];
        }
    }

    public TokenList()
    {
    }

    public TokenList(TokenList other)
    {
        _tokens = other._tokens.ToList();
        Cursor = other.Cursor;
    }

    public TokenList(IEnumerable<Token> tokens)
    {
        _tokens = tokens.ToList();
    }

    public TokenList(params Token[] tokens)
        : this((IEnumerable<Token>)tokens)
    {
    }

    #region Everything else is forwarded to _tokens.
    public void Add(Token item) => _tokens.Add(item);

    public void AddRange(IEnumerable<Token> collection) => _tokens.AddRange(collection);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    #endregion
}
