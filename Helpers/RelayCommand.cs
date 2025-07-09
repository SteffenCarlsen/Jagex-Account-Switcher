#region

using System;
using System.Windows.Input;

#endregion

namespace JagexAccountSwitcher.Helpers;

public class RelayCommand<T> : ICommand
{
    private readonly Func<T, bool>[] _canExecuteConditions;
    private readonly Action<T> _execute;

    public RelayCommand(Action<T> execute, params Func<T, bool>[] canExecuteConditions)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecuteConditions = canExecuteConditions;
    }

    public event EventHandler CanExecuteChanged;

    public bool CanExecute(object parameter)
    {
        if (_canExecuteConditions == null || _canExecuteConditions.Length == 0)
            return true;

        if (parameter is T typedParameter)
        {
            foreach (var condition in _canExecuteConditions)
            {
                if (!condition(typedParameter))
                    return false;
            }

            return true;
        }

        return parameter == null && typeof(T).IsValueType == false;
    }

    public void Execute(object parameter)
    {
        if (parameter is T typedParameter ||
            (parameter == null && typeof(T).IsValueType == false))
        {
            _execute((T)parameter);
        }
    }

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}

public class RelayCommand : ICommand
{
    private readonly Func<bool>[] _canExecuteConditions;
    private readonly Action _execute;

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