using System.Windows.Input;

namespace GrimmDiff.Gui.Commands;

public sealed class DelegateCommand<T>(Action<T> execute) : ICommand
{
    public event EventHandler? CanExecuteChanged
    {
        add { }
        remove { }
    }

    public bool CanExecute(object? parameter)
    {
        return parameter is T || (parameter is null && default(T) is null);
    }

    public void Execute(object? parameter)
    {
        if (parameter is T value) execute(value);
        else if (parameter is null && default(T) is null) execute((T)parameter!);
    }
}
