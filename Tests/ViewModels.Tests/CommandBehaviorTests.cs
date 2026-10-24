using System.Threading.Tasks;
using System.Windows.Input;
using Bonfire.Infrastructure.Commands;
using Bonfire.Infrastructure.Commands.Base;

namespace ViewModels.Tests;

[CollectionDefinition("Command errors", DisableParallelization = true)]
public class CommandErrorsCollection;

[Collection("Command errors")]
public class CommandBehaviorTests : IDisposable
{
    private readonly Action<Exception>? _previousHandler = CommandExceptionHandler.OnException;
    public void Dispose() => CommandExceptionHandler.OnException = _previousHandler;

    [Fact]
    public void SynchronousCommand_TransfersParameterAndRespectsPredicateAndExecutableFlag()
    {
        object? received = null;
        var command = new LambdaCommand(p => received = p, p => p is string);
        var changes = 0;
        command.ExecutableChanged += (_, _) => changes++;
        var api = (ICommand)command;
        api.Execute(42);
        Assert.Null(received);
        api.Execute("value");
        Assert.Equal("value", received);
        command.Executable = false;
        command.Executable = false;
        Assert.False(api.CanExecute("next"));
        api.Execute("next");
        Assert.Equal("value", received);
        Assert.Equal(1, changes);
        command.Executable = true;
        Assert.True(api.CanExecute("next"));
    }

    [Fact]
    public void SynchronousCommand_ReportsExceptionAndCanRunAgain()
    {
        var error = new InvalidOperationException("Ошибка команды");
        Exception? reported = null;
        CommandExceptionHandler.OnException = exception => reported = exception;
        var calls = 0;
        ICommand command = new LambdaCommand(() => { if (++calls == 1) throw error; });
        command.Execute(null);
        Assert.Same(error, reported);
        command.Execute(null);
        Assert.Equal(2, calls);
        CommandExceptionHandler.OnException = null;
        CommandExceptionHandler.Report(error);
    }

    [Fact]
    public async Task AsyncCommand_PendingOperation_BlocksReentryAndRestoresAvailability()
    {
        var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        var command = new LambdaCommandAsync(async () => { calls++; await pending.Task; });
        command.ExecutableChanged += (_, _) => { if (command.Executable) completed.TrySetResult(); };
        var api = (ICommand)command;
        api.Execute(null);
        Assert.False(api.CanExecute(null));
        api.Execute(null);
        Assert.Equal(1, calls);
        pending.SetResult();
        await completed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(api.CanExecute(null));
    }

    [Fact]
    public void AsyncCommand_Failure_ReportsErrorAndReleasesExecutionLock()
    {
        var error = new InvalidOperationException("Сбой");
        Exception? reported = null;
        CommandExceptionHandler.OnException = exception => reported = exception;
        var calls = 0;
        ICommand command = new LambdaCommandAsync(() => ++calls == 1 ? Task.FromException(error) : Task.CompletedTask);
        command.Execute(null);
        Assert.Same(error, reported);
        Assert.True(command.CanExecute(null));
        command.Execute(null);
        Assert.Equal(2, calls);
    }

    [Fact]
    public void AsyncCommand_FalsePredicate_DoesNotExecute()
    {
        var executed = false;
        ICommand command = new LambdaCommandAsync(() => { executed = true; return Task.CompletedTask; }, () => false);
        command.Execute(null);
        Assert.False(executed);
    }
}
