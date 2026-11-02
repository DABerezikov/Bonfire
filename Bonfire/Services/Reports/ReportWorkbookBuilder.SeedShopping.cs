using System.Collections.Immutable;
using System.Drawing;
using System.Globalization;
using Bonfire.Application.Planning;
using Bonfire.Domain.Functional;
using OfficeOpenXml.Style;

namespace Bonfire.Services.Reports;

internal sealed partial class ReportWorkbookBuilder
{
    public Result<ImmutableArray<byte>> BuildSeedShopping(SeedShoppingListFromViewModel shopping)
    {
        if (shopping.Items.Length == 0) return Result.Failure<ImmutableArray<byte>>("В плане нет дефицита семян для покупки");
        if (shopping.Items.Length > 1048570) return Result.Failure<ImmutableArray<byte>>("Список покупки превышает допустимое число строк Excel");
        return Build("Покупка семян", sheet =>
        {
            sheet.Cells[1, 1, 1, 8].Merge = true;
            sheet.Cells[1, 1].Value = "Покупка семян — " + shopping.PlanName;
            sheet.Cells[1, 1].Style.Font.Bold = true; sheet.Cells[1, 1].Style.Font.Size = 16;
            sheet.Cells[2, 1, 2, 8].Merge = true;
            sheet.Cells[2, 1].Value = $"Сезон {shopping.SeasonYear}; версия плана {shopping.PlanVersion}; сформирован {shopping.CreatedAt.UtcDateTime:dd.MM.yyyy HH:mm} UTC";
            sheet.Cells[3, 1, 3, 8].Merge = true;
            sheet.Cells[3, 1].Value = "По сохранённым партиям. Остаток учтён один раз на сорт. Вес не переводится в штуки: такие строки требуют уточнения.";
            sheet.Cells[3, 1].Style.WrapText = true; sheet.Row(3).Height = 34;
            string[] headers = ["Культура", "Сорт", "Нужно, шт.", "Известный остаток, шт.", "Известный остаток, г", "Купить, шт.", "Первый посев", "Действие"];
            for (var column = 0; column < headers.Length; column++) sheet.Cells[5, column + 1].Value = headers[column];
            sheet.Cells[5, 1, 5, 8].Style.Font.Bold = true;
            sheet.Cells[5, 1, 5, 8].Style.Fill.PatternType = ExcelFillStyle.Solid;
            sheet.Cells[5, 1, 5, 8].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(225, 238, 220));
            var row = 6;
            foreach (var item in shopping.Items)
            {
                sheet.Cells[row, 1].Value = item.Culture; sheet.Cells[row, 2].Value = item.Sort;
                sheet.Cells[row, 3].Value = item.Deficit.RequiredSeeds;
                sheet.Cells[row, 4].Value = item.Deficit.KnownPieces;
                sheet.Cells[row, 5].Value = item.Deficit.KnownGrams.Match<object?>(value => value, () => null);
                sheet.Cells[row, 6].Value = item.Deficit.BuySeeds.Match<object?>(value => value, () => null);
                sheet.Cells[row, 7].Value = item.FirstSowing.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
                sheet.Cells[row, 8].Value = item.Deficit.NeedsStockReview ? "Уточнить остаток в штуках" : "Купить";
                if (item.Deficit.NeedsStockReview)
                {
                    sheet.Cells[row, 1, row, 8].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    sheet.Cells[row, 1, row, 8].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 244, 204));
                }
                row++;
            }
            sheet.Cells[5, 1, row - 1, 8].AutoFilter = true; sheet.View.FreezePanes(6, 1);
            sheet.PrinterSettings.RepeatRows = sheet.Cells["5:5"];
            sheet.Cells[5, 1, row - 1, 8].Style.WrapText = true;
            sheet.Columns[1, 2].Width = 25; sheet.Columns[3, 6].Width = 18; sheet.Columns[7].Width = 16; sheet.Columns[8].Width = 30;
            ApplyBorders(sheet, row - 1, 8, 5);
        });
    }
}
