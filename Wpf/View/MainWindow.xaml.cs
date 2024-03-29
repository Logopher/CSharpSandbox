﻿using CSharpSandbox.Common;
using CSharpSandbox.Wpf.Gestures;
using CSharpSandbox.Wpf.ViewModel;
using Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace CSharpSandbox.Wpf.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public IReadOnlyDictionary<string, ICommand> Commands => _commands;
        public IReadOnlyList<MenuItem> MenuItems => _menuItems;

        static readonly TimeSpan GestureTimeout = TimeSpan.FromSeconds(2);

        readonly Dictionary<string, ICommand> _commands = new();
        readonly List<MenuItem> _menuItems;

        // TODO: inject
        readonly DispatcherTimer _gestureTextTimer = new();

        // injected
        readonly MainViewModel _viewModel;
        readonly AboutWindow _aboutWindow;
        readonly InputGestureTree _gestureTree;
        readonly Repository _repository;

        // working objects overwritten during operation
        InputGestureTree.Walker? _gestureWalker;
        DateTime? _gestureTime;

        public MainWindow(
            MainViewModel viewModel,
            AboutWindow aboutWindow,
            Repository repository)
        {
            _repository = repository;

            _commands.Add("About", new RelayCommand(AboutCommand_Invoked));

            _menuItems = _repository.MenuItems
                .Select(i => new MenuItem(i, GetCommand))
                .ToList();

            ResetMenus();
            _repository.Save();

            _aboutWindow = aboutWindow;

            _gestureTree = new(GetCommand);

            DataContext = viewModel;

            _viewModel = viewModel;

            InitializeComponent();

            _gestureTextTimer.Interval = GestureTimeout;
            _gestureTextTimer.Tick += new EventHandler((o, e) =>
            {
                _viewModel.GestureText = Mundane.EmptyString;
                _gestureTextTimer.Stop();
            });

            Task.Factory.StartNew(Terminal.Start);
        }

        public void SetKeyBinding(string commandName, params InputGestureTree.Stimulus[] stimuli)
        {
            if (!Commands.ContainsKey(commandName))
            {
                throw new ArgumentException($"Invalid command name: {commandName}", nameof(commandName));
            }

            _gestureTree.SetCommand(commandName, stimuli);
        }

        public void ResetMenus()
        {
            var model = new Data.Model.MenuItem[]
            {
                new("File", 'F'),
                new("Edit", 'E'),
                new("View", 'V'),
                new("Tools", 'T'),
                new("Help", 'H', new Data.Model.MenuItem[]
                {
                    new("About", 'A', "About")
                }),
            };

            _repository.Delete(_menuItems
                .Select(i => i.Model)
                .ToArray());

            _menuItems.Clear();

            _menuItems.AddRange(model
                .Select(i => new MenuItem(i, GetCommand)));

            _repository.Add(_menuItems
                .Select(i => i.Model)
                .ToArray());

            _repository.Save();
        }

        public void DefineCommand(string commandName, Action execute, Func<bool>? canExecute = null)
        {
            var command = new RelayCommand(execute, canExecute);

            if (!_commands.TryAdd(commandName, command))
            {
                throw new ArgumentException($"Command already exists: {commandName}", nameof(commandName));
            }
        }

        public ICommand GetCommand(string commandName)
        {
            if (!_commands.TryGetValue(commandName, out var command))
            {
                throw new KeyNotFoundException($"Command not found: {commandName}");
            }

            return command;
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
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

            base.OnPreviewKeyDown(e);
        }

        protected override void OnPreviewKeyUp(KeyEventArgs e)
        {
            base.OnPreviewKeyUp(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            Terminal.Exit(0);

            base.OnClosed(e);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
        }

        private void AboutCommand_Invoked()
        {
            _aboutWindow.Show();
        }
    }
}
