using GrimmDiff.Logic.Abstracts;
using System.Windows.Input;

namespace GrimmDiff.Gui;

public sealed class AsyncCommandOfT<T>(Func<T, Task> execute, Func<T, bool>? canExecute = null) : ICommand
{
    private bool _executing;
    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
        return !_executing && parameter is T value && (canExecute?.Invoke(value) ?? true);
    }

    public async void Execute(object? parameter)
    {
        if (parameter is not T value || !CanExecute(value)) return;
        _executing = true;
        RaiseCanExecuteChanged();
        try
        {
            await execute(value);
        }
        finally
        {
            _executing = false;
            RaiseCanExecuteChanged();
        }
    }

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
