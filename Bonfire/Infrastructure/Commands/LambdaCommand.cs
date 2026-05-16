using Bonfire.Infrastructure.Commands.Base;
using System;

namespace Bonfire.Infrastructure.Commands;

internal class LambdaCommand(Action<object> execute, Func<object, bool> canExecute = null)
    : Command
{
    public LambdaCommand(Action Execute, Func<bool> CanExecute = null)
        : this(p => Execute(), CanExecute is null ? (Func<object, bool>)null : p => CanExecute())
    {

    }

    protected override bool CanExecute(object p) => canExecute?.Invoke(p) ?? true;

    protected override void Execute(object p) => execute(p);
}