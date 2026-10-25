using System.Reflection;
using Bonfire.Domain.Functional;
using Bonfire.Application.Seeds;
using Bonfire.Models.Mappers;

namespace Models.Tests;

public class ArchitectureBoundaryTests
{
    [Theory]
    [InlineData("Bonfire.ViewModels")]
    [InlineData("Bonfire.Models")]
    [InlineData("Bonfire.Services")]
    public void PresentationAndFacades_DoNotStoreOrExposePersistenceTypes(string prefix)
    {
        var types = typeof(SeedlingEditMapper).Assembly.GetTypes().Where(type => type.Namespace is { } name
            && (name == prefix || name.StartsWith(prefix + ".", StringComparison.Ordinal)));
        foreach (var type in types)
        foreach (var member in Members(type).SelectMany(Expand))
            Assert.False(member.Assembly.GetName().Name == "BonfireDB", $"{type.FullName}: {member}");
    }

    [Fact]
    public void ApplicationContracts_DoNotLeakQueryable()
    {
        foreach (var type in typeof(ISeedReader).Assembly.GetExportedTypes())
        foreach (var member in Members(type).SelectMany(Expand))
            Assert.False(member.IsGenericType && member.GetGenericTypeDefinition() == typeof(IQueryable<>), $"{type}: {member}");
    }

    [Fact]
    public void Option_Map_IsAnExtensionRatherThanVariantOverrides()
    {
        Assert.DoesNotContain(typeof(Option<>).GetMethods(), method => method.Name == "Map");
        var map = Assert.Single(typeof(OptionExtensions).GetMethods().Where(method => method.Name == "Map"));
        Assert.True(map.IsStatic); Assert.True(map.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false));
    }

    [Theory]
    [InlineData("Value")]
    [InlineData("Error")]
    public void Result_DoesNotExposeSilentDefaultOrNullableError(string property)
        => Assert.Null(typeof(Result<int>).GetProperty(property));

    [Fact]
    public void Placement_DoesNotExposeObsoleteNullableWrapper()
        => Assert.Null(typeof(Bonfire.Infrastructure.CollisionHelper).GetMethod("FindFreeSpot"));

    [Theory]
    [InlineData(typeof(Bonfire.Services.Interfaces.ISeedsService))]
    [InlineData(typeof(Bonfire.Services.Interfaces.ISeedlingsService))]
    [InlineData(typeof(Bonfire.Services.Interfaces.IGardenService))]
    public void InventoryServiceContracts_DoNotExposePersistence(Type type)
    {
        foreach (var method in type.GetMethods())
        foreach (var boundary in method.GetParameters().Select(p => p.ParameterType).Append(method.ReturnType).SelectMany(Expand))
            Assert.NotEqual("BonfireDB", boundary.Assembly.GetName().Name);
    }

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
        Assert.All(assembly.GetReferencedAssemblies(), dependency => Assert.True(dependency.Name is { } value
            && (value.StartsWith("System", StringComparison.Ordinal) && value != "System.Xaml"
                || name == "Bonfire.Application" && value == "Bonfire.Domain"), $"{name} → {dependency.Name}"));
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

    private static IEnumerable<Type> Members(Type type)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        return type.GetFields(flags).Select(field => field.FieldType)
            .Concat(type.GetProperties(flags).Select(property => property.PropertyType))
            .Concat(type.GetConstructors(flags).SelectMany(constructor => constructor.GetParameters().Select(parameter => parameter.ParameterType)))
            .Concat(type.GetMethods(flags).SelectMany(method => method.GetParameters().Select(parameter => parameter.ParameterType).Append(method.ReturnType)));
    }

    private static IEnumerable<Type> Expand(Type type)
        => new[] { type }.Concat(type.GetGenericArguments().SelectMany(Expand))
            .Concat(type.HasElementType ? Expand(type.GetElementType()!) : []);
}
