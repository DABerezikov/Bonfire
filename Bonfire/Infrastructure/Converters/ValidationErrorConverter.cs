using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;

namespace Bonfire.Infrastructure.Converters;

public sealed class ValidationErrorConverter : IMultiValueConverter
{
    public object? Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
        => (values.FirstOrDefault() as IEnumerable<ValidationError>)?.FirstOrDefault()?.ErrorContent;

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}