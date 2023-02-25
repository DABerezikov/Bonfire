namespace Bonfire.Models;

public class CultureFromViewModel
{
    internal int Id { get; set; }
    public string? Name { get; set; }

    public override string? ToString()
    {
        return Name;
    }
}