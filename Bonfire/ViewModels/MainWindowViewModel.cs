using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using AutoMapper;
using Bonfire.Infrastructure.Commands;
using Bonfire.Services.Interfaces;
using Bonfire.ViewModels.Base;


namespace Bonfire.ViewModels
{
    internal class MainWindowViewModel(
        IUserDialog UserDialog,
        ISeedsService SeedsService,
        ISeedlingsService SeedlingsService,
        ISeedbedsService SeedbedsService,
        SeedsViewModel SeedsViewModel,
        SeedlingsViewModel SeedlingsViewModel,
        GardenViewModel GardenViewModel,
        IMapper Mapper)
        : ViewModel
    {
        private readonly SeedlingsViewModel _SeedlingsViewModel = SeedlingsViewModel;

        #region Title : string - Заголовок окна

        /// <summary>Заголовок окна</summary>
        private string _Title = "Огородик";

        /// <summary>Заголовок окна</summary>
        public string Title { get => _Title; set => Set(ref _Title, value); }

        #endregion

        #region Status : string - Статус

        /// <summary>Статус</summary>
        private string _Status = "Готов!";

        /// <summary>Статус</summary>
        public string Status { get => _Status; set => Set(ref _Status, value); }

        #endregion

        #region CurrentViewModel : ViewModel - Текущая модель представления

        /// <summary>Текущая модель представления</summary>
        private ViewModel _CurrentViewModel;

        /// <summary>Текущая модель представления</summary>
        public ViewModel CurrentViewModel
        {
            get => _CurrentViewModel;
            set => Set(ref _CurrentViewModel, value);
        }

        #endregion



        #region SeedsBold : FontWeight - Выделение выбранного окна

        private readonly FontWeight _BoldFontWeight = FontWeights.Bold;
        private readonly Brush _BackgroundBrash = Brushes.LightGray;

        /// <summary>Окно семян</summary>
        private FontWeight _SeedsBold;
        private Brush _SeedBackground;

        /// <summary>Окно семян</summary>
        public FontWeight SeedsBold { get => _SeedsBold; set => Set(ref _SeedsBold, value); }
        public Brush SeedBackground { get => _SeedBackground; set => Set(ref _SeedBackground, value); }

        /// <summary>Окно рассады</summary>
        private FontWeight _SeedlingsBold;
        private Brush _SeedlingBackground;
        /// <summary>Окно рассады</summary>
        public FontWeight SeedlingsBold { get => _SeedlingsBold; set => Set(ref _SeedlingsBold, value); }
        public Brush SeedlingBackground { get => _SeedlingBackground; set => Set(ref _SeedlingBackground, value); }

        /// <summary>Окно редактора</summary>
        private FontWeight _LibraryBold;
        private Brush _LibraryBackground;
        /// <summary>Окно редактора</summary>
        public FontWeight LibraryBold { get => _LibraryBold; set => Set(ref _LibraryBold, value); }
        public Brush LibraryBackground { get => _LibraryBackground; set => Set(ref _LibraryBackground, value); }
        
        /// <summary>Окно огорода</summary>
        private FontWeight _GardenBold;
        private Brush _GardenBackground;
        /// <summary>Окно огорода</summary>
        public FontWeight GardenBold { get => _GardenBold; set => Set(ref _GardenBold, value); }
        public Brush GardenBackground { get => _GardenBackground; set => Set(ref _GardenBackground, value); }





        #endregion

        #region Command ShowSeedViewModelCommand - Отобразить представление семян

        /// <summary> Отобразить представление семян </summary>
        private ICommand _ShowSeedViewModelCommand;

        /// <summary> Отобразить представление семян </summary>
        public ICommand ShowSeedViewModelCommand => _ShowSeedViewModelCommand
            ??= new LambdaCommand(OnShowSeedViewModelCommandExecuted, CanShowSeedViewModelCommandExecute);

        /// <summary> Проверка возможности выполнения - Отобразить представление семян </summary>
        private bool CanShowSeedViewModelCommandExecute() => true;

        /// <summary> Логика выполнения - Отобразить представление семян </summary>
        private void OnShowSeedViewModelCommandExecuted()
        {
            if (CurrentViewModel is SeedsViewModel) return;
            CurrentViewModel = SeedsViewModel;
            ClearBold();
            SeedsBold = _BoldFontWeight;
            SeedBackground = _BackgroundBrash;



        }

        private void ClearBold()
        {
            SeedsBold = default;
            SeedBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00FFFFFF"));
            SeedlingsBold = default;
            SeedlingBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00FFFFFF"));
            LibraryBold = default;
            LibraryBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00FFFFFF"));
            GardenBold = default;
            GardenBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00FFFFFF"));

        }

        #endregion

        #region Command ShowLibraryEditorViewModelCommand - Отобразить представление редактора библиотек

        /// <summary> Отобразить представление редактора библиотек </summary>
        private ICommand _ShowLibraryEditorViewModelCommand;

        /// <summary> Отобразить представление редактора библиотек </summary>
        public ICommand ShowLibraryEditorViewModelCommand => _ShowLibraryEditorViewModelCommand
            ??= new LambdaCommand(OnShowLibraryEditorViewModelCommandExecuted, CanShowLibraryEditorViewModelCommandExecute);

        /// <summary> Проверка возможности выполнения - Отобразить представление редактора библиотек </summary>
        private bool CanShowLibraryEditorViewModelCommandExecute() => true;

        /// <summary> Логика выполнения - Отобразить представление редактора библиотек </summary>
        private void OnShowLibraryEditorViewModelCommandExecuted()
        {
            if (CurrentViewModel is LibraryEditorViewModel) return;
            var sort = SeedsViewModel.AddSortList;
            var culture = SeedsViewModel.AddCultureList;
            var producer = SeedsViewModel.AddProducerList;
            var seeds = SeedsViewModel.Seeds;
            CurrentViewModel = new LibraryEditorViewModel(SeedsService, UserDialog, sort, culture, producer, seeds);
            ClearBold();
            LibraryBold = _BoldFontWeight;
            LibraryBackground = _BackgroundBrash;


        }

        #endregion

        #region Command ShowSeedlingsViewModelCommand - Отобразить представление рассады

        /// <summary> Отобразить представление рассады </summary>
        private ICommand _ShowSeedlingsViewModelCommand;

        /// <summary> Отобразить представление рассады </summary>
        public ICommand ShowSeedlingsViewModelCommand => _ShowSeedlingsViewModelCommand
            ??= new LambdaCommand(OnShowSeedlingsViewModelCommandExecuted, CanShowSeedlingsViewModelCommandExecute);

        /// <summary> Проверка возможности выполнения - Отобразить представление рассады </summary>
        private bool CanShowSeedlingsViewModelCommandExecute() => true;

        /// <summary> Логика выполнения - Отобразить представление рассады </summary>
        private void OnShowSeedlingsViewModelCommandExecuted()
        {
            if (CurrentViewModel is SeedlingsViewModel) return;
            CurrentViewModel = new SeedlingsViewModel(SeedlingsService, SeedsService, UserDialog, Mapper);
            ClearBold();
            SeedlingsBold = _BoldFontWeight;
            SeedlingBackground = _BackgroundBrash;


        }

        #endregion

        #region Command ShowGardenViewModelCommand - Отобразить представление рассады

        /// <summary> Отобразить представление рассады </summary>
        private ICommand _ShowGardenViewModelCommand;

        /// <summary> Отобразить представление рассады </summary>
        public ICommand ShowGardenViewModelCommand => _ShowGardenViewModelCommand
            ??= new LambdaCommand(OnShowGardenViewModelCommandExecuted, CanShowGardenViewModelCommandExecute);

        /// <summary> Проверка возможности выполнения - Отобразить представление рассады </summary>
        private bool CanShowGardenViewModelCommandExecute() => true;

        /// <summary> Логика выполнения - Отобразить представление рассады </summary>
        private void OnShowGardenViewModelCommandExecuted()
        {
            if (CurrentViewModel is GardenViewModel) return; 
            CurrentViewModel = new GardenViewModel(SeedlingsService, SeedsService, SeedbedsService, UserDialog, Mapper);
            ClearBold();
            GardenBold = _BoldFontWeight;
            GardenBackground = _BackgroundBrash;


        }

        #endregion


        #region Command CreateSeedsReportCommand - Команда для формирования отчета по семенам

        /// <summary> Команда для формирования отчета по семенам </summary>
        private ICommand _CreateSeedsReportCommand;

        /// <summary> Команда для формирования отчета по семенам </summary>
        public ICommand CreateSeedsReportCommand => _CreateSeedsReportCommand
            ??= new LambdaCommandAsync(OnCreateSeedsReportCommandExecuted, CanCreateSeedsReportCommandExecute);

       

        /// <summary> Проверка возможности выполнения - Команда для формирования отчета по семенам </summary>
        private bool CanCreateSeedsReportCommandExecute() => CurrentViewModel == SeedsViewModel;

        /// <summary> Логика выполнения - Команда для формирования отчета по семенам </summary>
        private async Task OnCreateSeedsReportCommandExecuted()
        {
           SeedsViewModel.CreateSeedReport();
        }



        #endregion
    }
}
