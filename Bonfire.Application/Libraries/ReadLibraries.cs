using Bonfire.Domain.Functional;
using Bonfire.Domain.Libraries;
using Bonfire.Domain.Seeds;

namespace Bonfire.Application.Libraries;

public sealed record SortReadFromViewModel(
    int Id,
    string Name,
    ProducerDetailsFromViewModel Producer,
    Option<int> CultureId,
    SortCharacteristicsValues Characteristics);

public interface ILibraryReader
{
    Task<Result<SortReadFromViewModel>> ReadSortAsync(SortId id, CancellationToken cancellationToken);
    Task<Result<CultureDetailsFromViewModel>> ReadCultureAsync(CultureId id, CancellationToken cancellationToken);
    Task<Result<ProducerDetailsFromViewModel>> ReadProducerAsync(ProducerId id, CancellationToken cancellationToken);
}