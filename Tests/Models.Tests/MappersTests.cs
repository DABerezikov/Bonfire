using BonfireDB.SeedEditing;
using BonfireDB.Sowing;
using Bonfire.Domain.Functional;
using Bonfire.Models;
using BonfireDB.Planting;
using Bonfire.Models.Mappers;
using BonfireDB.Entities;
using BonfireDB.Entities.GardenPlanning;
using Bonfire.Domain.Garden.SpotStates;

namespace Models.Tests;

public class MappersTests
{
    private static Seed MakeSeed() => new()
    {
        Id = 7,
        SeedsInfoId = 3,
        Plant = new Plant
        {
            Id = 11,
            PlantCulture = new PlantCulture { Id = 1, Name = "Томат", Class = "Овощи" },
            PlantSort = new PlantSort
            {
                Id = 2, Name = "Черри", Description = "описание",
                MinGerminationTime = 5, MaxGerminationTime = 10, AgeOfSeedlings = 60,
                GrowingSeason = 100, LandingPattern = 30, PlantHeight = 150, PlantColor = "красный",
                Producer = new Producer { Id = 4, Name = "Гавриш" }
            }
        },
        SeedsInfo = new SeedsInfo
        {
            Id = 3, WeightPack = 1.5, QuantityPack = 20,
            PurchaseDate = new DateTime(2026, 1, 1), ExpirationDate = new DateTime(2027, 12, 31),
            CostPack = 100m, DisposeComment = "—", AmountSeeds = 18, AmountSeedsWeight = 2.2,
            SeedSource = "Куплено", Note = "примечание"
        }
    };

    // ── SeedMapper.ToViewModel ────────────────────────────────────────────────

    [Fact]
    public void SeedMapper_ToViewModel_MapsAllFields()
    {
        var dto = SeedMapper.ToViewModel(SeedReadDbMapper.Read(MakeSeed()).ValueOrThrow());

        Assert.Equal(7, dto.Id);
        Assert.Equal("Томат", dto.Culture);
        Assert.Equal("Черри", dto.Sort);
        Assert.Equal("Гавриш", dto.Producer);
        Assert.Equal(new DateTime(2027, 12, 31), dto.ExpirationDate);
        Assert.Equal(20, dto.QuantityPack);
        Assert.Equal(1.5, dto.WeightPack);
        Assert.Equal(18, dto.AmountSeedsQuantity);
        Assert.Equal(2.2, dto.AmountSeedsWeight);
    }

    // ── SeedReadDbMapper.Read ───────────────────────────────────────────────────

    [Fact]
    public void SeedRead_MapsScalarAndNestedFields()
    {
        var from = MakeSeed();
        var to = SeedReadDbMapper.Read(from).ValueOrThrow();

        Assert.Equal(7, to.Id);
        Assert.Equal(3, to.SeedsInfoId);
        Assert.Equal("Томат", to.Plant.PlantCulture.Name);
        Assert.Equal("Овощи", to.Plant.PlantCulture.Class);
        Assert.Equal("Черри", to.Plant.PlantSort.Name);
        Assert.Equal("Гавриш", to.Plant.PlantSort.Producer.Name);
        Assert.Equal(150, to.Plant.PlantSort.Characteristics.PlantHeight.ToNullable());
        Assert.Equal(18, to.SeedsInfo.AmountSeeds);
        Assert.Equal(2.2, to.SeedsInfo.AmountSeedsWeight.ToNullable());
        Assert.Equal("Куплено", to.SeedsInfo.SeedSource.ToNullable());
        Assert.Equal("примечание", to.SeedsInfo.Note.ToNullable());
    }

    [Fact]
    public void SeedRead_IsDetachedFromMutableEntityGraph()
    {
        var from = MakeSeed(); var to = SeedReadDbMapper.Read(from).ValueOrThrow();
        from.Plant.PlantSort.Name = "Другое"; from.SeedsInfo.AmountSeeds = 0;
        Assert.Equal("Черри", to.Plant.PlantSort.Name); Assert.Equal(18, to.SeedsInfo.AmountSeeds);
    }

    // ── SeedlingReadDbMapper.Read ───────────────────────────────────────────────

    [Fact]
    public void SeedlingRead_PreservesExtendedSharedSortCharacteristics()
    {
        var from = new Seedling { Plant = MakeSeed().Plant };
        from.Plant.PlantSort.MaxGrowingSeason = 120;
        from.Plant.PlantSort.Maturity = "Среднеспелый";
        from.Plant.PlantSort.MaxPlantHeight = 180;
        from.Plant.PlantSort.MaxAgeOfSeedlings = 70;
        from.Plant.PlantSort.LandingRowSpacing = 50;
        from.Plant.PlantSort.MinFruitWeight = 15;
        from.Plant.PlantSort.MaxFruitWeight = 25;
        from.Plant.PlantSort.YieldKg = 3;
        from.Plant.PlantSort.MinFruitSize = 2;
        from.Plant.PlantSort.MaxFruitSize = 7;
        var to = LibraryMapper.ToEditModel(SeedlingReadDbMapper.Read(from).ValueOrThrow().Plant.PlantSort);

        Assert.Equal(120, to.MaxGrowingSeason);
        Assert.Equal("Среднеспелый", to.Maturity);
        Assert.Equal(180, to.MaxPlantHeight);
        Assert.Equal(70, to.MaxAgeOfSeedlings);
        Assert.Equal(50, to.LandingRowSpacing);
        Assert.Equal(15, to.MinFruitWeight);
        Assert.Equal(25, to.MaxFruitWeight);
        Assert.Equal(3, to.YieldKg);
        Assert.Equal(2, to.MinFruitSize);
        Assert.Equal(7, to.MaxFruitSize);
    }

    [Fact]
    public void SeedlingRead_CopiesFieldsAndInfos()
    {
        var infos = new List<SeedlingInfo> { new() { Id = 1 }, new() { Id = 2 } };
        var from = new Seedling
        {
            Id = 9, Weight = 3.3, Quantity = 12, SeedId = 5,
            Plant = new Plant
            {
                Id = 1,
                PlantCulture = new PlantCulture { Id = 1, Name = "Огурец", Class = "Овощи" },
                PlantSort = new PlantSort { Id = 2, Name = "Кураж", Producer = new Producer { Id = 3, Name = "Аэлита" } }
            },
            SeedlingInfos = infos
        };
        var to = new SeedlingReadModel(SeedlingReadDbMapper.Read(from).ValueOrThrow());

        Assert.Equal(9, to.Id);
        Assert.Equal(3.3, to.Weight);
        Assert.Equal(12, to.Quantity);
        Assert.Equal(5, to.SeedId);
        Assert.Equal("Огурец", to.Plant.PlantCulture.Name);
        Assert.Equal("Кураж", to.Plant.PlantSort.Name);
        Assert.Equal("Аэлита", to.Plant.PlantSort.Producer.Name);
        Assert.NotSame(infos, to.SeedlingInfos);
        Assert.Equal(infos.Select(info => info.Id), to.SeedlingInfos.Select(info => info.Id));
    }

    // ── GardenPlanMapper.MapSpot ──────────────────────────────────────────────

    [Fact]
    public void GardenPlanMapper_MapSpot_MapsFields()
    {
        var spot = new PlantingSpot
        {
            Id = 5, Row = 2, Column = 3,
            StateTypeName = nameof(PlantedSpotState),
            SeedlingInfoId = 8,
            PlantedDate = new DateTime(2026, 5, 1),
            Note = "Томат Черри"
        };

        var dto = GardenPlanMapper.MapSpot(DbGridMapper.Receipt(spot));

        Assert.Equal(5, dto.Id);
        Assert.Equal(2, dto.Row);
        Assert.Equal(3, dto.Column);
        Assert.Equal(nameof(PlantedSpotState), dto.StateTypeName);
        Assert.Equal(8, dto.SeedlingInfoId);
        Assert.Equal(new DateTime(2026, 5, 1), dto.PlantedDate);
        Assert.Equal("Томат Черри", dto.PlantLabel);
        Assert.Equal("Томат Черри", dto.Note);
    }

    // ── GardenPlanMapper.MapElement ───────────────────────────────────────────

    [Fact]
    public void GardenPlanMapper_MapElement_MapsTypeAndStateAndSpots()
    {
        var bed = new Bed
        {
            Id = 1, Name = "Грядка 1",
            X = 10, Y = 20, DisplayWidth = 120, DisplayHeight = 80,
            StateTypeName = nameof(Bonfire.Domain.Garden.States.PlannedState),
            GridRows = 2, GridColumns = 2, Orientation = "СЮ",
            PlantingSpots =
            [
                new() { Row = 1, Column = 0 },
                new() { Row = 0, Column = 1 },
                new() { Row = 0, Column = 0 }
            ]
        };

        var dto = GardenPlanMapper.MapElement(GardenReadDbMapper.ReadElement(bed).ValueOrThrow(), containerW: 800, containerH: 600);

        Assert.Equal("Bed", dto.ElementType);
        Assert.Equal("Грядка 1", dto.Name);
        Assert.Equal(800, dto.ContainerCanvasWidth);
        Assert.Equal(600, dto.ContainerCanvasHeight);
        Assert.Equal("СЮ", dto.Orientation);
        Assert.Equal(3, dto.PlantingSpots.Count);
        // Ячейки отсортированы по Row, затем Column
        Assert.Equal((0, 0), (dto.PlantingSpots[0].Row, dto.PlantingSpots[0].Column));
        Assert.Equal((0, 1), (dto.PlantingSpots[1].Row, dto.PlantingSpots[1].Column));
        Assert.Equal((1, 0), (dto.PlantingSpots[2].Row, dto.PlantingSpots[2].Column));
    }

    // ── GardenPlanMapper.MapGarden ────────────────────────────────────────────

    [Fact]
    public void GardenPlanMapper_MapGarden_WiresContainerReferences()
    {
        var garden = new Garden
        {
            Id = 1, Name = "Участок", CanvasWidth = 800, CanvasHeight = 600,
            Elements =
            [
                new Bed
                {
                    Id = 1, Name = "Грядка",
                    StateTypeName = nameof(Bonfire.Domain.Garden.States.PlannedState)
                }
            ],
            Greenhouses = []
        };

        var dto = GardenPlanMapper.MapGarden(GardenReadDbMapper.ReadGarden(garden).ValueOrThrow());

        Assert.Single(dto.Elements);
        // Каждый элемент получает ссылки на коллекции братьев для проверки коллизий
        Assert.Same(dto.Elements, dto.Elements[0].ContainerElements);
        Assert.Same(dto.Greenhouses, dto.Elements[0].ContainerGreenhouses);
    }
}
