namespace Bonfire.Models;

public class ProducerFromViewModel
{
    internal int Id { get; set; }
    public string? Name { get; set; }

    public override string? ToString()
    {
        return Name;
    }
}