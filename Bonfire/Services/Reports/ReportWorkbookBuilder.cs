using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using Bonfire.Domain.Functional;
using Bonfire.Models;
using Bonfire.Services.Interfaces;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace Bonfire.Services.Reports;

public interface IReportWorkbookBuilder
{
    Result<ImmutableArray<byte>> BuildSeeds(IReadOnlyList<SeedReportItem> items, int currentYear);
    Result<ImmutableArray<byte>> BuildSeedlings(IReadOnlyList<SeedlingReportItem> items);
}

internal sealed class ReportWorkbookBuilder : IReportWorkbookBuilder
{
    public Result<ImmutableArray<byte>> BuildSeeds(IReadOnlyList<SeedReportItem> items, int currentYear)
    {
        if (items.Count == 0) return Result.Failure<ImmutableArray<byte>>("Нет данных для формирования отчёта");
        return items.Traverse(item =>
                DateTime.TryParse(item.ExpirationDate, CultureInfo.CurrentCulture, DateTimeStyles.None, out var date)
                    ? Result.Success(date)
                    : Result.Failure<DateTime>(
                        $"Некорректный срок годности сорта «{item.Sort}»: {item.ExpirationDate}"))
            .Bind(dates => Build("Семена", sheet =>
            {
                SetSeedsHeader(sheet);
                FillSeedsSheet(items, sheet, dates, currentYear);
                AutoFitSeedsColumns(sheet);
            }));
    }

    public Result<ImmutableArray<byte>> BuildSeedlings(IReadOnlyList<SeedlingReportItem> items)
        => items.Count == 0
            ? Result.Failure<ImmutableArray<byte>>("Нет данных для формирования отчёта")
            : Build("Рассада", sheet =>
            {
                SetSeedlingsHeader(sheet);
                FillSeedlingsSheet(items, sheet);
                AutoFitSeedlingsColumns(sheet);
            });

    private static Result<ImmutableArray<byte>> Build(string name, Action<ExcelWorksheet> fill)
    {
        try
        {
            ExcelPackage.License.SetNonCommercialPersonal("Bonfire");
            using var package = new ExcelPackage();
            var sheet = package.Workbook.Worksheets.Add(name);
            ApplyCommonSettings(sheet);
            fill(sheet);
            return Result.Success(package.GetAsByteArray().ToImmutableArray());
        }
        catch (Exception error)
        {
            Debug.WriteLine(error);
            return Result.Failure<ImmutableArray<byte>>($"Не удалось сформировать отчёт: {error.Message}");
        }
    }

    private static void FillSeedsSheet(IReadOnlyList<SeedReportItem> list, ExcelWorksheet sheet,
        IReadOnlyList<DateTime> dates, int currentYear)
    {
        var tempCulture = list[0].Culture;
        var cell = sheet.Cells[2, 1, 2, 7];
        MergeAndStyle(cell);
        cell.Value = tempCulture;

        for (int i = 0, row = 3; i < list.Count; i++, row++)
        {
            if (list[i].Culture != tempCulture)
            {
                tempCulture = list[i].Culture;
                cell = sheet.Cells[row, 1, row, 7];
                MergeAndStyle(cell);
                cell.Value = tempCulture;
                row++;
            }

            sheet.Cells[row, 1].Value = list[i].Sort;
            sheet.Cells[row, 2].Value = list[i].Producer;
            sheet.Cells[row, 3].Value = dates[i].Year.ToString("D4", CultureInfo.CurrentCulture);

            var yearDiff = currentYear - dates[i].Year;
            if (yearDiff > 0)
            {
                sheet.Cells[row, 3].Style.Font.Bold = true;
            }
            else if (yearDiff == 0)
            {
                sheet.Cells[row, 3].Style.Font.Italic = true;
                sheet.Cells[row, 3].Style.Font.UnderLine = true;
            }

            sheet.Cells[row, 4].Value = list[i].WeightPack;
            sheet.Cells[row, 5].Value = list[i].QuantityPack;

            if (i == list.Count - 1)
                ApplyBorders(sheet, row);
        }
    }

    private static void FillSeedlingsSheet(IReadOnlyList<SeedlingReportItem> list, ExcelWorksheet sheet)
    {
        var tempCulture = list[0].Culture;
        var cell = sheet.Cells[2, 1, 2, 7];
        MergeAndStyle(cell);
        cell.Value = tempCulture;

        for (int i = 0, row = 3; i < list.Count; i++, row++)
        {
            if (list[i].Culture != tempCulture)
            {
                tempCulture = list[i].Culture;
                cell = sheet.Cells[row, 1, row, 7];
                MergeAndStyle(cell);
                cell.Value = tempCulture;
                row++;
            }

            sheet.Cells[row, 1].Value = list[i].Sort;
            sheet.Cells[row, 2].Value = list[i].Producer;
            sheet.Cells[row, 3].Value = list[i].LandingDate;
            sheet.Cells[row, 4].Value = list[i].Weight != 0 ? list[i].Weight : null;
            sheet.Cells[row, 5].Value = list[i].Quantity != 0 ? list[i].Quantity : null;
            sheet.Cells[row, 6].Value = list[i].CountGerminate != 0 ? list[i].CountGerminate : null;
            sheet.Cells[row, 7].Value = list[i].Balance != 0 ? list[i].Balance : null;

            if (i == list.Count - 1)
                ApplyBorders(sheet, row);
        }
    }

    private static void ApplyBorders(ExcelWorksheet sheet, int lastRow)
    {
        var range = sheet.Cells[1, 1, lastRow, 7];
        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
        range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
        range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
    }

    private static void MergeAndStyle(ExcelRange cell)
    {
        cell.Merge = true;
        cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
        cell.Style.Font.Bold = true;
        cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
        cell.Style.Fill.BackgroundColor.SetColor(Color.DarkGray);
        cell.Style.Font.Color.SetColor(Color.Black);
    }

    private static void SetSeedsHeader(ExcelWorksheet sheet)
    {
        sheet.Cells[1, 1].Value = "Сорт";
        sheet.Cells[1, 2].Value = "Фирма";
        sheet.Cells[1, 3].Value = "Годен до";
        sheet.Cells[1, 4].Value = Units.GramsOption;
        sheet.Cells[1, 5].Value = Units.PiecesOption;
        sheet.Cells[1, 1, 1, 7].Style.Font.Bold = true;
    }

    private static void SetSeedlingsHeader(ExcelWorksheet sheet)
    {
        sheet.Cells[1, 1].Value = "Сорт";
        sheet.Cells[1, 2].Value = "Производитель";
        sheet.Cells[1, 3].Value = "Дата посева";
        sheet.Cells[1, 4].Value = Units.GramAbbr;
        sheet.Cells[1, 5].Value = Units.PiecesAbbr;
        sheet.Cells[1, 6].Value = "Взошло";
        sheet.Cells[1, 7].Value = "Остаток";
        sheet.Cells[1, 1, 1, 7].Style.Font.Bold = true;
    }

    private static void ApplyCommonSettings(ExcelWorksheet sheet)
    {
        sheet.PrinterSettings.BottomMargin = 0.1;
        sheet.PrinterSettings.FooterMargin = 0;
        sheet.PrinterSettings.HeaderMargin = 0;
        sheet.PrinterSettings.LeftMargin = 0.2;
        sheet.PrinterSettings.RightMargin = 0.1;
        sheet.PrinterSettings.TopMargin = 0.1;
        sheet.PrinterSettings.RepeatRows = sheet.Cells["1:1"];

        sheet.Cells.Style.Font.Name = "Calibri";
        sheet.Cells.Style.Font.Size = 12;

        sheet.Columns[1, 2].Style.WrapText = true;
        sheet.Cells["A:B"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
        sheet.Cells["C:G"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        sheet.Cells["A1:G1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        sheet.Cells["A:G"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
    }

    private static void AutoFitSeedsColumns(ExcelWorksheet sheet)
    {
        sheet.Columns[1].AutoFit(20);
        sheet.Columns[2].AutoFit(15);
        sheet.Columns[3].AutoFit(7);
        sheet.Columns[4].AutoFit(7);
        sheet.Columns[5].AutoFit(7);
        sheet.Columns[6].AutoFit(18);
        sheet.Columns[7].AutoFit(18);
    }

    private static void AutoFitSeedlingsColumns(ExcelWorksheet sheet)
    {
        sheet.Columns[1].AutoFit(20);
        sheet.Columns[2].AutoFit(15);
        sheet.Columns[3].AutoFit(12);
        sheet.Columns[4].AutoFit(7);
        sheet.Columns[5].AutoFit(7);
        sheet.Columns[6].AutoFit(9);
        sheet.Columns[7].AutoFit(9);
    }
}