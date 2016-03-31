using System;

namespace ChatClient.BaseClasses
{
    internal class SimpleCommand : CommandBase
    {
        private object parentObject;
        private Func<object, bool> canExecute;
        private Action<object> execute;

        public SimpleCommand(Func<object,bool> _canExecute,Action<object> _execute,object _parent)
        {
            parentObject = _parent;
            canExecute = _canExecute;
            execute = _execute;
        }

        public override bool CanExecute(object parameter)
        {
            return canExecute(parameter);
        }

        public override void Execute(object parameter)
        {
            execute(parameter);
        }
    }
}