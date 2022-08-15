using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Text;
using System.Collections.Concurrent;

namespace CSharpSandbox.Shells;

internal class BatchDriver : IShellDriver
{
    private Action<string, bool>? _print;
    private Process? _shellProcess;
    private StreamReader? _shellOutput;
    private StreamWriter? _shellInput;
    private StreamReader? _shellError;
    private Thread? _outputThread;
    private Thread? _errorThread;
    private Thread? _queueThread;
    private TaskCompletionSource _whenIdle = new();
    private readonly Dictionary<StreamReader, StringBuilder> _buffers = new();
    private readonly Regex _shellPromptPattern = new(@"^([A-Za-z]:(?:\\[A-Za-z0-9._ -]+)+\\?)>$");
    private readonly CancellationTokenSource _keyboardInterrupt = new();
    private readonly BlockingCollection<Line> _queue = new();

    public bool HasStarted { get; private set; }

    public bool IsExecuting { get; private set; }

    public bool HasExited => _shellProcess?.HasExited ?? throw new Exception();

    public string FullPrompt => $"{CurrentDirectory}>";

    public string? CurrentDirectory { get; private set; }

    public void Start(Action<string, bool> print)
    {
        _print = print ?? throw new ArgumentNullException(nameof(print));

        _shellProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "cmd",
                Arguments = null,
                WindowStyle = ProcessWindowStyle.Normal,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                CreateNoWindow = true,
                WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            }
        };

        _shellProcess.Start();

        _shellOutput = _shellProcess.StandardOutput;
        _shellInput = _shellProcess.StandardInput;
        _shellError = _shellProcess.StandardError;

        _outputThread = new Thread(async () => await WriteQueue(_shellOutput, (text, time, isEnd) =>
        {
            if (text == Environment.NewLine)
            {
                return null;
            }

            return new Line(
                _shellOutput,
                isEnd,
                time,
                isEnd ? FullPrompt : text);
        }));
        _outputThread.Start();

        _errorThread = new Thread(async () => await WriteQueue(_shellError, (text, time, isEnd) =>
        {
            return new Line(
                _shellError,
                isEnd,
                time,
                text);
        }));
        _errorThread.Start();

        _queueThread = new Thread(ReadQueue);
        _queueThread.Start();

        HasStarted = true;
    }

    private void ReadQueue()
    {
        while (!HasExited)
        {
            if (!_queue.TryTake(out Line? result, 50))
            {
                if (!_whenIdle.Task.IsCompleted)
                {
                    _whenIdle.SetResult();
                }
                continue;
            }

            if (result == null)
            {
                throw new Exception();
            }

            Print(result.Text, false);

            if (result.EndOfContent && !_whenIdle.Task.IsCompleted)
            {
                _whenIdle.SetResult();
            }
        }
    }

    public void End()
    {
        _shellProcess?.Kill();
    }

    public async Task Execute(string command)
    {
        if (_shellOutput == null || _shellError == null || _shellInput == null)
        {
            throw new InvalidOperationException();
        }

        IsExecuting = true;

        _whenIdle = new();

        _shellInput.Write(command + Environment.NewLine);

        await _whenIdle.Task;

        IsExecuting = false;
    }

    private async Task WriteQueue(StreamReader reader, Func<string, DateTime, bool, Line?> cstor)
    {
        if (_shellOutput == null || _shellError == null || _shellInput == null)
        {
            throw new InvalidOperationException();
        }

        while (!reader.EndOfStream)
        {
            foreach (var line in ReadLines(reader, cstor))
            {
                _queue.Add(line);
            }

            await Task.Delay(25);
        }
    }

    private int Peek(StreamReader reader)
    {
        var buffer = GetBuffer(reader);
        if (0 < buffer.Length)
        {
            return buffer[0];
        }

        FillBuffer(reader);

        var ch = buffer.Length == 0 ? -1 : buffer[0];
        return ch;
    }

    private IEnumerable<Line> ReadLines(StreamReader reader, Func<string, DateTime, bool, Line?> cstor)
    {
        var lines = new List<string>();

        var buffer = GetBuffer(reader);

        while (-1 < reader.Peek())
        {
            var now = DateTime.Now;

            FillBuffer(reader);

            var bufferContents = buffer.ToString();

            lines.AddRange(bufferContents.Split(Environment.NewLine));
            buffer.Clear();

            if (lines.Count == 0)
            {
                continue;
            }

            var lastIsPrompt = false;
            var lastLine = lines.Last();
            lines.RemoveAt(lines.Count - 1);

            if (lastLine != string.Empty)
            {
                var match = _shellPromptPattern.Match(lastLine);
                lastIsPrompt = match?.Success ?? false;
                if (!lastIsPrompt)
                {
                    buffer.Append(lastLine);
                }
            }

            foreach (var line in lines)
            {
                var temp = cstor(line + Environment.NewLine, now, false);
                if (temp != null)
                {
                    yield return temp;
                }
            }

            lines.Clear();

            if (lastIsPrompt)
            {
                var temp = cstor(lastLine, now, true);
                if (temp != null)
                {
                    yield return temp;
                }
            }
        }
    }

    private StringBuilder GetBuffer(StreamReader reader)
    {
        if (!_buffers.TryGetValue(reader, out var buffer))
        {
            buffer = new StringBuilder();
            _buffers.Add(reader, buffer);
        }
        return buffer;
    }

    private void FillBuffer(StreamReader reader)
    {
        if (-1 < reader.Peek())
        {
            var tempBuffer = new char[1024];

            var read = reader.Read(tempBuffer, 0, tempBuffer.Length);

            GetBuffer(reader).Append(tempBuffer, 0, read);
        }
    }

    public void StopExecution()
    {
        _keyboardInterrupt.Cancel(true);
    }

    private void Print(string text, bool newline = true)
    {
        if (_print == null)
        {
            throw new InvalidOperationException($"{nameof(_print)} is null.");
        }

        _print(text, newline);
    }
}
