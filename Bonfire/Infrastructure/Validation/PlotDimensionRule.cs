using System.Globalization;
using System.Windows.Controls;
using Bonfire.Domain.Garden;

namespace Bonfire.Infrastructure.Validation;

public sealed class PlotDimensionRule : ValidationRule
{
    public override ValidationResult Validate(object? value, CultureInfo cultureInfo)
        => double.TryParse(value?.ToString(), NumberStyles.Any, cultureInfo, out var meters)
            ? PlotDimensions
                .Create(meters, 0, 1)
                .Match(
                    _ => ValidationResult.ValidResult,
                error => new ValidationResult(false, error))
            : new ValidationResult(false, "Введите число");
}