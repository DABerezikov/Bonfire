﻿namespace Bonfire.Models;

public class CultureFromViewModel
{
    internal int Id { get; set; }
    public string? Name { get; set; }

    public override string? ToString()
    {
        return Name;
    }
}

public class SortFromViewModel
{
    internal int Id { get; set; }
    public string? Name { get; set; }

    public override string? ToString()
    {
        return Name;
    }
}
public class ProducerFromViewModel
{
    internal int Id { get; set; }
    public string? Name { get; set; }

    public override string? ToString()
    {
        return Name;
    }
}