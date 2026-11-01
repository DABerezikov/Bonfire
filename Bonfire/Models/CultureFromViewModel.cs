namespace Bonfire.Models;

public class CultureFromViewModel
{
    public int Id { get; set; }
    public string? Name { get; set; }

    public override string? ToString() => Name;
}