using CSharpSandbox.Common;
using Microsoft.Extensions.Logging;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace CSharpSandbox.Parsing;

public sealed class TokenList : IReadOnlyList<Token>, IDisposable
{
    static readonly ILogger CurrentLogger = Toolbox.LoggerFactory.CreateLogger<TokenList>();
    public static TokenList Create(IEnumerable<Token> tokens) => new(tokens.ToArray());
    public static TokenList Create(params Token[] tokens) => Create(tokens);

    static readonly Dictionary<Guid, StackFrame> _stackFrames = new();

    readonly Guid _guid;

    TokenList? _parent;

    readonly IReadOnlyList<Token> _tokens;

    readonly List<TokenList> _forks = new();

    int _cursor;

    public int Cursor
    {
        get => _cursor;
        private set
        {
            if (IsDisposed)
            {
                throw new InvalidOperationException();
            }

            if (_forks.Any())
            {
                throw new InvalidOperationException();
            }

            _cursor = value;
        }
    }

    public int Count => _tokens.Count - Cursor;

    public bool IsDisposed { get; private set; }

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
        _guid = Guid.NewGuid();

        _parent = parent;
        _tokens = parent._tokens;

        Cursor = parent.Cursor;

        TrackUsage();
    }

    private TokenList(IReadOnlyList<Token> tokens)
    {
        _guid = Guid.NewGuid();

        _tokens = tokens.ToList();

        TrackUsage();
    }

    ~TokenList()
    {
        if (!IsDisposed)
        {
            var frame = _stackFrames[_guid];
            CurrentLogger.LogWarning("The TokenList object created here was never disposed: {Frame}", frame);
            Debugger.Break();
        }
        _stackFrames.Remove(_guid);
    }

    void TrackUsage()
    {
        var frame = new StackTrace(true)
            .GetFrames()
            .First(f =>
            {
                var filepath = f.GetFileName();
                if (filepath == null)
                {
                    return false;
                }
                return Path.GetFileName(filepath) != $"{GetType().Name}.cs";
            });

        _stackFrames.Add(_guid, frame);
    }

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
        if (IsDisposed)
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

        var parent = _parent;

        Detach();

        parent.Cursor = Cursor;
    }

    private void Detach()
    {
        if (_parent == null)
        {
            throw new InvalidOperationException();
        }

        _parent._forks.Remove(this);
        _parent = null;
    }

    public IEnumerator<Token> GetEnumerator() => _tokens.Skip(Cursor).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    internal bool TryGetCachedMatch(IRule rule, [NotNullWhen(true)] out Token.Match? match)
    {
        if (IsDisposed)
        {
            throw new InvalidOperationException();
        }

        if (0 < _forks.Count)
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

    public void Dispose()
    {
        if (IsDisposed)
        {
            throw new InvalidOperationException();
        }

        if (0 < _forks.Count)
        {
            throw new InvalidOperationException();
        }

        IsDisposed = true;

        if (_parent != null)
        {
            Detach();
        }
    }
}
