namespace Bonfire.Domain.Garden;

public sealed record GardenPlanDefinition(int Year)
{
    public string Name => $"План {Year}";
}