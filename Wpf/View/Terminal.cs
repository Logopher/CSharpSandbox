using CSharpSandbox.Common;
using ICSharpCode.AvalonEdit;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Text.RegularExpressions;

namespace CSharpSandbox.Wpf.View
{
    public class Terminal : TextEditor
    {
        private bool _isExecuting = false;
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

        private int LastLineStart
        {
            get
            {
                var lineEnd = Text.LastIndexOf(Environment.NewLine);

                if (lineEnd == -1)
                {
                    return 0;
                }
                else
                {
                    return lineEnd + Environment.NewLine.Length;
                }
            }
        }
        private string LastLine => Text[LastLineStart..];
        private int CommandStart => LastLineStart + FullPrompt.Length;
        private string Command => Text[CommandStart..];

        private string FullPrompt => "> ";
        private bool IsInputRestricted => !IsStarted || CaretOffset < Math.Min(CommandStart, Text.Length);

        public bool IsStarted { get; private set; }
        public string? CurrentDirectory { get; private set; }

        public Terminal()
        {
            PreviewKeyDown += Self_PreviewKeyDown;
            PreviewTextInput += Self_PreviewTextInput;
            PreviewKeyUp += Self_PreviewKeyUp;
        }

        public void Start()
        {
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

            _readWriteTask = _taskFactory.StartNew(ReadWriteShell, _keyboardInterrupt.Token);
            _readErrorsTask = _taskFactory.StartNew(ReadErrors, _keyboardInterrupt.Token);

            IsStarted = true;
        }

        private void Read(StreamReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            string buffer = string.Empty;

            while (!(_shellProcess!.HasExited))
            {
                do
                {
                    var next = (char)reader.Read();

                    buffer += next;
                }
                while (!buffer.EndsWith(Environment.NewLine));

                Dispatcher.Invoke(() => Print(buffer.ToString(), false));
                buffer = string.Empty;
            }
        }

        private void ReadErrors()
        {
            if (_shellError == null)
            {
                throw new InvalidOperationException("Output stream not initialized.");
            }

            Read(_shellError);
        }

        private void ReadWriteShell()
        {
            if (_shellOutput == null)
            {
                throw new InvalidOperationException("Output stream not initialized.");
            }

            string outputBuffer = string.Empty;

            while (!(_shellProcess!.HasExited))
            {
                while (!outputBuffer.EndsWith(Environment.NewLine))
                {
                    var next = (char)_shellOutput.Read();

                    outputBuffer += next;

                    if (_shellPromptPattern.Match(outputBuffer)?.Success ?? false)
                    {
                        break;
                    }
                }

                var match = _shellPromptPattern.Match(outputBuffer);
                if (match?.Success ?? false)
                {
                    CurrentDirectory = match.Groups[1].Value;
                    outputBuffer = string.Empty;
                    Dispatcher.Invoke(() =>
                    {
                        Print(FullPrompt, false);
                        CaretOffset = Text.Length;
                    });
                    _enteredCommand = null;
                    _isExecuting = false;
                }
                else if (outputBuffer != (_enteredCommand + Environment.NewLine))
                {
                    Dispatcher.Invoke(() => Print(outputBuffer, false));
                }
                outputBuffer = string.Empty;
            }
        }

        public void End() => _shellProcess?.Kill();

        private void Execute(string command)
        {
            Debug.Assert(!_isExecuting);
            Debug.Assert(_shellInput != null);

            _isExecuting = true;

            _shellInput.Write(command + Environment.NewLine);
        }

        private void Print(string? text = null, bool newline = true)
        {
            text ??= string.Empty;

            if (newline)
            {
                text += Environment.NewLine;
            }
            Text += text;
        }

        private void StopExecution()
        {
            _keyboardInterrupt.Cancel(true);
        }

        private void Self_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            Debug.Assert(LastLine.StartsWith(FullPrompt));

            // Never restrict unmodified arrow keys.
            if (e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.None))
            {
                switch (e.Key)
                {
                    case Key.Up:
                    case Key.Down:
                    case Key.Left:
                    case Key.Right:
                        return;
                }
            }

            if (IsInputRestricted)
            {
                e.Handled = true;
                return;
            }

            if (e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control))
            {
                switch (e.Key)
                {
                    case Key.C:
                        StopExecution();
                        e.Handled = true;
                        return;
                }
            }

            if (e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.None))
            {
                switch (e.Key)
                {
                    case Key.Enter:
                        _enteredCommand = Command;
                        return;
                }
            }
        }

        private void Self_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Debug.Assert(LastLine.StartsWith(FullPrompt));

        }

        private void Self_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    Debug.Assert(_enteredCommand != null);

                    Execute(_enteredCommand);
                    break;
            }
        }
    }
}
