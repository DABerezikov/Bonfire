using Bonfire.Services.Interfaces;
using Bonfire.ViewModels.Base;
using BonfireDB.Entities;
using System.Threading.Tasks;
using System.Windows.Input;
using Bonfire.Infrastructure.Commands;

namespace Bonfire.ViewModels
{
    public class SeedlingsViewModel : ViewModel
    {
        private readonly ISeedlingsService _seedlings;
        private readonly IUserDialog _dialog;

        public SeedlingsViewModel(ISeedlingsService seedlings, IUserDialog dialog)
        {
            _seedlings = seedlings;
            _dialog = dialog;
        }

        #region Комманды

        #region Command LoadDataCommand - Команда для загрузки данных из репозитория

        /// <summary> Команда для загрузки данных из репозитория </summary>
        private ICommand _LoadDataCommand;

        /// <summary> Команда для загрузки данных из репозитория </summary>
        public ICommand LoadDataCommand => _LoadDataCommand
            ??= new LambdaCommandAsync(OnLoadDataCommandExecuted, CanLoadDataCommandExecute);

        /// <summary> Проверка возможности выполнения - Команда для загрузки данных из репозитория </summary>
        private bool CanLoadDataCommandExecute() => true;

        /// <summary> Логика выполнения - Команда для загрузки данных из репозитория </summary>
        private async Task OnLoadDataCommandExecuted()
        {

        }
        #endregion

        #endregion


    }
}
