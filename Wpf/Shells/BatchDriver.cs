using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace CSharpSandbox.Wpf.Shells
{
    internal class BatchDriver : IShellDriver
    {
        private Action<string, bool>? _print;
        private Process? _shellProcess;
        private StreamReader? _shellOutput;
        private StreamWriter? _shellInput;
        private StreamReader? _shellError;
        private Task? _readWriteTask;
        private Task? _readErrorsTask;
        private string? _enteredCommand;
        private readonly Regex _shellPromptPattern = new(@"^([A-Za-z]:(?:\\[A-Za-z0-9._ -]+)+\\?)>$");
        private readonly CancellationTokenSource _keyboardInterrupt = new();
        private readonly TaskFactory _taskFactory = new();

        public bool HasStarted { get; private set; }

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

            _readWriteTask = _taskFactory.StartNew(ReadOutputStream, _keyboardInterrupt.Token);
            _readErrorsTask = _taskFactory.StartNew(ReadErrorStream, _keyboardInterrupt.Token);

            HasStarted = true;
        }
        public void End() => _shellProcess?.Kill();

        public void Execute(string command)
        {
            Debug.Assert(_shellInput != null);

            _enteredCommand = command;

            _shellInput.Write(command + Environment.NewLine);
        }

        public void StopExecution()
        {
            _keyboardInterrupt.Cancel(true);
        }

        private void Read(StreamReader reader, Action<ShellEventArgs>? characterRead = null, Action<ShellEventArgs>? chunkRead = null)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            string buffer = string.Empty;

            while (!(_shellProcess!.HasExited))
            {
                while (!buffer.EndsWith(Environment.NewLine))
                {
                    var next = (char)reader.Read();

                    buffer += next;

                    var ev1 = new ShellEventArgs(ShellEventType.Character, buffer, next)
                    {
                        BreakChunk = buffer.EndsWith(Environment.NewLine)
                    };
                    characterRead?.Invoke(ev1);
                    buffer = ev1.ChunkBuffer;

                    if (ev1.BreakChunk)
                    {
                        break;
                    }
                }

                var ev2 = new ShellEventArgs(ShellEventType.Chunk, buffer);
                chunkRead?.Invoke(ev2);
                buffer = ev2.ChunkBuffer;

                Print(buffer, false);

                buffer = string.Empty;
            }
        }

        private void ReadErrorStream()
        {
            if (_shellError == null)
            {
                throw new InvalidOperationException("Error stream not initialized.");
            }

            Read(_shellError);
        }

        private void ReadOutputStream()
        {
            if (_shellOutput == null)
            {
                throw new InvalidOperationException("Output stream not initialized.");
            }

            Match? match = null;
            bool matchSuccess = false;

            Read(_shellOutput,
                e =>
                {
                    Debug.Assert(e != null);

                    match = _shellPromptPattern.Match(e.ChunkBuffer);
                    matchSuccess = match?.Success ?? false;
                    e.BreakChunk = matchSuccess;
                },
                e =>
                {
                    Debug.Assert(e != null);

                    if (matchSuccess)
                    {
                        CurrentDirectory = match!.Groups[1].Value;
                        _enteredCommand = null;
                        e.ChunkBuffer = FullPrompt;
                    }
                    else if (e.ChunkBuffer == (_enteredCommand + Environment.NewLine))
                    {
                        e.ChunkBuffer = string.Empty;
                    }

                    matchSuccess = false;
                });
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
}
