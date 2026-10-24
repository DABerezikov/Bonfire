using System;
using Bonfire.Domain.Seeds;
using BonfireDB.Entities;

namespace Bonfire.Models;

public sealed record SowingRequest(int SeedId, double Amount, SeedUnit Unit,
    DateTime Date, string Source, string? Place);

// Временный контракт существующего UI; при выделении Application EF-типы заменяются моделями чтения.
public sealed record SowingReceipt(Seedling Batch, Seed Seed);
