using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Bonfire.Application.Planning;
using Bonfire.Application.Planning.Libraries;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Planning;
using Bonfire.Domain.Planning.GrowingProfiles;
using Bonfire.Domain.Planning.Regions;
using Bonfire.Infrastructure.Commands;
using Bonfire.Models.Mappers;
using Bonfire.Models.Planning;
using Bonfire.Services.Interfaces;
using Bonfire.ViewModels.Base;

namespace Bonfire.ViewModels;

public sealed class PlanningLibrariesViewModel(IPlanningService service) : ViewModel
{
    private PlanningContextFromViewModel? _context;
    private long _contextRevision;
    private bool _preserveRegionEditor;

    public string[] TimeZoneIds { get; } =
        TimeZoneInfo.GetSystemTimeZones().Select(x => x.Id).OrderBy(x => x).ToArray();

    public ObservableCollection<RegionFromViewModel> Regions { get; } = [];
    public ObservableCollection<PlanningChoiceFromViewModel> Choices { get; } = [];
    public ObservableCollection<RegionalWindowItem> Windows { get; } = [];

    public string Status
    {
        get;
        private set => Set(ref field, value);
    } = "";

    public bool ContextReady
    {
        get;
        private set => Set(ref field, value);
    }

    public bool IsBusy
    {
        get;
        private set => Set(ref field, value);
    }

    public RegionFromViewModel? SelectedRegion
    {
        get;
        set
        {
            if (!Set(ref field, value)) return;
            if (value is not null && !_preserveRegionEditor) RegionEditor = PlanningRegionEditorDraft.From(value);
            _ = LoadContextAsync();
        }
    }

    public PlanningChoiceFromViewModel? SelectedChoice
    {
        get;
        set
        {
            if (Set(ref field, value)) _ = LoadContextAsync();
        }
    }

    public PlanningRegionEditorDraft RegionEditor
    {
        get;
        private set => Set(ref field, value);
    } = new(Option.None<int>(), new PlanningRegionEditorValues());

    public GrowingProfileFields CultureFields
    {
        get;
        private set => Set(ref field, value);
    } = new(new GrowingProfileFieldValues(), true, true);

    public GrowingProfileFields SortFields
    {
        get;
        private set => Set(ref field, value);
    } = new(new GrowingProfileFieldValues(), false, true);

    public GrowingLocation WindowLocation
    {
        get;
        set => Set(ref field, value);
    }

    public PlantPlacement WindowPlacement
    {
        get;
        set => Set(ref field, value);
    }

    public RegionalWindowPurpose WindowPurpose
    {
        get;
        set => Set(ref field, value);
    }

    public string WindowStart
    {
        get;
        set => Set(ref field, value);
    } = "";

    public string WindowEnd
    {
        get;
        set => Set(ref field, value);
    } = "";

    public bool WindowNextYear
    {
        get;
        set => Set(ref field, value);
    }

    public string? WindowSource
    {
        get;
        set => Set(ref field, value);
    }

    public string? WindowNote
    {
        get;
        set => Set(ref field, value);
    }

    public RegionalWindowItem? SelectedWindow
    {
        get;
        set
        {
            if (!Set(ref field, value) || value is null) return;
            var reference = value.Reference;
            WindowLocation = reference.Conditions.Location;
            WindowPlacement = reference.Conditions.Placement;
            WindowPurpose = reference.Purpose;
            WindowStart = $"{reference.Window.StartDay:00}.{reference.Window.StartMonth:00}";
            WindowEnd = $"{reference.Window.EndDay:00}.{reference.Window.EndMonth:00}";
            WindowNextYear = reference.Window.EndInNextYear;
            WindowSource = reference.Source.ToNullable();
            WindowNote = reference.Note.ToNullable();
        }
    }

    public ICommand LoadCommand => field ??= new LambdaCommandAsync(LoadAsync, () => !IsBusy);

    public ICommand NewRegionCommand => field ??= new LambdaCommand(() =>
    {
        RegionEditor = new PlanningRegionEditorDraft(Option.None<int>(), new PlanningRegionEditorValues());
        Status = "Новый регион";
    });

    public ICommand SaveRegionCommand => field ??= new LambdaCommandAsync(SaveRegionAsync, () => !IsBusy);

    public ICommand SaveCultureCommand =>
        field ??= new LambdaCommandAsync(SaveCultureAsync, () => ContextReady && !IsBusy);

    public ICommand SaveSortCommand => field ??= new LambdaCommandAsync(SaveSortAsync, () => ContextReady && !IsBusy);

    public ICommand SaveWindowCommand =>
        field ??= new LambdaCommandAsync(SaveWindowAsync, () => ContextReady && !IsBusy);

    public ICommand ReloadContextCommand => field ??= new LambdaCommandAsync(() => LoadContextAsync(true),
        () => SelectedRegion is not null && SelectedChoice is not null);

    public async Task LoadAsync()
    {
        var regions = await service.ReadRegionsAsync(true);
        regions.Match(items =>
        {
            var selected = SelectedRegion?.Id;
            Regions.Clear();
            foreach (var region in items) Regions.Add(region);
            if (SelectedRegion is null)
                SelectedRegion = Regions.FirstOrDefault(x => x.Id == selected) ?? Regions.FirstOrDefault();
            return 0;
        }, Error);
        var choices = await service.ReadChoicesAsync();
        choices.Match(items =>
        {
            Choices.Clear();
            foreach (var choice in items) Choices.Add(choice);
            return 0;
        }, Error);
    }

    public async Task LoadContextAsync(bool force = false)
    {
        var revision = ++_contextRevision;
        var region = SelectedRegion;
        var choice = SelectedChoice;
        ContextReady = false;
        if (region is null || choice is null) return;
        var result = await service.ReadContextAsync(region.Id.Value, choice.CultureId, choice.SortId);
        if (revision != _contextRevision) return;
        result.Match(context =>
        {
            var samePair = _context is { } previous && previous.Culture.Id == choice.CultureId &&
                           previous.SortId == choice.SortId;
            if (force || !samePair)
            {
                CultureFields = new GrowingProfileFields(
                    context.CultureProfile.Match(x => PlanningMapper.Fields(x.Parameters, x.Extras),
                        () => new GrowingProfileFieldValues()), true, true);
                SortFields = new GrowingProfileFields(
                    context.SortSettings.Match(x => PlanningMapper.Fields(new GrowingProfileParameters(), x.Extras),
                        () => new GrowingProfileFieldValues()), false, true);
            }

            _context = context;
            ContextReady = true;
            Windows.Clear();
            foreach (var window in context.RegionalWindows)
                Windows.Add(new RegionalWindowItem(window, DateTime.Now.Year));
            Status = "Справка загружена. Изменения сохраняются отдельно для каждой формы.";
            return 0;
        }, Error);
    }

    private async Task SaveRegionAsync()
    {
        var editor = RegionEditor;
        var submitted = editor.Values;
        var request = editor.BuildRequest();
        if (request.IsFailure)
        {
            Status = PlanningInputParser.Error(request);
            return;
        }

        IsBusy = true;
        try
        {
            var result = await service.SaveRegionAsync(request.ValueOrThrow());
            result.Match(saved =>
            {
                var existing = Regions.FirstOrDefault(x => x.Id == saved.Id);
                if (existing is not null) Regions[Regions.IndexOf(existing)] = saved;
                else Regions.Add(saved);
                if (ReferenceEquals(RegionEditor, editor))
                {
                    editor.AcceptSaved(submitted, saved.Id.Value);
                    _preserveRegionEditor = true;
                    try
                    {
                        SelectedRegion = saved;
                    }
                    finally
                    {
                        _preserveRegionEditor = false;
                    }
                }

                Status = "Регион сохранён";
                return 0;
            }, Error);
        }
        finally
        {
            IsBusy = false;
            CommandManager.InvalidateRequerySuggested();
        }
    }

    private async Task SaveCultureAsync()
    {
        var choice = SelectedChoice;
        var fields = CultureFields;
        if (!ContextReady || choice is null) return;
        var request = fields.BuildOverrides(false).Bind(ranges => fields.BuildExtras().Bind(extras =>
            Result.Success(new SaveCultureProfileFromViewModel(choice.CultureId,
                new GrowingProfileParameters
                {
                    Germination = ranges.Germination, SeedlingAge = ranges.SeedlingAge, Vegetation = ranges.Vegetation
                }, extras))));
        if (request.IsFailure)
        {
            Status = PlanningInputParser.Error(request);
            return;
        }

        IsBusy = true;
        try
        {
            (await service.SaveCultureProfileAsync(request.ValueOrThrow())).Match(_ =>
            {
                Status = "Агропрофиль культуры сохранён";
                return 0;
            }, Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SaveSortAsync()
    {
        var choice = SelectedChoice;
        if (!ContextReady || choice is null) return;
        var request = SortFields.BuildExtras()
            .Bind(extras => Result.Success(new SaveSortSettingsFromViewModel(choice.SortId, extras)));
        if (request.IsFailure)
        {
            Status = PlanningInputParser.Error(request);
            return;
        }

        IsBusy = true;
        try
        {
            (await service.SaveSortSettingsAsync(request.ValueOrThrow())).Match(_ =>
            {
                Status = "Дополнительные настройки сорта сохранены";
                return 0;
            }, Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SaveWindowAsync()
    {
        var region = SelectedRegion;
        var choice = SelectedChoice;
        if (!ContextReady || region is null || choice is null) return;
        var request = PlanningInputParser.Annual(WindowStart, WindowEnd, WindowNextYear).Bind(window => window.Match(
            value => Result.Success(new SaveRegionalWindowFromViewModel(region.Id.Value, choice.CultureId,
                WindowLocation, WindowPlacement, WindowPurpose,
                value, WindowSource.ToOption(), WindowNote.ToOption())),
            () => Result.Failure<SaveRegionalWindowFromViewModel>("Заполните обе границы справочного периода")));
        if (request.IsFailure)
        {
            Status = PlanningInputParser.Error(request);
            return;
        }

        IsBusy = true;
        try
        {
            var result = await service.SaveRegionalWindowAsync(request.ValueOrThrow());
            if (result.IsFailure)
            {
                Status = PlanningInputParser.Error(result);
                return;
            }

            await LoadContextAsync();
            Status = "Справочный интервал сохранён";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private int Error(string error)
    {
        Status = error;
        return 0;
    }
}