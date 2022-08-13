using CSharpSandbox.Common;
using CSharpSandbox.Wpf.Gestures;
using CSharpSandbox.Wpf.Infrastructure;
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
using Keyboard = CSharpSandbox.Wpf.Gestures.Keyboard;

namespace CSharpSandbox.Wpf.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        static readonly TimeSpan GestureTimeout = TimeSpan.FromSeconds(10);

        Dictionary<string, ICommand> _commands = new Dictionary<string, ICommand>();

        InputGestureTree _gestureTree = new();

        InputGestureTree.Walker? _gestureWalker;
        DateTime? _gestureTime;
        string _statusText = Mundane.EmptyString;
        string _gestureText = Mundane.EmptyString;

        public IServiceProvider Services { get; }
        public IReadOnlyDictionary<string, ICommand> Commands => _commands;
        public string StatusText
        {
            get => _statusText;
            set
            {
                _statusText = value;
                NotifyPropertyChanged();
            }
        }
        public string GestureText
        {
            get => _gestureText;
            set
            {
                _gestureText = value;
                NotifyPropertyChanged();
            }
        }

        public MainWindow(IServiceProvider services)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));

            InitializeComponent();

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

            //InputBindings.Add(new InputBinding(command, gesture));
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
                var key = e.Key == Key.System ? e.SystemKey : e.Key;

                if (KeyClass.ModifierKeys.Contains(key))
                {
                    return;
                }

                var stim = new InputGestureTree.Stimulus(e.KeyboardDevice.Modifiers, key);

                Debug.Assert((_gestureWalker == null) == (_gestureTime == null));

                if (_gestureWalker == null)
                {
                    if (stim.ModifierKeys == ModifierKeys.None || stim.ModifierKeys == ModifierKeys.Shift)
                    {
                        return;
                    }

                    _gestureWalker = _gestureTree.Walk(stim, true);
                }
                else if (_gestureTime + GestureTimeout <= DateTime.Now)
                {
                    _gestureWalker = _gestureTree.Walk(stim, true);
                }
                else
                {
                    _gestureWalker.Walk(stim);
                }

                GestureText = string.Join(" ", _gestureWalker.Breadcrumbs);
                Debug.WriteLine(GestureText);

                _gestureTime = DateTime.Now;

                if (_gestureWalker.IsLeaf && _gestureWalker.Command.CanExecute(null))
                {
                    _gestureWalker.Command.Execute(null);
                    _gestureWalker = null;
                    _gestureTime = null;
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

        public event PropertyChangedEventHandler? PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string name = Mundane.EmptyString)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
