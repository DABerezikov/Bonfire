using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using Bonfire.Services.Interfaces;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace Bonfire.Services;

internal class ReportService(IUserDialog userDialog) : IReportService
{
    public void CreateSeedsReport(IEnumerable<SeedReportItem> items, string fileName = "Семена")
    {
        var list = items.ToList();
        if (list.Count == 0)
        {
            userDialog.Warning("Нет данных для формирования отчёта", "Предупреждение");
            return;
        }

        ExcelPackage.License.SetNonCommercialPersonal("Bonfire");
        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add("Семена");

        ApplyCommonSettings(sheet);
        SetSeedsHeader(sheet);
        FillSeedsSheet(list, sheet);
        AutoFitSeedsColumns(sheet);
        SaveReport(package, fileName);
    }

    public void CreateSeedlingsReport(IEnumerable<SeedlingReportItem> items, string fileName = "Рассада")
    {
        var list = items.ToList();
        if (list.Count == 0)
        {
            userDialog.Warning("Нет данных для формирования отчёта", "Предупреждение");
            return;
        }

        ExcelPackage.License.SetNonCommercialPersonal("Bonfire");
        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add("Рассада");

        ApplyCommonSettings(sheet);
        SetSeedlingsHeader(sheet);
        FillSeedlingsSheet(list, sheet);
        AutoFitSeedlingsColumns(sheet);
        SaveReport(package, fileName);
    }

    private static void FillSeedsSheet(IReadOnlyList<SeedReportItem> list, ExcelWorksheet sheet)
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
            sheet.Cells[row, 3].Value = list[i].ExpirationDate.Split('.')[2];

            var yearDiff = DateTime.Now.Year - DateTime.Parse(list[i].ExpirationDate).Year;
            if (yearDiff > 0)
                sheet.Cells[row, 3].Style.Font.Bold = true;
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
        range.Style.Border.Top.Style    = ExcelBorderStyle.Thin;
        range.Style.Border.Left.Style   = ExcelBorderStyle.Thin;
        range.Style.Border.Right.Style  = ExcelBorderStyle.Thin;
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
        sheet.Cells[1, 4].Value = "Граммы";
        sheet.Cells[1, 5].Value = "Штуки";
        sheet.Cells[1, 1, 1, 7].Style.Font.Bold = true;
    }

    private static void SetSeedlingsHeader(ExcelWorksheet sheet)
    {
        sheet.Cells[1, 1].Value = "Сорт";
        sheet.Cells[1, 2].Value = "Производитель";
        sheet.Cells[1, 3].Value = "Дата посева";
        sheet.Cells[1, 4].Value = "г.";
        sheet.Cells[1, 5].Value = "шт.";
        sheet.Cells[1, 6].Value = "Взошло";
        sheet.Cells[1, 7].Value = "Остаток";
        sheet.Cells[1, 1, 1, 7].Style.Font.Bold = true;
    }

    private static void ApplyCommonSettings(ExcelWorksheet sheet)
    {
        sheet.PrinterSettings.BottomMargin = 0.1;
        sheet.PrinterSettings.FooterMargin = 0;
        sheet.PrinterSettings.HeaderMargin = 0;
        sheet.PrinterSettings.LeftMargin   = 0.2;
        sheet.PrinterSettings.RightMargin  = 0.1;
        sheet.PrinterSettings.TopMargin    = 0.1;
        sheet.PrinterSettings.RepeatRows   = sheet.Cells["1:1"];

        sheet.Cells.Style.Font.Name = "Calibri";
        sheet.Cells.Style.Font.Size = 12;

        sheet.Columns[1, 2].Style.WrapText = true;
        sheet.Cells["A:B"].Style.HorizontalAlignment  = ExcelHorizontalAlignment.Left;
        sheet.Cells["C:G"].Style.HorizontalAlignment  = ExcelHorizontalAlignment.Center;
        sheet.Cells["A1:G1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        sheet.Cells["A:G"].Style.VerticalAlignment    = ExcelVerticalAlignment.Center;
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

    private void SaveReport(ExcelPackage package, string fileName)
    {
        try
        {
            var bytes = package.GetAsByteArray() ?? throw new InvalidOperationException("package.GetAsByteArray()");
            var path = $"{fileName}_{DateTime.Now.ToShortDateString()}.xlsx";

            if (!Directory.Exists("Reports"))
                Directory.CreateDirectory("Reports");

            File.WriteAllBytes($"Reports\\{path}", bytes);
            userDialog.Information($"Отчет {path} сформирован", "Информация");
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            userDialog.Warning("Что-то пошло не так...", "Предупреждение");
        }
    }
}
