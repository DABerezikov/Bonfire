using Bonfire.Domain.Functional;

namespace Bonfire.Models;

public sealed record GardenPlanFromViewModel
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int Year { get; init; }
    public Option<string> Description { get; init; } = Option.None<string>();
}