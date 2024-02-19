namespace Bonfire.Models;

public class SortFromSeedsViewModel
{
    internal int Id { get; set; }
    public string? Name { get; set; }

    public override string? ToString() => Name;
}