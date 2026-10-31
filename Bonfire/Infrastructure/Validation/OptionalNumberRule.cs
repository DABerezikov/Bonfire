using System.Globalization;
using System.Windows.Controls;

namespace Bonfire.Infrastructure.Validation;

public sealed class OptionalNumberRule : ValidationRule
{
    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    {
        var text = value as string;
        return string.IsNullOrWhiteSpace(text)
            || double.TryParse(text, NumberStyles.Float, cultureInfo, out _)
            ? ValidationResult.ValidResult
            : new ValidationResult(false, "Введите число, например 2,6");
    }
}
