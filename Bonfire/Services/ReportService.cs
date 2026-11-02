using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Bonfire.Domain.Functional;
using Bonfire.Application.Planning;
using Bonfire.Services.Interfaces;
using Bonfire.Services.Reports;

namespace Bonfire.Services;

internal sealed class ReportService(
    IUserDialog userDialog,
    IReportWorkbookBuilder workbooks,
    IReportPathPolicy paths,
    IReportFileWriter files,
    TimeProvider clock) : IReportService
{
    public void CreateSeedsReport(IEnumerable<SeedReportItem> items, string fileName = "Семена")
    {
        var date = clock.GetLocalNow().DateTime;
        Export(workbooks.BuildSeeds(items.ToArray(), date.Year), fileName, date);
    }

    public void CreateSeedlingsReport(IEnumerable<SeedlingReportItem> items, string fileName = "Рассада")
        => Export(workbooks.BuildSeedlings(items.ToArray()), fileName, clock.GetLocalNow().DateTime);

    public Result<string> CreateSeedShoppingReport(SeedShoppingListFromViewModel shopping)
        => Write(workbooks.BuildSeedShopping(shopping), $"Покупка_семян_{shopping.SeasonYear}_план{shopping.PlanId}", clock.GetLocalNow().DateTime);

    private Result<string> Write(Result<ImmutableArray<byte>> content, string fileName, DateTime date)
        => content.Bind(bytes => paths.Resolve(fileName, date).Bind(path => files.Save(path, bytes)));

    private void Export(Result<ImmutableArray<byte>> content, string fileName, DateTime date)
        => Write(content, fileName, date).Match(path =>
        {
            userDialog.Information($"Отчет {Path.GetFileName(path)} сформирован", "Информация");
            return true;
        }, error =>
        {
            userDialog.Warning(error, "Предупреждение");
            return false;
        });
}
