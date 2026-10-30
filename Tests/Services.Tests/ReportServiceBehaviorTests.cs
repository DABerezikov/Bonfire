using Bonfire.Services.Reports;
using Bonfire.Domain.Functional;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using Bonfire.Services.Interfaces;
using OfficeOpenXml;

namespace Services.Tests;

[CollectionDefinition("Report filesystem", DisableParallelization = true)]
public class ReportFilesystemCollection;

[Collection("Report filesystem")]
public class ReportServiceBehaviorTests : IDisposable
{
    private readonly string _previousDirectory = Directory.GetCurrentDirectory();
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "BonfireReports-" + Guid.NewGuid().ToString("N"));
    private readonly CultureInfo _previousCulture = CultureInfo.CurrentCulture;
    private readonly IUserDialog _dialog = Substitute.For<IUserDialog>();

    public ReportServiceBehaviorTests()
    {
        Directory.CreateDirectory(_directory);
        Directory.SetCurrentDirectory(_directory);
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ru-RU");
        ExcelPackage.Configure(config => config.JsonConfigBasePath = _directory);
        ExcelPackage.License.SetNonCommercialPersonal("Bonfire tests");
    }

    public void Dispose()
    {
        Directory.SetCurrentDirectory(_previousDirectory);
        ExcelPackage.Configure(config => config.JsonConfigBasePath = _previousDirectory);
        CultureInfo.CurrentCulture = _previousCulture;
        Directory.Delete(_directory, true);
    }

    private ReportService CreateService() => new(_dialog, new ReportWorkbookBuilder(), new ReportPathPolicy(), new ReportFileWriter(), TimeProvider.System);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void EmptyReport_WarnsWithoutCreatingFiles(bool seeds)
    {
        var service = CreateService();
        if (seeds) service.CreateSeedsReport([]);
        else service.CreateSeedlingsReport([]);
        _dialog.Received(1).Warning("Нет данных для формирования отчёта", "Предупреждение");
        Assert.Empty(Directory.GetFileSystemEntries(_directory));
    }

    [Fact]
    public void SeedsReport_WritesGroupsAmountsAndExpirationStyles()
    {
        var year = DateTime.Now.Year;
        CreateService().CreateSeedsReport([
            new("Астра", "Старый", "Фирма", $"31.12.{year - 1}", 0.8, 0),
            new("Астра", "Текущий", "Фирма", $"31.12.{year}", 0, 20),
            new("Томат", "Свежий", "Партнёр", $"31.12.{year + 1}", 1.5, 10)
        ], "Запасы");

        using var package = OpenOnlyReport();
        var sheet = package.Workbook.Worksheets["Семена"];
        Assert.Equal("Сорт", sheet.Cells[1, 1].Text);
        Assert.Equal("Годен до", sheet.Cells[1, 3].Text);
        Assert.Equal("Астра", sheet.Cells[2, 1].Text);
        Assert.True(sheet.Cells[2, 1, 2, 7].Merge);
        Assert.Equal("Старый", sheet.Cells[3, 1].Text);
        Assert.Equal(0.8, sheet.Cells[3, 4].GetValue<double>());
        Assert.True(sheet.Cells[3, 3].Style.Font.Bold);
        Assert.Equal(20, sheet.Cells[4, 5].GetValue<int>());
        Assert.True(sheet.Cells[4, 3].Style.Font.Italic);
        Assert.True(sheet.Cells[4, 3].Style.Font.UnderLine);
        Assert.Equal("Томат", sheet.Cells[5, 1].Text);
        Assert.Equal("Партнёр", sheet.Cells[6, 2].Text);
        Assert.False(sheet.Cells[6, 3].Style.Font.Bold);
        Assert.Equal("Calibri", sheet.Cells[6, 1].Style.Font.Name);
        _dialog.Received(1).Information(Arg.Is<string>(s => s.Contains("Запасы_")), "Информация");
    }

    [Fact]
    public void SeedlingsReport_WritesCountsAndLeavesZeroValuesBlank()
    {
        CreateService().CreateSeedlingsReport([
            new("Томат", "Черри", "Фирма", "01.03.2026", 0, 10, 8, 6),
            new("Перец", "Красный", "Партнёр", "02.03.2026", 0.5, 0, 0, 0)
        ]);

        using var package = OpenOnlyReport();
        var sheet = package.Workbook.Worksheets["Рассада"];
        Assert.Equal("Дата посева", sheet.Cells[1, 3].Text);
        Assert.Equal("Черри", sheet.Cells[3, 1].Text);
        Assert.Equal("01.03.2026", sheet.Cells[3, 3].Text);
        Assert.Null(sheet.Cells[3, 4].Value);
        Assert.Equal(10, sheet.Cells[3, 5].GetValue<int>());
        Assert.Equal(8, sheet.Cells[3, 6].GetValue<int>());
        Assert.Equal(6, sheet.Cells[3, 7].GetValue<int>());
        Assert.Equal("Перец", sheet.Cells[4, 1].Text);
        Assert.Equal(0.5, sheet.Cells[5, 4].GetValue<double>());
        Assert.Null(sheet.Cells[5, 5].Value);
        Assert.Null(sheet.Cells[5, 6].Value);
        Assert.Null(sheet.Cells[5, 7].Value);
    }

    [Fact]
    public void Report_WriteFailure_WarnsInsteadOfReportingSuccess()
    {
        File.WriteAllText(Path.Combine(_directory, "Reports"), "Блокируем создание каталога");
        CreateService().CreateSeedlingsReport([
            new("Томат", "Черри", "Фирма", "01.03.2026", 0, 1, 1, 1)
        ]);
        _dialog.Received(1).Warning("Что-то пошло не так...", "Предупреждение");
        _dialog.DidNotReceive().Information(Arg.Any<string>(), Arg.Any<string>());
    }

    private ExcelPackage OpenOnlyReport()
        => new(new FileInfo(Assert.Single(Directory.GetFiles(Path.Combine(_directory, "Reports"), "*.xlsx"))));

    [Fact]
    public void SeedsReport_InvalidExpiration_ReportsReasonWithoutFileOrException()
    {
        var error = Record.Exception(() => CreateService().CreateSeedsReport([
            new("Томат", "Черри", "Фирма", "не дата", 1, 2)
        ]));
        Assert.Null(error);
        _dialog.Received(1).Warning(Arg.Is<string>(message => message.Contains("срок годности")), "Предупреждение");
        _dialog.DidNotReceive().Information(Arg.Any<string>(), Arg.Any<string>());
        Assert.Empty(Directory.GetFileSystemEntries(_directory));
    }

    [Fact]
    public void Workbook_InMemory_UsesProvidedYearWithoutWritingFiles()
    {
        var result = new ReportWorkbookBuilder().BuildSeeds([new("Томат", "Черри", "Фирма", "31.12.2040", 1, 2)], 2041);
        using var stream = new MemoryStream(result.ValueOrThrow().ToArray());
        using var workbook = new ExcelPackage(stream);
        var sheet = workbook.Workbook.Worksheets["Семена"];
        Assert.True(sheet.Cells[3, 3].Style.Font.Bold); Assert.Equal("2040", sheet.Cells[3, 3].Text);
        Assert.Equal("Черри", sheet.Cells[3, 1].Text); Assert.Empty(Directory.GetFileSystemEntries(_directory));
    }

    [Fact]
    public void SeedsReport_EarlyExpirationYear_PreservesFourDigits()
    {
        CreateService().CreateSeedsReport([new("Томат", "Черри", "Фирма", "01.01.0001", 1, 2)]);
        using var report = OpenOnlyReport();
        Assert.Equal("0001", report.Workbook.Worksheets["Семена"].Cells[3, 3].Text);
    }

    [Fact]
    public void Report_LocalClock_CoordinatesBuilderPathAndWriter()
    {
        var builder = Substitute.For<IReportWorkbookBuilder>(); var paths = Substitute.For<IReportPathPolicy>();
        var files = Substitute.For<IReportFileWriter>(); var clock = Substitute.For<TimeProvider>();
        clock.GetUtcNow().Returns(new DateTimeOffset(2026, 12, 31, 22, 0, 0, TimeSpan.Zero));
        clock.LocalTimeZone.Returns(TimeZoneInfo.CreateCustomTimeZone("Test", TimeSpan.FromHours(3), "Test", "Test"));
        var date = new DateTime(2027, 1, 1, 1, 0, 0); var path = Path.Combine(_directory, "Результат.xlsx");
        var bytes = ImmutableArray.Create<byte>(1, 2, 3);
        builder.BuildSeeds(Arg.Any<IReadOnlyList<SeedReportItem>>(), 2027).Returns(Result.Success(bytes));
        paths.Resolve("Семена", date).Returns(Result.Success(path)); files.Save(path, bytes).Returns(Result.Success(path));
        new ReportService(_dialog, builder, paths, files, clock).CreateSeedsReport([]);
        builder.Received(1).BuildSeeds(Arg.Any<IReadOnlyList<SeedReportItem>>(), 2027);
        files.Received(1).Save(path, bytes);
        _dialog.Received(1).Information("Отчет Результат.xlsx сформирован", "Информация");
        Assert.Empty(Directory.GetFileSystemEntries(_directory));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Report_BuilderOrPathRefuses_DoesNotWrite(bool builderRefuses)
    {
        var builder = Substitute.For<IReportWorkbookBuilder>(); var paths = Substitute.For<IReportPathPolicy>();
        var files = Substitute.For<IReportFileWriter>();
        builder.BuildSeedlings(Arg.Any<IReadOnlyList<SeedlingReportItem>>()).Returns(builderRefuses
            ? Result.Failure<ImmutableArray<byte>>("Отказ книги") : Result.Success(ImmutableArray.Create<byte>(1)));
        paths.Resolve(Arg.Any<string>(), Arg.Any<DateTime>()).Returns(Result.Failure<string>("Отказ пути"));
        new ReportService(_dialog, builder, paths, files, TimeProvider.System).CreateSeedlingsReport([]);
        files.DidNotReceive().Save(Arg.Any<string>(), Arg.Any<ImmutableArray<byte>>());
        _dialog.Received(1).Warning(builderRefuses ? "Отказ книги" : "Отказ пути", "Предупреждение");
        _dialog.DidNotReceive().Information(Arg.Any<string>(), Arg.Any<string>());
    }
}
