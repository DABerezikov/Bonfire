using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Seedlings;
using Bonfire.ViewModels.Base;

namespace Bonfire.Models;

public sealed class SeedlingEditorDraft(
    int id,
    SeedlingDetailsValues values,
    IEnumerable<SeedlingEntryEditModel> entries) : ViewModel
{
    public int Id { get; } = id;
    public SeedlingDetailsValues Values { get; private set; } = values;
    public ObservableCollection<SeedlingEntryEditModel> Entries { get; } = new(entries);

    public double Quantity
    {
        get => Values.Quantity;
        set => Update(Values with { Quantity = value });
    }

    public double Weight
    {
        get => Values.Weight;
        set => Update(Values with { Weight = value });
    }

    public DateTime? LandingDate
    {
        get => Values.LandingDate.ToNullable();
        set => Update(Values with { LandingDate = value.ToOption() });
    }

    public string? Source
    {
        get => Values.Source.ToNullable();
        set => Update(Values with { Source = value.ToOption() });
    }

    public string? Place
    {
        get => Values.Place.ToNullable();
        set => Update(Values with { Place = value.ToOption() });
    }

    public string? Note
    {
        get => Values.Note.ToNullable();
        set => Update(Values with { Note = value.ToOption() });
    }

    private void Update(SeedlingDetailsValues updated, [CallerMemberName] string? property = null)
    {
        if (Values == updated) return;
        Values = updated;
        OnPropertyChanged(property);
    }
}

public sealed class SeedlingEntryEditModel(SeedlingEntryEdit values) : ViewModel
{
    public SeedlingEntryEdit Values { get; private set; } = values;
    public int Id => Values.Id;

    public int Number
    {
        get => Values.Number;
        set => Update(Values with { Number = value });
    }

    public DateTime? GerminationDate
    {
        get => Values.GerminationDate.ToNullable();
        set => Update(Values with { GerminationDate = value.ToOption() });
    }

    public string? DeathNote
    {
        get => Values.DeathNote.ToNullable();
        set => Update(Values with { DeathNote = value.ToOption() });
    }

    private void Update(SeedlingEntryEdit updated, [CallerMemberName] string? property = null)
    {
        if (Values == updated) return;
        Values = updated;
        OnPropertyChanged(property);
    }
}