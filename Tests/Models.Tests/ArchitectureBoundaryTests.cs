using System.Reflection;
using Bonfire.Domain.Functional;
using Bonfire.Application.Seeds;
using Bonfire.Models.Mappers;

namespace Models.Tests;

public class ArchitectureBoundaryTests
{
    [Fact]
    public void EditorOptionsContract_ExpressesReadFailure()
    {
        var result = typeof(Bonfire.Services.Interfaces.ISeedsService).GetMethod("GetEditorOptionsAsync")!.ReturnType.GetGenericArguments()[0];
        Assert.True(result.IsGenericType && result.GetGenericTypeDefinition() == typeof(Result<>));
    }

    [Theory]
    [InlineData("Bonfire.Domain")]
    [InlineData("Bonfire.Application")]
    public void CoreAssemblies_DoNotReferencePersistencePresentationOrContainer(string name)
    {
        var assembly = name == "Bonfire.Domain" ? typeof(Option<>).Assembly : typeof(ISeedReader).Assembly;
        Assert.DoesNotContain(assembly.GetReferencedAssemblies(), dependency => dependency.Name is { } value
            && (value.StartsWith("BonfireDB") || value.StartsWith("Microsoft.EntityFrameworkCore")
                || value.StartsWith("Presentation") || value.StartsWith("Microsoft.Extensions.DependencyInjection") || value == "Bonfire"));
    }

    [Fact]
    public void PresentationMappers_DoNotAcceptOrReturnPersistenceEntities()
    {
        var types = typeof(SeedlingEditMapper).Assembly.GetTypes().Where(type => type.Namespace == "Bonfire.Models.Mappers");
        foreach (var type in types)
        foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
        foreach (var boundary in method.GetParameters().Select(p => p.ParameterType).Append(method.ReturnType).SelectMany(Expand))
            Assert.False(boundary.Assembly.GetName().Name == "BonfireDB", $"{type.Name}.{method.Name}: {boundary}");
    }

    private static IEnumerable<Type> Expand(Type type)
        => new[] { type }.Concat(type.GetGenericArguments().SelectMany(Expand));
}
