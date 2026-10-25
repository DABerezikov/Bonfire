using System;
using System.Runtime.CompilerServices;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Seeds;
using Bonfire.ViewModels.Base;

namespace Bonfire.Models;

public sealed class SeedPackageEditModel : ViewModel
{
    public SeedPackageValues Values { get; private set; }
    public bool HasErrors => SeedPackage.Create(Values).IsFailure;

    public SeedPackageEditModel(SeedPackageValues values)
    {
        ArgumentNullException.ThrowIfNull(values);
        Values = values;
    }

    public double WeightPack
    {
        get => Values.WeightPack;
        set => Update(Values with { WeightPack = value });
    }

    public double QuantityPack
    {
        get => Values.QuantityPack;
        set => Update(Values with { QuantityPack = value });
    }

    public double AmountSeeds
    {
        get => Values.AmountSeeds;
        set => Update(Values with { AmountSeeds = value });
    }

    public double? AmountSeedsWeight
    {
        get => ToNullable(Values.AmountSeedsWeight);
        set => Update(Values with { AmountSeedsWeight = ToOption(value) });
    }

    public decimal CostPack
    {
        get => Values.CostPack;
        set => Update(Values with { CostPack = value });
    }

    public DateTime PurchaseDate
    {
        get => Values.PurchaseDate;
        set => Update(Values with { PurchaseDate = value });
    }

    public DateTime ExpirationDate
    {
        get => Values.ExpirationDate;
        set => Update(Values with { ExpirationDate = value });
    }

    public string? Note
    {
        get => ToNullable(Values.Note);
        set => Update(Values with { Note = ToOption(value) });
    }

    public string? SeedSource
    {
        get => ToNullable(Values.SeedSource);
        set => Update(Values with { SeedSource = ToOption(value) });
    }

    public string? DisposeComment
    {
        get => ToNullable(Values.DisposeComment);
        set => Update(Values with { DisposeComment = ToOption(value) });
    }

    private void Update(SeedPackageValues values, [CallerMemberName] string? property = null)
    {
        if (Values == values) return;
        Values = values;
        OnPropertyChanged(property);
        OnPropertyChanged(nameof(Values));
        OnPropertyChanged(nameof(HasErrors));
    }

    private static Option<T> ToOption<T>(T? value) where T : struct
        => value is { } present ? Option.Some(present) : Option.None<T>();
    private static Option<string> ToOption(string? value)
        => value is { } present ? Option.Some(present) : Option.None<string>();
    private static T? ToNullable<T>(Option<T> value) where T : struct => value.Match<T?>(present => present, () => null);
    private static string? ToNullable(Option<string> value) => value.Match<string?>(present => present, () => null);
}
