using Bonfire.Infrastructure.Commands.Base;
using System;

namespace Bonfire.Infrastructure.Commands;

internal class LambdaCommand(Action<object> execute, Func<object, bool> canExecute = null)
    : Command
{
    public LambdaCommand(Action execute, Func<bool> canExecute = null)
        : this(p => execute(), canExecute is null ? (Func<object, bool>)null : p => canExecute())
    {

    }

    protected override bool CanExecute(object p) => canExecute?.Invoke(p) ?? true;

    protected override void Execute(object p) => execute(p);
}