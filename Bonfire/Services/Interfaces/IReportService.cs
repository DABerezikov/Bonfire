using System.Collections.Generic;
using Bonfire.Application.Planning;
using Bonfire.Domain.Functional;

namespace Bonfire.Services.Interfaces;

public interface IReportService
{
    Result<string> CreateSeedShoppingReport(SeedShoppingListFromViewModel shopping);
    void CreateSeedsReport(IEnumerable<SeedReportItem> items, string fileName = "Семена");
    void CreateSeedlingsReport(IEnumerable<SeedlingReportItem> items, string fileName = "Рассада");
}

public record SeedReportItem(
    string Culture,
    string Sort,
    string Producer,
    string ExpirationDate,
    double WeightPack,
    double QuantityPack);

public record SeedlingReportItem(
    string Culture,
    string Sort,
    string Producer,
    string LandingDate,
    double Weight,
    double Quantity,
    int CountGerminate,
    int Balance);
