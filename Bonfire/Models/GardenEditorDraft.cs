using System;
using System.Runtime.CompilerServices;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;
using Bonfire.ViewModels.Base;

namespace Bonfire.Models;

public sealed class GardenEditorDraft : ViewModel
{
    public int Id { get; }
    public GardenEditorValues Original { get; private set; }
    public GardenEditorValues Values { get; private set; }
    public bool IsDirty => Values != Original;
    public GardenEditorDraft(int id, GardenEditorValues values)
    {
        ArgumentNullException.ThrowIfNull(values);
        (Id, Original, Values) = (id, values, values);
    }

    public string Name { get => Values.Name; set => Update(Values with { Name = value }); }
    public string? Note { get => Values.Note.ToNullable(); set => Update(Values with { Note = value.ToOption() }); }
    public string? Address { get => Values.Address.ToNullable(); set => Update(Values with { Address = value.ToOption() }); }
    public double WidthMeters { get => Values.WidthMeters; set => Update(Values with { WidthMeters = value }); }
    public double HeightMeters { get => Values.HeightMeters; set => Update(Values with { HeightMeters = value }); }

    public void Accept(GardenEditorValues saved, GardenEditorValues submitted)
    {
        ArgumentNullException.ThrowIfNull(saved);
        Values = saved.WithChangesFrom(submitted, Values);
        Original = saved;
        OnPropertyChanged(string.Empty);
    }

    private void Update(GardenEditorValues values, [CallerMemberName] string? property = null)
    {
        if (Values == values) return;
        Values = values;
        OnPropertyChanged(property);
        OnPropertyChanged(nameof(Values));
        OnPropertyChanged(nameof(IsDirty));
    }
}
