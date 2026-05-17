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
        LibraryEditorViewModel libraryEditorViewModel)
        : ViewModel
    {
        public SeedsViewModel SeedsViewModel { get; } = seedsViewModel;
        public SeedlingsViewModel SeedlingsViewModel { get; } = seedlingsViewModel;
        public LibraryEditorViewModel LibraryEditorViewModel { get; } = libraryEditorViewModel;

        private string _Title = "Огородик";
        public string Title { get => _Title; set => Set(ref _Title, value); }

        private string _Status = "Готов!";
        public string Status { get => _Status; set => Set(ref _Status, value); }

        private ViewModel _CurrentViewModel;
        public ViewModel CurrentViewModel
        {
            get => _CurrentViewModel;
            set => Set(ref _CurrentViewModel, value);
        }

        // Подсветка активного пункта меню
        private readonly FontWeight _BoldFontWeight = FontWeights.Bold;
        private readonly Brush _BackgroundBrash = Brushes.LightGray;
        private static readonly Brush _TransparentBrush = new SolidColorBrush(Colors.Transparent);

        private FontWeight _SeedsBold;
        private Brush _SeedBackground;
        public FontWeight SeedsBold { get => _SeedsBold; set => Set(ref _SeedsBold, value); }
        public Brush SeedBackground { get => _SeedBackground; set => Set(ref _SeedBackground, value); }

        private FontWeight _SeedlingsBold;
        private Brush _SeedlingBackground;
        public FontWeight SeedlingsBold { get => _SeedlingsBold; set => Set(ref _SeedlingsBold, value); }
        public Brush SeedlingBackground { get => _SeedlingBackground; set => Set(ref _SeedlingBackground, value); }

        private FontWeight _LibraryBold;
        private Brush _LibraryBackground;
        public FontWeight LibraryBold { get => _LibraryBold; set => Set(ref _LibraryBold, value); }
        public Brush LibraryBackground { get => _LibraryBackground; set => Set(ref _LibraryBackground, value); }

        private void ClearBold()
        {
            SeedsBold = default;
            SeedBackground = _TransparentBrush;
            SeedlingsBold = default;
            SeedlingBackground = _TransparentBrush;
            LibraryBold = default;
            LibraryBackground = _TransparentBrush;
        }

        // Команды навигации

        private ICommand _ShowSeedViewModelCommand;
        public ICommand ShowSeedViewModelCommand => _ShowSeedViewModelCommand
            ??= new LambdaCommand(() =>
            {
                if (CurrentViewModel is SeedsViewModel) return;
                SeedsViewModel.IsActive = true;
                SeedlingsViewModel.IsActive = false;
                CurrentViewModel = SeedsViewModel;
                ClearBold();
                SeedsBold = _BoldFontWeight;
                SeedBackground = _BackgroundBrash;
            });

        private ICommand _ShowSeedlingsViewModelCommand;
        public ICommand ShowSeedlingsViewModelCommand => _ShowSeedlingsViewModelCommand
            ??= new LambdaCommand(() =>
            {
                if (CurrentViewModel is SeedlingsViewModel) return;
                SeedsViewModel.IsActive = false;
                SeedlingsViewModel.IsActive = true;
                CurrentViewModel = SeedlingsViewModel;
                ClearBold();
                SeedlingsBold = _BoldFontWeight;
                SeedlingBackground = _BackgroundBrash;
            });

        private ICommand _ShowLibraryEditorViewModelCommand;
        public ICommand ShowLibraryEditorViewModelCommand => _ShowLibraryEditorViewModelCommand
            ??= new LambdaCommand(() =>
            {
                if (CurrentViewModel is LibraryEditorViewModel) return;
                SeedsViewModel.IsActive = false;
                SeedlingsViewModel.IsActive = false;
                CurrentViewModel = LibraryEditorViewModel;
                ClearBold();
                LibraryBold = _BoldFontWeight;
                LibraryBackground = _BackgroundBrash;
            });
    }
}
