﻿using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Bonfire.Infrastructure.Commands;
using Bonfire.Services.Interfaces;
using Bonfire.ViewModels.Base;


namespace Bonfire.ViewModels
{
    internal class MainWindowViewModel : ViewModel
    {
        private readonly IUserDialog _UserDialog;
        private readonly ISeedsService _SeedsService;
        private readonly ISeedlingsService _SeedlingsService;
        private readonly SeedsViewModel _SeedsViewModel;
        private readonly SeedlingsViewModel _SeedlingsViewModel;

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



        #region SeedsBold : FontWeight - Жирный шрифт выбранного окна

        /// <summary>Окно семян</summary>
        private FontWeight _SeedsBold;

        /// <summary>Окно семян</summary>
        public FontWeight SeedsBold { get => _SeedsBold; set => Set(ref _SeedsBold, value); }

        /// <summary>Окно рассады</summary>
        private FontWeight _SeedlingsBold;

        /// <summary>Окно рассады</summary>
        public FontWeight SeedlingsBold { get => _SeedlingsBold; set => Set(ref _SeedlingsBold, value); }

        /// <summary>Окно редактора</summary>
        private FontWeight _LibraryBold;

        /// <summary>Окно редактора</summary>
        public FontWeight LibraryBold { get => _LibraryBold; set => Set(ref _LibraryBold, value); }





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
            if (CurrentViewModel is not SeedsViewModel)
                CurrentViewModel = _SeedsViewModel;
            ClearBold();
            SeedsBold = FontWeights.Bold;




        }

        private void ClearBold()
        {
            SeedsBold = default;
            SeedlingsBold = default;
            LibraryBold = default;
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
            if (CurrentViewModel is not LibraryEditorViewModel)
            {
                var sort = _SeedsViewModel.AddSortList;
                var culture = _SeedsViewModel.AddCultureList;
                var producer = _SeedsViewModel.AddProducerList;
                var seeds = _SeedsViewModel.Seeds;
                CurrentViewModel = new LibraryEditorViewModel(_SeedsService, _UserDialog, sort, culture, producer, seeds);
                ClearBold();
                LibraryBold = FontWeights.Bold;
            }
            

        }

        #endregion

        #region Command ShowSeedlingsViewModelCommand - Отобразить представление редактора библиотек

        /// <summary> Отобразить представление редактора библиотек </summary>
        private ICommand _ShowSeedlingsViewModelCommand;

        /// <summary> Отобразить представление редактора библиотек </summary>
        public ICommand ShowSeedlingsViewModelCommand => _ShowSeedlingsViewModelCommand
            ??= new LambdaCommand(OnShowSeedlingsViewModelCommandExecuted, CanShowSeedlingsViewModelCommandExecute);

        /// <summary> Проверка возможности выполнения - Отобразить представление редактора библиотек </summary>
        private bool CanShowSeedlingsViewModelCommandExecute() => true;

        /// <summary> Логика выполнения - Отобразить представление редактора библиотек </summary>
        private void OnShowSeedlingsViewModelCommandExecuted()
        {
            if (CurrentViewModel is not SeedlingsViewModel)
            {
               
                CurrentViewModel = new SeedlingsViewModel(_SeedlingsService, _SeedsService, _UserDialog);
                ClearBold();
                SeedlingsBold = FontWeights.Bold;
            }


        }

        #endregion

        #region Command CreateSeedsReportCommand - Команда для формирования отчета по семенам

        /// <summary> Команда для формирования отчета по семенам </summary>
        private ICommand _CreateSeedsReportCommand;

        /// <summary> Команда для формирования отчета по семенам </summary>
        public ICommand CreateSeedsReportCommand => _CreateSeedsReportCommand
            ??= new LambdaCommandAsync(OnCreateSeedsReportCommandExecuted, CanCreateSeedsReportCommandExecute);

       

        /// <summary> Проверка возможности выполнения - Команда для формирования отчета по семенам </summary>
        private bool CanCreateSeedsReportCommandExecute() => CurrentViewModel == _SeedsViewModel;

        /// <summary> Логика выполнения - Команда для формирования отчета по семенам </summary>
        private async Task OnCreateSeedsReportCommandExecuted()
        {
           _SeedsViewModel.CreateSeedReport();
        }



        #endregion



        public MainWindowViewModel( IUserDialog UserDialog,
                                    ISeedsService SeedsService,
                                    ISeedlingsService SeedlingsService,
                                    SeedsViewModel SeedsViewModel,
                                    SeedlingsViewModel SeedlingsViewModel)
        {
            _UserDialog = UserDialog;
            _SeedsService = SeedsService;
            _SeedlingsService = SeedlingsService;
            _SeedsViewModel = SeedsViewModel;
            _SeedlingsViewModel = SeedlingsViewModel;
        }
    }
}
