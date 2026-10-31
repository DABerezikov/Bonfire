using System.Globalization;
using System.Windows.Controls;
using Bonfire.Infrastructure.Converters;

namespace Models.Tests;

public class ValidationErrorConverterTests
{
    [Fact]
    public void Convert_EmptyErrors_ReturnsNull()
        => Assert.Null(new ValidationErrorConverter().Convert([Array.Empty<ValidationError>(), 0], typeof(object), null, CultureInfo.InvariantCulture));

    [Fact]
    public void Convert_PresentError_ReturnsMessage()
    {
        var error = new ValidationError(new ExceptionValidationRule(), new object(), "Исправьте значение", null);
        Assert.Equal("Исправьте значение", new ValidationErrorConverter().Convert([new[] { error }, 1], typeof(object), null, CultureInfo.InvariantCulture));
    }
}
