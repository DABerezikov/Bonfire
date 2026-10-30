using Bonfire.Domain.Functional;
using Bonfire.Domain.Seeds;

namespace Bonfire.Application.Seedlings;

public sealed class SowSeeds(ISowingSessionFactory sessions, ILunarCalendar calendar) : ISowSeeds
{
    public async Task<Result<SowingReceipt>> ExecuteAsync(SowingRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Place);
        cancellationToken.ThrowIfCancellationRequested();
        if (request.Date == default || string.IsNullOrWhiteSpace(request.Source))
            return Result.Failure<SowingReceipt>("Укажите дату и источник рассады");

        await using var session = sessions.Create();
        var available = await session.LoadStockAsync(request.SeedId, request.Unit, cancellationToken).ConfigureAwait(false);
        var change = available.Bind(stock => SeedAmount.Create(request.Amount, request.Unit).Bind(stock.Sow));
        return await change.Match(async sowing =>
        {
            var place = request.Place.Match(value => string.IsNullOrWhiteSpace(value)
                ? Option.None<string>() : Option.Some(value), () => Option.None<string>());
            var details = new SowingDetails(request.Date, request.Source, place, calendar.GetPhase(request.Date));
            var saved = await session.SaveAsync(sowing, details, cancellationToken).ConfigureAwait(false);
            return Result.Success(new SowingReceipt(request.SeedId, sowing.Sown, details, saved));
        }, error => Task.FromResult(Result.Failure<SowingReceipt>(error))).ConfigureAwait(false);
    }
}
