using ICSharpCode.AvalonEdit;
using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Input;
using CSharpSandbox.Shells;
using System.Threading.Tasks;

namespace CSharpSandbox.Wpf.View
{
    public class Terminal : TextEditor, ITerminal
    {
        private readonly CancellationTokenSource _keyboardInterrupt = new();
        private readonly IShellDriver _shellDriver;
        private string? _enteredCommand;
        private int _commandStart = 0;
        private TaskCompletionSource<string?> _readlineTCS = new();

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
        private string Command => Text[_commandStart..];

        private bool IsInputRestricted => !IsStarted
            || CaretOffset < Math.Min(_commandStart, Text.Length)
            || !_shellDriver.IsReadyForInput;

        public bool IsStarted => _shellDriver.HasStarted;

        public Terminal()
        {
            _shellDriver = new PSDriver(this, "$(CurrentDirectory)> ");

            PreviewKeyDown += Self_PreviewKeyDown;
            PreviewTextInput += Self_PreviewTextInput;
            PreviewKeyUp += Self_PreviewKeyUp;
        }

        public void Start()
        {
            _shellDriver.Start((text, newline) => Dispatcher.Invoke(() => Print(text, newline)));
        }

        private void Print(object? value = null, bool newline = true)
        {
            var text = value?.ToString() ?? string.Empty;

            var wasCaretAtEnd = CaretOffset == Text.Length;

            if (newline)
            {
                text += Environment.NewLine;
            }
            Text += text;

            _commandStart = Text.Length;

            if (wasCaretAtEnd)
            {
                CaretOffset = _commandStart;
                ScrollToEnd();
            }
        }

        private void Self_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!IsStarted)
            {
                e.Handled = true;
                return;
            }

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

            // Backspace is a special case because it can delete restricted text
            // while the caret is in an unrestricted position.
            if (!e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Alt))
            {
                switch (e.Key)
                {
                    case Key.Back:
                        if (CaretOffset <= _commandStart)
                        {
                            e.Handled = true;
                        }
                        return;
                }
            }

            if (e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control))
            {
                switch (e.Key)
                {
                    case Key.C:
                        _shellDriver.StopExecution();
                        e.Handled = true;
                        return;
                }
            }

            if (e.KeyboardDevice.Modifiers == ModifierKeys.None)
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
            if (!IsStarted)
            {
                e.Handled = true;
                return;
            }

        }

        private void Self_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (!IsStarted)
            {
                e.Handled = true;
                return;
            }

            if (IsInputRestricted)
            {
                e.Handled = true;
                return;
            }

            switch (e.Key)
            {
                case Key.Enter:
                    Debug.Assert(_enteredCommand != null);
                    _commandStart = Text.Length;
                    _shellDriver.Execute(_enteredCommand);
                    break;
            }
        }

        public void Exit(int exitCode)
        {
            _shellDriver.End();
        }

        public string? ReadLine()
        {
            if (Dispatcher.Thread == Thread.CurrentThread)
            {
                throw new InvalidOperationException();
            }

            var task = _readlineTCS.Task;
            task.Wait();
            return task.Result;
        }

        public void Write(object? value)
        {
            Dispatcher.Invoke(() => Print(value, false));
        }

        public void WriteLine(object? line = null)
        {
            Dispatcher.Invoke(() => Print(line));
        }
    }
}
