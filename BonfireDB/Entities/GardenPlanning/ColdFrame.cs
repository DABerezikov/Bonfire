namespace BonfireDB.Entities.GardenPlanning;

// Парник (TPH дискриминатор "ColdFrame")
public class ColdFrame : GardenElement
{
    public string? CoverMaterial { get; set; }  // Плёнка / спанбонд / стекло
}
