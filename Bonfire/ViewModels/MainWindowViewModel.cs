using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Bonfire.Infrastructure.Commands;
using Bonfire.ViewModels.Base;

namespace Bonfire.ViewModels
{
    internal class MainWindowViewModel(
        SeedsViewModel seedsViewModel,
        SeedlingsViewModel seedlingsViewModel,
        LibraryEditorViewModel libraryEditorViewModel,
        GardenPlanViewModel gardenPlanViewModel,
        PlantingPlanningViewModel plantingPlanningViewModel,
        HarvestJournalViewModel harvestJournalViewModel,
        PerennialPlanningViewModel perennialPlanningViewModel)
        : ViewModel
    {
        public SeedsViewModel SeedsViewModel { get; } = seedsViewModel;
        public SeedlingsViewModel SeedlingsViewModel { get; } = seedlingsViewModel;
        public LibraryEditorViewModel LibraryEditorViewModel { get; } = libraryEditorViewModel;
        public GardenPlanViewModel GardenPlanViewModel { get; } = gardenPlanViewModel;
        public PlantingPlanningViewModel PlantingPlanningViewModel { get; } = plantingPlanningViewModel;
        public HarvestJournalViewModel HarvestJournalViewModel { get; } = harvestJournalViewModel;
        public PerennialPlanningViewModel PerennialPlanningViewModel { get; } = perennialPlanningViewModel;
        public FontWeight PerennialBold { get; set => Set(ref field, value); }
        public Brush? PerennialBackground { get; set => Set(ref field, value); }
        public FontWeight HarvestBold { get; set => Set(ref field, value); }
        public Brush? HarvestBackground { get; set => Set(ref field, value); }
        public FontWeight PlanningBold { get; set => Set(ref field, value); }
        public Brush? PlanningBackground { get; set => Set(ref field, value); }

        public string Title
        {
            get;
            set => Set(ref field, value);
        } = "Огородик";

        public string Status
        {
            get;
            set => Set(ref field, value);
        } = "Готов!";

        public ViewModel? CurrentViewModel
        {
            get;
            set => Set(ref field, value);
        }

        // Подсветка активного пункта меню
        private readonly FontWeight _boldFontWeight = FontWeights.Bold;
        private readonly Brush _backgroundBrash = Brushes.LightGray;
        private static readonly Brush TransparentBrush = new SolidColorBrush(Colors.Transparent);

        public FontWeight SeedsBold
        {
            get;
            set => Set(ref field, value);
        }

        public Brush? SeedBackground
        {
            get;
            set => Set(ref field, value);
        }

        public FontWeight SeedlingsBold
        {
            get;
            set => Set(ref field, value);
        }

        public Brush? SeedlingBackground
        {
            get;
            set => Set(ref field, value);
        }

        public FontWeight LibraryBold
        {
            get;
            set => Set(ref field, value);
        }

        public Brush? LibraryBackground
        {
            get;
            set => Set(ref field, value);
        }

        public FontWeight GardenBold
        {
            get;
            set => Set(ref field, value);
        }

        public Brush? GardenBackground
        {
            get;
            set => Set(ref field, value);
        }

        private void ClearBold()
        {
            SeedsBold = default;
            SeedBackground = TransparentBrush;
            SeedlingsBold = default;
            SeedlingBackground = TransparentBrush;
            LibraryBold = default;
            LibraryBackground = TransparentBrush;
            GardenBold = default;
            GardenBackground = TransparentBrush;
            PlanningBold = default;
            HarvestBold = default;
            PerennialBold = default;
            PerennialBackground = TransparentBrush;
            HarvestBackground = TransparentBrush;
            PlanningBackground = TransparentBrush;
        }

        // Команды навигации

        public ICommand ShowSeedViewModelCommand => field
            ??= new LambdaCommand(() =>
            {
                if (CurrentViewModel is SeedsViewModel) return;
                SeedsViewModel.IsActive = true;
                SeedlingsViewModel.IsActive = false;
                CurrentViewModel = SeedsViewModel;
                PlantingPlanningViewModel.IsActive = false;
                ClearBold();
                SeedsBold = _boldFontWeight;
                SeedBackground = _backgroundBrash;
            });

        public ICommand ShowSeedlingsViewModelCommand => field
            ??= new LambdaCommand(() =>
            {
                if (CurrentViewModel is SeedlingsViewModel) return;
                if (CurrentViewModel is SeedsViewModel && !SeedsViewModel.TryDiscardSeedChanges()) return;
                SeedsViewModel.IsActive = false;
                SeedlingsViewModel.IsActive = true;
                CurrentViewModel = SeedlingsViewModel;
                PlantingPlanningViewModel.IsActive = false;
                ClearBold();
                SeedlingsBold = _boldFontWeight;
                SeedlingBackground = _backgroundBrash;
            });

        public ICommand ShowLibraryEditorViewModelCommand => field
            ??= new LambdaCommand(() =>
            {
                if (CurrentViewModel is LibraryEditorViewModel) return;
                if (CurrentViewModel is SeedsViewModel && !SeedsViewModel.TryDiscardSeedChanges()) return;
                SeedsViewModel.IsActive = false;
                SeedlingsViewModel.IsActive = false;
                CurrentViewModel = LibraryEditorViewModel;
                PlantingPlanningViewModel.IsActive = false;
                ClearBold();
                LibraryBold = _boldFontWeight;
                LibraryBackground = _backgroundBrash;
            });

        public ICommand ShowGardenPlanViewModelCommand => field
            ??= new LambdaCommand(() =>
            {
                if (CurrentViewModel is GardenPlanViewModel) return;
                if (CurrentViewModel is SeedsViewModel && !SeedsViewModel.TryDiscardSeedChanges()) return;
                SeedsViewModel.IsActive = false;
                SeedlingsViewModel.IsActive = false;
                GardenPlanViewModel.IsActive = true;
                CurrentViewModel = GardenPlanViewModel;
                PlantingPlanningViewModel.IsActive = false;
                ClearBold();
                GardenBold = _boldFontWeight;
                GardenBackground = _backgroundBrash;
            });
        public ICommand ShowPlantingPlanningCommand => field ??= new LambdaCommand(() =>
        {
            if (CurrentViewModel is PlantingPlanningViewModel) return;
            if (CurrentViewModel is SeedsViewModel && !SeedsViewModel.TryDiscardSeedChanges()) return;
            SeedsViewModel.IsActive = false;
            SeedlingsViewModel.IsActive = false;
            PlantingPlanningViewModel.IsActive = true;
            CurrentViewModel = PlantingPlanningViewModel;
            ClearBold();
            PlanningBold = _boldFontWeight;
            PlanningBackground = _backgroundBrash;
        });
        public ICommand ShowHarvestJournalCommand => field ??= new LambdaCommand(() =>
        {
            if (CurrentViewModel is HarvestJournalViewModel) return;
            if (CurrentViewModel is SeedsViewModel && !SeedsViewModel.TryDiscardSeedChanges()) return;
            SeedsViewModel.IsActive = false; SeedlingsViewModel.IsActive = false;
            PlantingPlanningViewModel.IsActive = false; GardenPlanViewModel.IsActive = false;
            CurrentViewModel = HarvestJournalViewModel; ClearBold(); HarvestBold = _boldFontWeight; HarvestBackground = _backgroundBrash;
        });
        public ICommand ShowPerennialPlanningCommand => field ??= new LambdaCommand(() =>
        {
            if (CurrentViewModel is PerennialPlanningViewModel) return;
            if (CurrentViewModel is SeedsViewModel && !SeedsViewModel.TryDiscardSeedChanges()) return;
            SeedsViewModel.IsActive = false; SeedlingsViewModel.IsActive = false;
            PlantingPlanningViewModel.IsActive = false; GardenPlanViewModel.IsActive = false;
            CurrentViewModel = PerennialPlanningViewModel; ClearBold(); PerennialBold = _boldFontWeight; PerennialBackground = _backgroundBrash;
        });
    }
}
