using System.Collections.Immutable;
using Bonfire.Application.Planning.Libraries;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Libraries;
using Bonfire.Domain.Planning;
using Bonfire.Domain.Planning.GrowingProfiles;
using Bonfire.Domain.Planning.Regions;
using BonfireDB.Entities.Base;
using BonfireDB.Entities.Planning;
using Microsoft.EntityFrameworkCore;

namespace BonfireDB.Planning.Libraries;

public sealed partial class DbPlanningLibraries(IUnitOfWorkFactory units)
    : IPlanningLibraryReader, IPlanningLibraryWriter
{
    public Task<Result<ImmutableArray<RegionFromViewModel>>> ReadRegionsAsync(bool includeArchived,
        CancellationToken cancellationToken)
        => DbRead.TryAsync(async () =>
        {
            await using var unit = units.Create();
            var rows = await unit.Repository<PlanningRegionRow>().Rows.AsNoTracking()
                .Where(x => includeArchived || !x.IsArchived).OrderBy(x => x.Name)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            var result = ImmutableArray.CreateBuilder<RegionFromViewModel>();
            foreach (var row in rows)
            {
                var mapped = PlanningLibraryDbMapper.Read(row);
                if (mapped.IsFailure) return mapped.Bind(_ => Result.Success(result.ToImmutable()));
                result.Add(mapped.ValueOrThrow());
            }

            return Result.Success(result.ToImmutable());
        }, "регионов");

    public Task<Result<ImmutableArray<RegionalPlantingWindow>>> ReadRegionalWindowsAsync(RegionId region,
        CultureId culture, CancellationToken cancellationToken)
        => DbRead.TryAsync(async () =>
        {
            await using var unit = units.Create();
            var rows = await unit.Repository<RegionalPlantingWindowRow>().Rows.AsNoTracking()
                .Where(x => x.RegionId == region.Value && x.CultureId == culture.Value)
                .OrderBy(x => x.Location).ThenBy(x => x.Placement).ThenBy(x => x.Purpose)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            var result = ImmutableArray.CreateBuilder<RegionalPlantingWindow>();
            foreach (var row in rows)
            {
                var mapped = PlanningLibraryDbMapper.Read(row);
                if (mapped.IsFailure) return mapped.Bind(_ => Result.Success(result.ToImmutable()));
                result.Add(mapped.ValueOrThrow());
            }

            return Result.Success(result.ToImmutable());
        }, "региональных сроков");

    public Task<Result<GrowingProfilesFromViewModel>> ReadProfilesAsync(CultureId culture, SortId sort,
        CancellationToken cancellationToken)
        => DbRead.TryAsync(async () =>
        {
            await using var unit = units.Create();
            var cultureRow = await unit.Repository<CultureGrowingProfileRow>().Rows.AsNoTracking()
                .SingleOrDefaultAsync(x => x.CultureId == culture.Value, cancellationToken).ConfigureAwait(false);
            var sortRow = await unit.Repository<SortPlanningSettingsRow>().Rows.AsNoTracking()
                .SingleOrDefaultAsync(x => x.SortId == sort.Value, cancellationToken).ConfigureAwait(false);
            var cultureResult = cultureRow is null
                ? Result.Success(Option.None<CultureGrowingProfile>())
                : PlanningLibraryDbMapper.Read(cultureRow).Bind(x => Result.Success(Option.Some(x)));
            var sortResult = sortRow is null
                ? Result.Success(Option.None<SortPlanningSettings>())
                : PlanningLibraryDbMapper.Read(sortRow).Bind(x => Result.Success(Option.Some(x)));
            return cultureResult.Bind(c =>
                sortResult.Bind(s => Result.Success(new GrowingProfilesFromViewModel(c, s))));
        }, "агропрофилей");
}