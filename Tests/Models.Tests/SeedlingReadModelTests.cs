using Bonfire.Application.Seedlings;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Seedlings;
using Bonfire.Models;
using Bonfire.Models.Mappers;
using BonfireDB.Entities;
using BonfireDB.Sowing;

namespace Models.Tests;

public class SeedlingReadModelTests
{
    [Fact]
    public void Apply_Change_PreservesOriginalAndUnrelatedHistory()
    {
        var entity = new Seedling { Id = 1, Quantity = 3, Plant = new() { PlantCulture = new() { Name = "Культура" },
            PlantSort = new() { Name = "Сорт", Producer = new() { Name = "Фирма" } } },
            SeedlingInfos = [new() { Id = 1, Note = "История", QuarantineCause = "Причина", QuenchingDate = new(2026, 4, 1),
                Treatments = [new() { Product = "Препарат", TreatmentMethod = "Метод" }] }] };
        var original = new SeedlingReadModel(SeedlingReadDbMapper.Read(entity).ValueOrThrow());
        var result = SeedlingReadMapper.Apply(original, new SeedlingChangeResultFromViewModel(1,
            [new(1, 0, Option.None<DateTime>(), Option.Some(true)), new(2, 1, Option.Some(new DateTime(2026, 4, 2)), Option.None<bool>())]) { PlantedOut = 2 }, Option.None<SeedlingDetails>());
        Assert.Single(original.SeedlingInfos); Assert.Null(original.SeedlingInfos[0].IsDead); Assert.Equal(0, original.PlantedOut);
        Assert.Equal(2, result.SeedlingInfos.Count); Assert.True(result.SeedlingInfos[0].IsDead); Assert.Equal(2, result.PlantedOut);
        Assert.Equal("История", result.SeedlingInfos[0].Note); Assert.Equal("Причина", result.SeedlingInfos[0].QuarantineCause);
        Assert.Equal(new DateTime(2026, 4, 1), result.SeedlingInfos[0].QuenchingDate);
        Assert.Equal("Препарат", result.Snapshot.SeedlingInfos[0].Treatments.Match(v => v[0].Product.ToNullable(), () => null));
    }
}
