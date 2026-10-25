using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bonfire.Domain.Functional;
using Bonfire.Models.Mappers;
using BonfireDB.Entities;

namespace Models.Tests;

public class SeedEditorDraftTests
{
    private static Seed Seed() => new()
    {
        Id = 1, SeedsInfo = new() { AmountSeeds = 10 },
        Plant = new()
        {
            PlantCulture = new() { Name = "Томат" },
            PlantSort = new() { Id = 2, Name = " Черри ", Producer = new() { Id = 3 } }
        }
    };

    [Fact]
    public void Draft_PackageEditAndRestore_PreservesSourceAndDistinguishesOptionalValues()
    {
        var source = Seed();
        var draft = SeedEditMapper.FromSeed(source);
        Assert.False(draft.HasPackageChanges);
        draft.Package.AmountSeeds = 8;
        draft.Package.Note = "";
        draft.Package.AmountSeedsWeight = 0;
        Assert.True(draft.HasPackageChanges);
        Assert.Equal(10, source.SeedsInfo.AmountSeeds);
        Assert.Null(source.SeedsInfo.Note);
        Assert.Null(source.SeedsInfo.AmountSeedsWeight);
        Assert.Equal(0, Assert.IsType<Option<double>.Some>(draft.Package.Values.AmountSeedsWeight).Value);
        Assert.Equal("", Assert.IsType<Option<string>.Some>(draft.Package.Values.Note).Value);
        draft.Package.AmountSeeds = 10;
        draft.Package.Note = null;
        draft.Package.AmountSeedsWeight = null;
        Assert.False(draft.HasPackageChanges);
    }

    [Fact]
    public void Mapper_RequestSnapshot_DoesNotChangeWhenDraftOrSourceChanges()
    {
        var source = Seed();
        var draft = SeedEditMapper.FromSeed(source);
        var sort = LibraryMapper.ToEditModel(source.Plant.PlantSort);
        draft.Package.Note = "Сохранить";
        sort.PlantHeight = 30;
        var request = SeedEditMapper.ToRequest(draft, sort, 3).ValueOrThrow();
        draft.Package.Note = "Другой черновик";
        sort.PlantHeight = 40;
        source.Plant.PlantCulture.Name = "Астра";
        source.Plant.PlantSort.Name = "Другое";
        source.SeedsInfo.AmountSeeds = 99;
        Assert.Equal("Сохранить", Assert.IsType<Option<string>.Some>(request.Package.Note).Value);
        Assert.Equal(30, Assert.IsType<Option<int>.Some>(request.Characteristics.PlantHeight).Value);
        Assert.Equal(10, request.Package.AmountSeeds);
        Assert.Equal(" Черри ", request.SortName);
        Assert.Equal("Томат", draft.CultureName);
        Assert.Equal(" Черри ", draft.SortName);
    }

    [Fact]
    public void Package_ValueChange_NotifiesBindingsValidationAndDraftState()
    {
        var draft = SeedEditMapper.FromSeed(Seed());
        var packageChanges = new List<string?>();
        var draftChanges = new List<string?>();
        draft.Package.PropertyChanged += (_, change) => packageChanges.Add(change.PropertyName);
        draft.PropertyChanged += (_, change) => draftChanges.Add(change.PropertyName);
        draft.Package.AmountSeeds = -1;
        Assert.True(draft.Package.HasErrors);
        Assert.Contains(nameof(SeedPackageEditModel.AmountSeeds), packageChanges);
        Assert.Contains(nameof(SeedPackageEditModel.HasErrors), packageChanges);
        Assert.Contains(nameof(SeedEditorDraft.HasPackageChanges), draftChanges);
        draft.Package.AmountSeeds = 10;
        Assert.False(draft.Package.HasErrors);
        Assert.False(draft.HasPackageChanges);
    }

    [Fact]
    public void DraftModels_Members_DoNotReferenceEFTypes()
    {
        foreach (var type in new[] { typeof(SeedEditorDraft), typeof(SeedPackageEditModel), typeof(SortEditModel) })
        {
            var memberTypes = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Select(field => field.FieldType).Concat(type.GetProperties().Select(property => property.PropertyType));
            foreach (var member in memberTypes.SelectMany(Types))
                Assert.NotEqual(typeof(Seed).Assembly, member.Assembly);
        }
    }

    private static IEnumerable<Type> Types(Type type)
        => new[] { type }.Concat(type.GetGenericArguments().SelectMany(Types));
}
