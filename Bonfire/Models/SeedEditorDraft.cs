using System;
using Bonfire.Domain.Seeds;
using Bonfire.ViewModels.Base;

namespace Bonfire.Models;

public sealed class SeedEditorDraft : ViewModel
{
    private readonly SeedPackageValues _originalPackage;

    public SeedEditorDraft(int id, string cultureName, string sortName, int producerId, SeedPackageValues package)
    {
        ArgumentNullException.ThrowIfNull(cultureName);
        ArgumentNullException.ThrowIfNull(sortName);
        ArgumentNullException.ThrowIfNull(package);
        (Id, CultureName, SortName, OriginalProducerId) = (id, cultureName, sortName, producerId);
        _originalPackage = package;
        Package = new SeedPackageEditModel(package);
        Package.PropertyChanged += (_, change) =>
        {
            if (change.PropertyName == nameof(SeedPackageEditModel.Values))
                OnPropertyChanged(nameof(HasPackageChanges));
        };
    }

    public int Id { get; }
    public string CultureName { get; }
    public string SortName { get; }
    public int OriginalProducerId { get; }
    public SeedPackageEditModel Package { get; }
    public bool HasPackageChanges => Package.Values != _originalPackage;
}