namespace Bonfire.Models;

public class SortFromSeedlingsViewModel
{
    internal int Id { get; set; }
    public string? Sort { get; set; }
    public string? Culture { get; set; }
    public override string? ToString() => Sort;
}