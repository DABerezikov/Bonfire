using Bonfire.Infrastructure.Commands.Base;
using System;

namespace Bonfire.Infrastructure.Commands;

internal class LambdaCommandAsync(ActionAsync<object> execute, Func<object, bool> canExecute = null)
    : Command
{
    public LambdaCommandAsync(ActionAsync Execute, Func<bool> CanExecute = null)
        : this(async p => await Execute(), CanExecute is null ? (Func<object, bool>)null : p => CanExecute())
    {

    }

    protected override bool CanExecute(object p) => canExecute?.Invoke(p) ?? true;

    protected override void Execute(object p) => execute(p);
}