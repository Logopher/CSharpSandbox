using System.Collections;

namespace CSharpSandbox.Parser;

public sealed class TokenList : IList<Token>
{
    readonly List<Token> _tokens = new();

    public int Cursor { get; set; }

    public void Commit()
    {
        _tokens.RemoveRange(0, Cursor);
        Cursor = 0;
    }

    public TokenList Fork() => new(Range(Cursor));

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

    private IEnumerable<Token> Range(int index) => Range(index, Count - index);

    public IEnumerator<Token> GetEnumerator()
    {
        for (var i = Cursor; i < Count; i++)
        {
            yield return _tokens[i];
        }
    }

    public TokenList()
    {
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
    public Token this[int index]
    {
        get => _tokens[index];
        set => _tokens[index] = value;
    }

    public int Count => _tokens.Count;

    public void Add(Token item) => _tokens.Add(item);

    public void AddRange(IEnumerable<Token> collection) => _tokens.AddRange(collection);

    bool ICollection<Token>.IsReadOnly => false;

    void ICollection<Token>.Clear() => _tokens.Clear();

    bool ICollection<Token>.Contains(Token item) => _tokens.Contains(item);

    void ICollection<Token>.CopyTo(Token[] array, int arrayIndex) => _tokens.CopyTo(array, arrayIndex);

    int IList<Token>.IndexOf(Token item) => _tokens.IndexOf(item);

    void IList<Token>.Insert(int index, Token item) => _tokens.Insert(index, item);

    bool ICollection<Token>.Remove(Token item) => _tokens.Remove(item);

    void IList<Token>.RemoveAt(int index) => _tokens.RemoveAt(index);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    #endregion
}
