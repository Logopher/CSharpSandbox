using CSharpSandbox.Common;
using CSharpSandbox.PSHell.Gestures;
using CSharpSandbox.PSHell.Infrastructure;
using CSharpSandbox.PSHell.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Keyboard = CSharpSandbox.PSHell.Gestures.Keyboard;

namespace CSharpSandbox.PSHell.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static readonly TimeSpan GestureTimeout = TimeSpan.FromSeconds(2);

        readonly MainViewModel _viewModel;

        Dictionary<string, ICommand> _commands = new Dictionary<string, ICommand>();

        InputGestureTree _gestureTree = new();

        InputGestureTree.Walker? _gestureWalker;
        DateTime? _gestureTime;

        DispatcherTimer _gestureTextTimer = new();

        public IServiceProvider Services { get; }
        public IReadOnlyDictionary<string, ICommand> Commands => _commands;

        public MainWindow(IServiceProvider services)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));

            InitializeComponent();

            _viewModel = (MainViewModel)DataContext;

            _gestureTextTimer.Interval = new TimeSpan(0, 0, 2);
            _gestureTextTimer.Tick += new EventHandler((o, e) =>
            {
                _viewModel.GestureText = Mundane.EmptyString;
                _gestureTextTimer.Stop();
            });

            Task.Factory.StartNew(Terminal.Start);
        }

        private void Self_Closing(object sender, CancelEventArgs e)
        {

        }

        private void Self_Closed(object sender, EventArgs e)
        {
            Terminal.Exit();
        }

        public void SetKeyBinding(string commandName, params InputGestureTree.Stimulus[] stimuli)
        {
            if (!Commands.TryGetValue(commandName, out var command))
            {
                throw new ArgumentException($"Invalid command name: {commandName}", nameof(commandName));
            }

            _gestureTree.SetCommand(command, stimuli);
        }

        public void DefineCommand(string commandName, Action execute, Func<bool>? canExecute = null)
        {
            var command = new RelayCommand(execute, canExecute);

            if (!_commands.TryAdd(commandName, command))
            {
                throw new ArgumentException($"Command already exists: {commandName}", nameof(commandName));
            }
        }

        private void Self_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                Debug.Assert((_gestureWalker == null) == (_gestureTime == null));

                var key = e.Key;

                var systemKey = e.Key == Key.System;
                if (systemKey)
                {
                    key = e.SystemKey;
                }

                var deadKey = e.Key == Key.DeadCharProcessed;
                if (deadKey)
                {
                    key = e.DeadCharProcessedKey;
                }

                var imeKey = e.Key == Key.ImeProcessed;
                if (imeKey)
                {
                    key = e.ImeProcessedKey;
                }

                {
                    var specialKeys = new[] { Key.System, Key.DeadCharProcessed, Key.ImeProcessed };
                    Debug.Assert(!specialKeys.Contains(key));
                }

                if (KeyClass.ModifierKeys.Contains(key))
                {
                    return;
                }

                var stim = new InputGestureTree.Stimulus(e.KeyboardDevice.Modifiers, key);

                if (_gestureWalker == null || _gestureTime + GestureTimeout <= DateTime.Now)
                {
                    if (stim.ModifierKeys == ModifierKeys.None || stim.ModifierKeys == ModifierKeys.Shift)
                    {
                        return;
                    }

                    _gestureWalker = _gestureTree.Walk(stim, true);
                }
                else
                {
                    _gestureWalker.Walk(stim);
                }

                _viewModel.GestureText = string.Join(" ", _gestureWalker.Breadcrumbs);
                _gestureTextTimer.Start();

                _gestureTime = DateTime.Now;

                if (_gestureWalker.IsLeaf && _gestureWalker.Command.CanExecute(null))
                {
                    _gestureWalker.Command.Execute(null);
                    _gestureWalker = null;
                    _gestureTime = null;
                }
                else if (3 <= _gestureWalker.Breadcrumbs.Count)
                {
                    _gestureWalker = null;
                    _gestureTime = null;
                    _viewModel.GestureText = Mundane.EmptyString;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                _gestureWalker = null;
                _gestureTime = null;
            }
        }

        private void Self_PreviewKeyUp(object sender, KeyEventArgs e)
        {

        }
    }
}
