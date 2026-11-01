using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using Bonfire.Domain.Functional;

namespace Bonfire.Services.Reports;

public interface IReportPathPolicy
{
    Result<string> Resolve(string fileName, DateTime date);
}

public interface IReportFileWriter
{
    Result<string> Save(string path, ImmutableArray<byte> content);
}

internal sealed class ReportPathPolicy : IReportPathPolicy
{
    public Result<string> Resolve(string fileName, DateTime date)
        => Result.Success(Path.Combine("Reports", $"{fileName}_{date.ToShortDateString()}.xlsx"));
}

internal sealed class ReportFileWriter : IReportFileWriter
{
    public Result<string> Save(string path, ImmutableArray<byte> content)
    {
        try
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            File.WriteAllBytes(path, content.AsSpan());
            return Result.Success(path);
        }
        catch (Exception error)
        {
            Debug.WriteLine(error);
            return Result.Failure<string>("Что-то пошло не так...");
        }
    }
}