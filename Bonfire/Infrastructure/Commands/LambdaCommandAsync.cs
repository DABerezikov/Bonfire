using System;
using System.Threading.Tasks;
using Bonfire.Infrastructure.Commands.Base;

namespace Bonfire.Infrastructure.Commands;

internal class LambdaCommandAsync(ActionAsync<object> execute, Func<object, bool>? canExecute = null)
    : CommandAsync
{
    public LambdaCommandAsync(ActionAsync execute, Func<bool>? canExecute = null)
        : this(async p => await execute(), canExecute is null ? null : p => canExecute())
    {

    }

    protected override bool CanExecute(object p) => canExecute?.Invoke(p) ?? true;

    protected override Task ExecuteAsync(object p) => execute(p);
}
