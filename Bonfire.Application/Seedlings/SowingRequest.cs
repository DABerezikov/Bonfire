using Bonfire.Domain.Functional;
using Bonfire.Domain.Seeds;

namespace Bonfire.Application.Seedlings;

public sealed record SowingRequest(int SeedId, double Amount, SeedUnit Unit,
    DateTime Date, string Source, Option<string> Place);

public sealed record SowingDetails(DateTime Date, string Source, Option<string> Place, string LunarPhase);

public sealed record SavedSowing(int BatchId, double RemainingPieces, Option<double> RemainingGrams);

public sealed record SowingReceipt(int SeedId, SeedAmount Sown, SowingDetails Details, SavedSowing Saved);
