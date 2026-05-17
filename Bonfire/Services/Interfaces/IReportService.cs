using System.Collections.Generic;

namespace Bonfire.Services.Interfaces;

public interface IReportService
{
    void CreateSeedsReport(IEnumerable<SeedReportItem> items, string fileName = "Семена");
    void CreateSeedlingsReport(IEnumerable<SeedlingReportItem> items, string fileName = "Рассада");
}

public record SeedReportItem(
    string Culture, string Sort, string Producer,
    string ExpirationDate, double WeightPack, double QuantityPack);

public record SeedlingReportItem(
    string Culture, string Sort, string Producer,
    string LandingDate, double Weight, double Quantity,
    int CountGerminate, int Balance);
