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
using CSharpSandbox.Wpf.Shells;

namespace CSharpSandbox.Wpf.View
{
    public class Terminal : TextEditor
    {
        private readonly CancellationTokenSource _keyboardInterrupt = new();
        private readonly IShellDriver _shellDriver;
        private string? _enteredCommand;
        private int _commandStart = 0;

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

        private bool IsInputRestricted => !IsStarted || CaretOffset < Math.Min(_commandStart, Text.Length);

        public bool IsStarted => _shellDriver.HasStarted;

        public Terminal()
        {
            _shellDriver = new BatchDriver();

            PreviewKeyDown += Self_PreviewKeyDown;
            PreviewTextInput += Self_PreviewTextInput;
            PreviewKeyUp += Self_PreviewKeyUp;
        }

        public void Start()
        {
            _shellDriver.Start((text, newline) => Dispatcher.Invoke(() => Print(text, newline)));
        }

        private void Print(string? text = null, bool newline = true)
        {
            text ??= string.Empty;

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

            switch (e.Key)
            {
                case Key.Enter:
                    Debug.Assert(_enteredCommand != null);

                    _shellDriver.Execute(_enteredCommand);
                    break;
            }
        }

        public void Exit()
        {
            _shellDriver.End();
        }
    }
}
