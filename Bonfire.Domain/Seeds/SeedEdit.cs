using Bonfire.Domain.Functional;
using Bonfire.Domain.Libraries;

namespace Bonfire.Domain.Seeds;

public sealed class SeedEdit
{
    public SeedId SeedId { get; }
    public SortId SortId { get; }
    public ProducerId ProducerId { get; }
    public SeedPackage Package { get; }
    public SortCharacteristics Characteristics { get; }

    private SeedEdit(SeedId seedId, SortId sortId, ProducerId producerId, SeedPackage package, SortCharacteristics characteristics)
        => (SeedId, SortId, ProducerId, Package, Characteristics) = (seedId, sortId, producerId, package, characteristics);

    public static Result<SeedEdit> Create(int seedId, int sortId, int producerId, string name,
        SeedPackageValues package, SortCharacteristicsValues characteristics)
        => SeedId.Create(seedId).Bind(seed => SortId.Create(sortId).Bind(sort =>
            ProducerId.Create(producerId).Bind(producer => SeedPackage.Create(package).Bind(pack =>
                SortCharacteristics.Create(name, characteristics).Bind(details =>
                    Result.Success(new SeedEdit(seed, sort, producer, pack, details)))))));
}
