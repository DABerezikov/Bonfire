using System;
using Bonfire.Application.Seeds;
using Bonfire.Domain.Functional;

namespace Bonfire.Models.Mappers;

internal static class SeedAcquisitionMapper
{
    public static Result<AcquireSeedsFromViewModel> ToRequest(AddSeedRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Units.ResolveSeedUnit(request.SizeUnit).Bind(unit => Result.Success(new AcquireSeedsFromViewModel(new(
            request.Culture, request.Sort, request.Producer, request.Class, request.SeedSource, unit,
            request.QuantityInPack, request.PackCount, request.CostPack, request.BestBy,
            request.Note.ToOption()))));
    }
}
