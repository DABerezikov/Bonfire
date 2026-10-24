using Bonfire.Domain.Functional;
using Bonfire.Domain.Seeds;

namespace Bonfire.Application.Seedlings;

public interface ISowSeeds
{
    Task<Result<SowingReceipt>> ExecuteAsync(SowingRequest request, CancellationToken cancellationToken = default);
}

public interface ILunarCalendar
{
    string GetPhase(DateTime date);
}

public interface ISowingSessionFactory
{
    ISowingSession Create();
}

/// <summary>Загрузка и сохранение одной операции в общем контексте хранения.</summary>
public interface ISowingSession : IAsyncDisposable
{
    Task<Result<SeedStock>> LoadStockAsync(int seedId, SeedUnit unit, CancellationToken cancellationToken);
    Task<SavedSowing> SaveAsync(SowingChange change, SowingDetails details, CancellationToken cancellationToken);
}
