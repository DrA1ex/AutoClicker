using System;
using System.Windows.Input;

namespace AutoClicker.Common.Utils
{
    public class DelegateCommand : ICommand
    {
        private readonly Action _action;
        private readonly Func<bool> _canExecute = () => true;

        public DelegateCommand(Action action)
        {
            _action = action;
        }

        public DelegateCommand(Action action, Func<bool> canExecute)
        {
            _action = action;
            _canExecute = canExecute;
        }

        public void Execute(object parameter)
        {
            _action();
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute();
        }

        public event EventHandler CanExecuteChanged;

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public class DelegateCommand<T> : ICommand
    {
        private readonly Action<T> _action;
        private readonly Func<bool> _canExecute = () => true;

        public DelegateCommand(Action<T> action)
        {
            _action = action;
        }

        public DelegateCommand(Action<T> action, Func<bool> canExecute)
        {
            _action = action;
            _canExecute = canExecute;
        }

        public void Execute(object parameter)
        {
            _action((T)parameter);
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute();
        }

        public event EventHandler CanExecuteChanged;

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}