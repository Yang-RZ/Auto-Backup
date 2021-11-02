using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace MYLibrary.Bindings.Commands
{

    public class RelayCommand : ICommand
    {
        Dispatcher _dispatcher = null;
        readonly Predicate<Object> _canExecute = null;
        readonly Action<Object> _executeAction = null;

        public RelayCommand(Action<object> executeAction, Predicate<Object> canExecute = null, Dispatcher dispatcher = null)
        {
            _canExecute = canExecute;
            _executeAction = executeAction;
            _dispatcher = dispatcher;
        }

        public bool CanExecute(object parameter)
        {
            if (_canExecute != null)
                return _canExecute(parameter);
            return true;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object parameter)
        {
            if (_executeAction != null)
                _executeAction(parameter);
        }
    }
}
