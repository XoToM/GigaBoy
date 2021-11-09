using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace GigaBoy_WPF.Components
{
    class CommandAction : ICommand
    {
        public event EventHandler? CanExecuteChanged;
        public Action<object?> Action { get; private set; }
        public CommandAction(Action action)
        {
            Action = (object? _) => { action(); };
        }
        public CommandAction(Action<object?> action)
        {
            Action = action;
        }

        public bool CanExecute(object? parameter)
        {
            return true;
        }

        public void Execute(object? parameter)
        {
            Action.Invoke(parameter);   
        }
    }
}
