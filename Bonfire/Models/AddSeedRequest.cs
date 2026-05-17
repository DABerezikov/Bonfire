using System;

namespace Bonfire.Models;

public record AddSeedRequest(
    string Culture,
    string Sort,
    string Producer,
    string Class,
    string SeedSource,
    string SizeUnit,
    double QuantityInPack,
    double PackCount,
    decimal CostPack,
    DateTime BestBy,
    string? Note);
