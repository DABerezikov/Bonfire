using System;

namespace Bonfire.Models;

public class SeedlingInfoFromViewModel
{
    public int Id { get; set; }
    public int Number { get; set; }
    public DateTime? GerminationData { get; set; }
    public DateTime? QuenchingDate { get; set; }
    public bool IsQuarantine { get; set; }

}