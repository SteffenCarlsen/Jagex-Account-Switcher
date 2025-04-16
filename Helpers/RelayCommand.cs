using System;
using System.Windows.Input;

namespace JagexAccountSwitcher.Helpers;

public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>[] _canExecuteConditions;

    public RelayCommand(Action execute, params Func<bool>[] canExecuteConditions)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecuteConditions = canExecuteConditions;
    }

    public event EventHandler CanExecuteChanged;

    public bool CanExecute(object parameter)
    {
        if (_canExecuteConditions == null || _canExecuteConditions.Length == 0)
            return true;
            
        foreach (var condition in _canExecuteConditions)
        {
            if (!condition())
                return false;
        }
        
        return true;
    }

    public void Execute(object parameter)
    {
        _execute();
    }

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}