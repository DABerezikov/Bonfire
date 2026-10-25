using System;
using System.Runtime.CompilerServices;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;
using Bonfire.ViewModels.Base;

namespace Bonfire.Models;

public sealed class GreenhouseEditorDraft : ViewModel
{
    public int Id { get; }
    public GreenhouseEditorValues Original { get; private set; }
    public GreenhouseEditorValues Values { get; private set; }
    public bool IsDirty => Values != Original;
    public GreenhouseEditorDraft(int id, GreenhouseEditorValues values)
    {
        ArgumentNullException.ThrowIfNull(values);
        (Id, Original, Values) = (id, values, values);
    }

    public string Name { get => Values.Name; set => Update(Values with { Name = value }); }
    public string? Note { get => Values.Note.ToNullable(); set => Update(Values with { Note = value.ToOption() }); }
    public string? Material { get => Values.Material.ToNullable(); set => Update(Values with { Material = value.ToOption() }); }
    public double WidthMeters { get => Values.WidthMeters; set => Update(Values with { WidthMeters = value }); }
    public double HeightMeters { get => Values.HeightMeters; set => Update(Values with { HeightMeters = value }); }

    public void Accept(GreenhouseEditorValues saved, GreenhouseEditorValues submitted)
    {
        ArgumentNullException.ThrowIfNull(saved);
        Values = saved.WithChangesFrom(submitted, Values);
        Original = saved;
        OnPropertyChanged(string.Empty);
    }

    private void Update(GreenhouseEditorValues values, [CallerMemberName] string? property = null)
    {
        if (Values == values) return;
        Values = values;
        OnPropertyChanged(property);
        OnPropertyChanged(nameof(Values));
        OnPropertyChanged(nameof(IsDirty));
    }
}
