using System.Collections.Immutable;
using Bonfire.Application.Seeds;
using Bonfire.Domain.Functional;
using BonfireDB.SeedEditing;

namespace ViewModels.Tests;

internal static class SeedReadFixtures
{
    public static SeedReadFromViewModel Read(this Seed seed) => SeedReadDbMapper.Read(seed).ValueOrThrow();
    public static Result<ImmutableArray<SeedReadFromViewModel>> ReadSeeds(this IEnumerable<Seed> seeds)
        => seeds.Traverse(SeedReadDbMapper.Read);
}
