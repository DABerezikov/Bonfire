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
        GardenPlanViewModel gardenPlanViewModel)
        : ViewModel
    {
        public SeedsViewModel SeedsViewModel { get; } = seedsViewModel;
        public SeedlingsViewModel SeedlingsViewModel { get; } = seedlingsViewModel;
        public LibraryEditorViewModel LibraryEditorViewModel { get; } = libraryEditorViewModel;
        public GardenPlanViewModel GardenPlanViewModel { get; } = gardenPlanViewModel;

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
        }

        // Команды навигации

        public ICommand ShowSeedViewModelCommand => field
            ??= new LambdaCommand(() =>
            {
                if (CurrentViewModel is SeedsViewModel) return;
                SeedsViewModel.IsActive = true;
                SeedlingsViewModel.IsActive = false;
                CurrentViewModel = SeedsViewModel;
                ClearBold();
                SeedsBold = _boldFontWeight;
                SeedBackground = _backgroundBrash;
            });

        public ICommand ShowSeedlingsViewModelCommand => field
            ??= new LambdaCommand(() =>
            {
                if (CurrentViewModel is SeedlingsViewModel) return;
                SeedsViewModel.IsActive = false;
                SeedlingsViewModel.IsActive = true;
                CurrentViewModel = SeedlingsViewModel;
                ClearBold();
                SeedlingsBold = _boldFontWeight;
                SeedlingBackground = _backgroundBrash;
            });

        public ICommand ShowLibraryEditorViewModelCommand => field
            ??= new LambdaCommand(() =>
            {
                if (CurrentViewModel is LibraryEditorViewModel) return;
                SeedsViewModel.IsActive = false;
                SeedlingsViewModel.IsActive = false;
                CurrentViewModel = LibraryEditorViewModel;
                ClearBold();
                LibraryBold = _boldFontWeight;
                LibraryBackground = _backgroundBrash;
            });

        public ICommand ShowGardenPlanViewModelCommand => field
            ??= new LambdaCommand(() =>
            {
                if (CurrentViewModel is GardenPlanViewModel) return;
                SeedsViewModel.IsActive = false;
                SeedlingsViewModel.IsActive = false;
                GardenPlanViewModel.IsActive = true;
                CurrentViewModel = GardenPlanViewModel;
                ClearBold();
                GardenBold = _boldFontWeight;
                GardenBackground = _backgroundBrash;
            });
    }
}
