using System.Diagnostics;
using Bonfire.Application.Planning.Libraries;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Libraries;
using Bonfire.Domain.Planning;
using Bonfire.Domain.Planning.GrowingProfiles;
using Bonfire.Domain.Planning.Regions;
using BonfireDB.Entities.Planning;

namespace BonfireDB.Planning.Libraries;

internal static class PlanningLibraryDbMapper
{
    internal static AnnualWindowRow Write(AnnualDateWindow value) => new()
    {
        StartMonth = value.StartMonth, StartDay = value.StartDay, EndMonth = value.EndMonth,
        EndDay = value.EndDay, EndInNextYear = value.EndInNextYear
    };

    internal static AnnualWindowRow? Write(Option<AnnualDateWindow> value)
        => value.Match<AnnualWindowRow?>(Write, () => null);

    internal static Result<AnnualDateWindow> Read(AnnualWindowRow value)
        => AnnualDateWindow.Create(value.StartMonth, value.StartDay, value.EndMonth, value.EndDay, value.EndInNextYear);

    private static Result<Option<AnnualDateWindow>> ReadOptional(AnnualWindowRow? value)
        => value is null ? Result.Success(Option.None<AnnualDateWindow>())
            : Read(value).Bind(window => Result.Success(Option.Some(window)));

    internal static Result<RegionFromViewModel> Read(PlanningRegionRow row)
        => RegionId.Create(row.Id).Bind(id => ReadOptional(row.LastSpringFrost).Bind(spring =>
            ReadOptional(row.FirstAutumnFrost).Bind(autumn => RegionDefinition.Create(row.Name, row.TimeZoneId,
                new RegionReference { LastSpringFrost = spring, FirstAutumnFrost = autumn,
                    Note = row.Note.ToOption(), Source = row.Source.ToOption() }, row.IsArchived)
                .Bind(region => Result.Success(new RegionFromViewModel(id, region))))));

    internal static DayRangeRow? Write(Option<DayRange> value)
        => value.Match<DayRangeRow?>(Write, () => null);

    private static DayRangeRow Write(DayRange value) => new() { Min = value.Min, Max = value.Max };

    private static Result<Option<DayRange>> ReadOptional(DayRangeRow? value)
        => value is null ? Result.Success(Option.None<DayRange>())
            : DayRange.Create(value.Min, value.Max).Bind(range => Result.Success(Option.Some(range)));

    internal static PlanningExtrasRow Write(PlanningExtras value) => new()
    {
        PrickingMode = Mode(value.PrickingOut), PrickingOut = Settings(value.PrickingOut),
        HardeningMode = Mode(value.Hardening), Hardening = Settings(value.Hardening),
        GerminationPercentage = value.GerminationPercentage.Map(x => x.Value).ToNullable(),
        LossPercentage = value.LossPercentage.Map(x => x.Value).ToNullable(),
        LunarGroup = value.LunarGroup.Map(x => (int)x).ToNullable(),
        ReferenceSoilTemperatureC = value.ReferenceSoilTemperatureC.ToNullable(),
        YieldPerPlantKg = value.YieldPerPlantKg.ToNullable(), YieldPerM2Kg = value.YieldPerM2Kg.ToNullable(),
        LifeCycleJson = value.LifeCycle.Match<string?>(PlanningSnapshotJson.WriteLifeCycle, () => null)
    };

    private static int Mode(OperationOverride<DayRange> operation) => operation switch
    {
        OperationOverride<DayRange>.Inherit => 0,
        OperationOverride<DayRange>.Disabled => 1,
        OperationOverride<DayRange>.Enabled => 2,
        _ => throw new UnreachableException()
    };

    private static DayRangeRow? Settings(OperationOverride<DayRange> operation)
        => operation is OperationOverride<DayRange>.Enabled enabled ? Write(enabled.Settings) : null;

    private static Result<OperationOverride<DayRange>> ReadOperation(int mode, DayRangeRow? range)
    {
        if (mode == 0 && range is null)
            return Result.Success<OperationOverride<DayRange>>(new OperationOverride<DayRange>.Inherit());
        if (mode == 1 && range is null)
            return Result.Success<OperationOverride<DayRange>>(new OperationOverride<DayRange>.Disabled());
        if (mode == 2 && range is not null)
            return DayRange.Create(range.Min, range.Max).Bind(value =>
                Result.Success<OperationOverride<DayRange>>(new OperationOverride<DayRange>.Enabled(value)));
        return Result.Failure<OperationOverride<DayRange>>("Повреждены настройки операции в справочнике");
    }

    private static Result<PlanningExtras> Read(PlanningExtrasRow row)
        => ReadOperation(row.PrickingMode, row.PrickingOut).Bind(pricking =>
            ReadOperation(row.HardeningMode, row.Hardening).Bind(hardening =>
                row.GerminationPercentage.ToOption().Match(value => GerminationPercentage.Create(value)
                    .Bind(x => Result.Success(Option.Some(x))), () => Result.Success(Option.None<GerminationPercentage>()))
                .Bind(germination => row.LossPercentage.ToOption().Match(value => LossPercentage.Create(value)
                    .Bind(x => Result.Success(Option.Some(x))), () => Result.Success(Option.None<LossPercentage>()))
                .Bind(losses => ReadLunar(row.LunarGroup).Bind(lunar => Result.Success(new PlanningExtras
                {
                    PrickingOut = pricking, Hardening = hardening, GerminationPercentage = germination,
                    LossPercentage = losses, LunarGroup = lunar, ReferenceSoilTemperatureC = row.ReferenceSoilTemperatureC.ToOption(),
                    YieldPerPlantKg = row.YieldPerPlantKg.ToOption(), YieldPerM2Kg = row.YieldPerM2Kg.ToOption()
                })))))).Bind(extras => row.LifeCycleJson.ToOption().Match(json => PlanningSnapshotJson.ReadLifeCycle(json)
                    .Bind(life => Result.Success(extras with { LifeCycle = Option.Some(life) })), () => Result.Success(extras)));

    private static Result<Option<LunarGroup>> ReadLunar(int? value)
        => value.ToOption().Match(id => Enum.IsDefined((LunarGroup)id)
            ? Result.Success(Option.Some((LunarGroup)id)) : Result.Failure<Option<LunarGroup>>("Неизвестная лунная группа"),
            () => Result.Success(Option.None<LunarGroup>()));

    internal static Result<CultureGrowingProfile> Read(CultureGrowingProfileRow row)
        => CultureId.Create(row.CultureId).Bind(id => ReadOptional(row.Germination).Bind(germination =>
            ReadOptional(row.SeedlingAge).Bind(age => ReadOptional(row.Vegetation).Bind(vegetation =>
                Read(row.Extras).Bind(extras => CultureGrowingProfile.Create(id,
                    new GrowingProfileParameters { Germination = germination, SeedlingAge = age, Vegetation = vegetation }, extras))))));

    internal static Result<SortPlanningSettings> Read(SortPlanningSettingsRow row)
        => SortId.Create(row.SortId).Bind(id => Read(row.Extras).Bind(extras => SortPlanningSettings.Create(id, extras)));

    internal static Result<RegionalPlantingWindow> Read(RegionalPlantingWindowRow row)
        => RegionId.Create(row.RegionId).Bind(region => CultureId.Create(row.CultureId).Bind(culture =>
            GrowingConditions.Create((GrowingLocation)row.Location, (PlantPlacement)row.Placement).Bind(conditions =>
                Read(row.Window).Bind(window => RegionalPlantingWindow.Create(region, culture, conditions,
                    (RegionalWindowPurpose)row.Purpose, window, row.Source.ToOption(), row.Note.ToOption())))));
}
