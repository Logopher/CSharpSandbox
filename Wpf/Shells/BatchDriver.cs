using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Timers;

using Timer = System.Timers.Timer;
using ThreadedTimer = System.Threading.Timer;
using System.Collections.Generic;
using CSharpSandbox.Common;
using System.Linq;

namespace CSharpSandbox.Wpf.Shells;

internal class BatchDriver : IShellDriver
{
    private Action<string, bool>? _print;
    private Process? _shellProcess;
    private ShellStreamReader? _shellOutput;
    private StreamWriter? _shellInput;
    private ShellStreamReader? _shellError;
    private Task? _readOutputTask;
    private Task? _readErrorsTask;
    private string? _enteredCommand;
    private readonly Regex _shellPromptPattern = new(@"^([A-Za-z]:(?:\\[A-Za-z0-9._ -]+)+\\?)>$");
    private readonly CancellationTokenSource _keyboardInterrupt = new();
    private readonly TaskFactory _taskFactory = new();
    private readonly TaskCompletionSource _taskCompletionSource = new();

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

        _shellOutput = new(_shellProcess, _shellProcess.StandardOutput, Print);
        _shellInput = _shellProcess.StandardInput;
        _shellError = new(_shellProcess, _shellProcess.StandardError, Print);

        _readOutputTask = _taskFactory.StartNew(ReadOutputStream, _keyboardInterrupt.Token);
        _readErrorsTask = _taskFactory.StartNew(ReadErrorStream, _keyboardInterrupt.Token);

        HasStarted = true;
    }
    public void End() => _shellProcess?.Kill();

    public async Task Execute(string command)
    {
        Debug.Assert(_shellOutput != null);
        Debug.Assert(_shellInput != null);
        Debug.Assert(_shellError != null);

        IsExecuting = true;

        _enteredCommand = command;

        _shellInput.Write(command + Environment.NewLine);

        async Task<bool> output()
        {
            var result = await _shellOutput!.WaitForIdle();
            return result;
        }
        async Task<bool> error()
        {
            var result = await _shellError!.WaitForIdle();
            return result;
        }

        //var idle = await Task.WhenAll(output(), error());
        var idle = await Task.WhenAll(error());

        Debug.Assert(idle != null && idle.All(b => b));

        Print(FullPrompt, false);

        IsExecuting = false;
    }

    public void StopExecution()
    {
        _keyboardInterrupt.Cancel(true);
    }

    private void ReadErrorStream()
    {
        if (_shellError == null)
        {
            throw new InvalidOperationException("Error stream not initialized.");
        }

        _shellError.Read(null, null);
    }

    private void ReadOutputStream()
    {
        if (_shellOutput == null)
        {
            throw new InvalidOperationException("Output stream not initialized.");
        }

        Match? match = null;
        bool matchSuccess = false;

        _shellOutput.Read(e =>
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
                    if (IsExecuting)
                    {
                        if (_shellOutput.IsIdle)
                            _shellOutput.ProclaimIdle();
                        e.ChunkBuffer = string.Empty;
                    }
                    else
                    {
                        e.ChunkBuffer = FullPrompt;
                    }
                }
                else if (e.ChunkBuffer == (_enteredCommand + Environment.NewLine))
                {
                    e.ChunkBuffer = string.Empty;
                }

                matchSuccess = false;

                Task.Run(async () =>
                {
                    await Task.Delay(50);

                    _taskCompletionSource.SetResult();
                });
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
