using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Bonfire.Infrastructure.Commands.Base;

internal abstract class CommandAsync : ICommand
{
    private bool _executable = true;

    public bool Executable
    {
        get => _executable;
        set
        {
            if (_executable == value) return;
            _executable = value;
            ExecutableChanged?.Invoke(this, EventArgs.Empty);
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public event EventHandler? ExecutableChanged;

    event EventHandler? ICommand.CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    bool ICommand.CanExecute(object parameter) => _executable && CanExecute(parameter);

    async void ICommand.Execute(object parameter)
    {
        if (!((ICommand)this).CanExecute(parameter)) return;
        try
        {
            Executable = false;
            await ExecuteAsync(parameter);
        }
        catch
        {
            Executable = true;
            throw;
        }
    }

    protected virtual bool CanExecute(object p) => true;

    protected abstract Task ExecuteAsync(object p);
}