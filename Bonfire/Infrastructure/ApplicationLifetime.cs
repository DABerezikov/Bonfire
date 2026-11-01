using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Bonfire.Infrastructure;

internal sealed class ApplicationLifetime : IAsyncDisposable
{
    private readonly Lazy<Task> _dispose;
    private readonly IHost _host;
    private readonly Func<CancellationToken, Task> _initialize;
    private readonly Lazy<Task> _start;
    private readonly CancellationTokenSource _startupCancellation = new();
    private readonly Lazy<Task> _stop;

    public ApplicationLifetime(IHost host, Func<CancellationToken, Task> initialize)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(initialize);
        _host = host;
        _initialize = initialize;
        _start = new Lazy<Task>(StartCoreAsync);
        _stop = new Lazy<Task>(StopCoreAsync);
        _dispose = new Lazy<Task>(DisposeCoreAsync);
    }

    public ValueTask DisposeAsync() => new(_dispose.Value);

    public Task StartAsync() => _stop.IsValueCreated
        ? Task.FromException(new InvalidOperationException("Приложение уже завершает работу"))
        : _start.Value;

    public Task StopAsync() => _stop.Value;

    private async Task StartCoreAsync()
    {
        await _initialize(_startupCancellation.Token).ConfigureAwait(false);
        await _host.StartAsync(_startupCancellation.Token).ConfigureAwait(false);
    }

    private async Task StopCoreAsync()
    {
        await _startupCancellation.CancelAsync().ConfigureAwait(false);
        if (!_start.IsValueCreated) return;
        try
        {
            await _start.Value.ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Ошибку запуска получает вызывающий StartAsync; очистка нужна и после частичного запуска.
        }

        await _host.StopAsync().ConfigureAwait(false);
    }

    private async Task DisposeCoreAsync()
    {
        try
        {
            await StopAsync().ConfigureAwait(false);
        }
        finally
        {
            try
            {
                if (_host is IAsyncDisposable asynchronous)
                    await asynchronous.DisposeAsync().ConfigureAwait(false);
                else
                    _host.Dispose();
            }
            finally
            {
                _startupCancellation.Dispose();
            }
        }
    }
}