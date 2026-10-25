using Bonfire.Application.Seeds;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Seeds;
using BonfireDB.Entities;
using BonfireDB.Libraries;

namespace BonfireDB.SeedEditing;

public static class SeedReadDbMapper
{
    public static Result<PlantReadFromViewModel> ReadPlant(Plant plant)
        => plant.PlantCulture is null || plant.PlantSort is null
            ? Result.Failure<PlantReadFromViewModel>($"Растение {plant.Id}: культура или сорт отсутствуют")
            : LibraryReadDbMapper.Read(plant.PlantSort).Bind(sort =>
                Result.Success(new PlantReadFromViewModel(plant.Id, LibraryReadDbMapper.Read(plant.PlantCulture), sort)));

    public static Result<SeedReadFromViewModel> Read(Seed seed)
        => seed.Plant is null || seed.SeedsInfo is null ? Result.Failure<SeedReadFromViewModel>($"Семена {seed.Id}: растение или пакет отсутствуют")
            : ReadPlant(seed.Plant).Bind(plant => SeedPackage.Create(SeedPackageDbMapper.Read(seed.SeedsInfo))
                .Bind(package => Result.Success(new SeedReadFromViewModel(seed.Id, seed.SeedsInfoId, plant, package.Values))))
                .Match(Result.Success, error => Result.Failure<SeedReadFromViewModel>($"Семена {seed.Id}: {error}"));
}
