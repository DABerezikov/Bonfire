using Bonfire.Domain.Functional;
using BonfireDB.Entities.GardenPlanning;

namespace Services.Tests;

public class GardenReadQueryTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public async Task Read_LoadsRequiredGraphWithFixedQueryCountWithoutSeedlingHistory(int count)
    {
        var commands = new List<string>(); using var fixture = new SqliteUowFixture(commands.Add);
        var plan = await fixture.PreparePlanAsync("План", 2026);
        await fixture.ExecuteAsync(async db =>
        {
            for (var index = 0; index < count; index++) db.Add(new Garden
            {
                GardenPlanId = plan.Id, Name = "Участок", CanvasWidth = 1000, CanvasHeight = 1000,
                Elements = [new Bed { Name = "Грядка", PlantingSpots = [new() { Note = "Ячейка" }] }, new FlowerBed { Shape = "Круг" }],
                Greenhouses = [new() { Name = "Теплица", CanvasWidth = 600, CanvasHeight = 400,
                    Elements = [new ColdFrame { CoverMaterial = "Плёнка", PlantingSpots = [new() { Note = "Внутри" }] }] }]
            });
            await db.SaveChangesAsync();
        });
        commands.Clear();
        var gardens = (await fixture.CreateGardenService().ReadGardensAsync(plan.Id)).ValueOrThrow();
        Assert.Equal(count, gardens.Length); Assert.Equal(6, commands.Count);
        Assert.All(commands, sql => Assert.DoesNotContain("SeedlingInfos", sql, StringComparison.Ordinal));
        Assert.All(commands, sql => Assert.DoesNotContain("JOIN \"GardenPlans\"", sql, StringComparison.Ordinal));
        Assert.All(gardens, garden =>
        {
            Assert.Equal(2, garden.Elements.Length);
            Assert.Equal("Круг", garden.Elements.Single(e => e.ElementType == "FlowerBed").Shape.ToNullable());
            Assert.Equal("Ячейка", Assert.Single(garden.Elements.Single(e => e.ElementType == "Bed").Spots).Note.ToNullable());
            var inner = Assert.Single(Assert.Single(garden.Greenhouses).Elements);
            Assert.Equal("Плёнка", inner.Properties.Details.CoverMaterial.ToNullable());
            Assert.Equal("Внутри", Assert.Single(inner.Spots).Note.ToNullable());
        });
    }
}
