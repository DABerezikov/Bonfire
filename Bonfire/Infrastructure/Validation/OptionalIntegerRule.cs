using System.Globalization;
using System.Windows.Controls;

namespace Bonfire.Infrastructure.Validation;

public sealed class OptionalIntegerRule : ValidationRule
{
    public override ValidationResult Validate(object? value, CultureInfo cultureInfo)
        => string.IsNullOrWhiteSpace(value as string)
           || int.TryParse(value as string, NumberStyles.Integer, cultureInfo, out _)
            ? ValidationResult.ValidResult
            : new ValidationResult(false, "Введите целое число");
}