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
        private readonly SeedsViewModel _SeedsViewModel;

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
            }
            

        }

        #endregion


        public MainWindowViewModel(IUserDialog UserDialog, ISeedsService SeedsService, SeedsViewModel SeedsViewModel)
        {
            _UserDialog = UserDialog;
            _SeedsService = SeedsService;
            _SeedsViewModel = SeedsViewModel;
        }
    }
}
