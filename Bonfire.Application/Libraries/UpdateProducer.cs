using Bonfire.Domain.Functional;
using Bonfire.Domain.Libraries;

namespace Bonfire.Application.Libraries;

public sealed record UpdateProducerFromViewModel(int Id, string Name);
public sealed record ProducerDetailsFromViewModel(int Id, string Name);

public interface IProducerWriter
{
    Task<Result<ProducerDetailsFromViewModel>> SaveAsync(ProducerDefinition producer, CancellationToken cancellationToken);
}

public interface IUpdateProducer
{
    Task<Result<ProducerDetailsFromViewModel>> ExecuteAsync(UpdateProducerFromViewModel request, CancellationToken cancellationToken = default);
}

public sealed class UpdateProducer(IProducerWriter writer) : IUpdateProducer
{
    public async Task<Result<ProducerDetailsFromViewModel>> ExecuteAsync(UpdateProducerFromViewModel request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        return await ProducerDefinition.Create(request.Id, request.Name).Match(
            producer => writer.SaveAsync(producer, cancellationToken),
            error => Task.FromResult(Result.Failure<ProducerDetailsFromViewModel>(error))).ConfigureAwait(false);
    }
}
