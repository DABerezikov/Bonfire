using Bonfire.Application.Seedlings;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Seeds;
using BonfireDB.Entities;
using BonfireDB.Entities.Base;

namespace BonfireDB.Sowing;

public sealed class DbSowingSessionFactory(IUnitOfWorkFactory units) : ISowingSessionFactory
{
    public ISowingSession Create() => new Session(units.Create());

    private sealed class Session(IUnitOfWork unit) : ISowingSession
    {
        private Option<Seed> _loaded = Option.None<Seed>();

        public async Task<Result<SeedStock>> LoadStockAsync(int seedId, SeedUnit measure, CancellationToken cancellationToken)
        {
            var seed = await unit.Repository<Seed>().GetAsync(seedId, cancellationToken).ConfigureAwait(false);
            if (seed is null) return Result.Failure<SeedStock>("Пакет семян не найден");
            var amount = measure switch
            {
                SeedUnit.Pieces => SeedAmount.Create(seed.SeedsInfo.AmountSeeds, measure),
                SeedUnit.Grams when seed.SeedsInfo.AmountSeedsWeight is double weight => SeedAmount.Create(weight, measure),
                SeedUnit.Grams => Result.Failure<SeedAmount>("Вес остатка семян не указан"),
                _ => Result.Failure<SeedAmount>("Неизвестная единица семян")
            };
            return amount.Bind(value =>
            {
                _loaded = Option.Some(seed);
                return Result.Success(new SeedStock(value));
            });
        }

        public Task<SavedSowing> SaveAsync(SowingChange change, SowingDetails details, CancellationToken cancellationToken)
            => _loaded.Match(seed => SaveAsync(seed, change, details, cancellationToken),
                () => throw new InvalidOperationException("Перед сохранением загрузите пакет семян"));

        private async Task<SavedSowing> SaveAsync(Seed seed, SowingChange change, SowingDetails details, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var place = details.Place.Match<string?>(value => value, () => null);
            var batch = new Seedling
            {
                Plant = seed.Plant, SeedId = seed.Id,
                Quantity = change.Sown.Unit == SeedUnit.Pieces ? change.Sown.Value : 0,
                Weight = change.Sown.Unit == SeedUnit.Grams ? change.Sown.Value : 0,
                LandingDate = details.Date, LunarPhase = details.LunarPhase,
                SeedlingSource = details.Source, PlantPlace = place,
                SeedlingInfos = [new SeedlingInfo
                {
                    LandingDate = details.Date, LunarPhase = details.LunarPhase,
                    SeedlingSource = details.Source, PlantPlace = place, SeedlingNumber = 0
                }]
            };
            if (change.Sown.Unit == SeedUnit.Pieces)
                seed.SeedsInfo.AmountSeeds = change.Remaining.Available.Value;
            else
                seed.SeedsInfo.AmountSeedsWeight = change.Remaining.Available.Value;

            await unit.Repository<Seedling>().AddAsync(batch, cancellationToken).ConfigureAwait(false);
            await unit.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            var grams = seed.SeedsInfo.AmountSeedsWeight is double weight ? Option.Some(weight) : Option.None<double>();
            return new SavedSowing(batch.Id, seed.SeedsInfo.AmountSeeds, grams);
        }

        public ValueTask DisposeAsync() => unit.DisposeAsync();
    }
}
