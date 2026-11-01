using System;
using System.Runtime.CompilerServices;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;
using Bonfire.ViewModels.Base;

namespace Bonfire.Models;

public sealed class ElementEditorDraft : ViewModel
{
    public ElementEditorDraft(int id, ElementEditorValues values)
    {
        ArgumentNullException.ThrowIfNull(values);
        (Id, Original, Values) = (id, values, values);
    }

    public int Id { get; }
    public ElementEditorValues Original { get; private set; }
    public ElementEditorValues Values { get; private set; }
    public bool IsDirty => Values != Original;

    public string Name
    {
        get => Values.Details.Name;
        set => Update(Values with { Details = Values.Details with { Name = value } });
    }

    public string? Note
    {
        get => Values.Details.Note.ToNullable();
        set => Update(Values with { Details = Values.Details with { Note = value.ToOption() } });
    }

    public string? SoilType
    {
        get => Values.Details.SoilType.ToNullable();
        set => Update(Values with { Details = Values.Details with { SoilType = value.ToOption() } });
    }

    public string? Orientation
    {
        get => Values.Details.Orientation.ToNullable();
        set => Update(Values with { Details = Values.Details with { Orientation = value.ToOption() } });
    }

    public string? CoverMaterial
    {
        get => Values.Details.CoverMaterial.ToNullable();
        set => Update(Values with { Details = Values.Details with { CoverMaterial = value.ToOption() } });
    }

    public double X
    {
        get => Values.X;
        set => Update(Values with { X = value });
    }

    public double Y
    {
        get => Values.Y;
        set => Update(Values with { Y = value });
    }

    public double Width
    {
        get => Values.Width;
        set => Update(Values with { Width = value });
    }

    public double Height
    {
        get => Values.Height;
        set => Update(Values with { Height = value });
    }

    public void Accept(ElementEditorValues saved, ElementEditorValues submitted)
    {
        Values = saved.WithChangesFrom(submitted, Values);
        Original = saved;
        OnPropertyChanged(string.Empty);
    }

    private void Update(ElementEditorValues values, [CallerMemberName] string? property = null)
    {
        if (Values == values) return;
        Values = values;
        OnPropertyChanged(property);
        OnPropertyChanged(nameof(Values));
        OnPropertyChanged(nameof(IsDirty));
    }
}