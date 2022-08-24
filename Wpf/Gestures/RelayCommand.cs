using Data;
using System;
using System.Windows.Input;

namespace CSharpSandbox.Wpf.Gestures
{
    public class RelayCommand : RelayCommand<object>
    {
        public RelayCommand(Action execute, Func<bool>? canExecute = null)
            : base(
                  _ => execute(),
                  canExecute == null ? null : _ => canExecute())
        {
        }

        public RelayCommand(Command command)
            : base()
        {

        }

        public void Execute() => Execute(null);

        public bool CanExecute() => CanExecute(null);
    }

    public class RelayCommand<T> : IRelayCommand<T>
    {
        private readonly Func<T?, bool>? _canExecute;
        private readonly Action<T?> _execute;

        public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public void Execute(object? parameter)
        {
            if (CanExecute(parameter))
            {
                _execute((T?)parameter);
            }
        }

        public bool CanExecute(object? parameter) => _canExecute == null || _canExecute((T?)parameter);

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
    public interface IRelayCommand<T> : ICommand
    {
    }
}