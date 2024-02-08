using Bonfire.Services.Interfaces;
using Bonfire.ViewModels.Base;
using BonfireDB.Entities;
using System.Threading.Tasks;
using System.Windows.Input;
using Bonfire.Infrastructure.Commands;
using Bonfire.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Bonfire.ViewModels
{
    public class SeedlingsViewModel : ViewModel
    {
        private readonly ISeedlingsService _seedlingsService;
        private readonly IUserDialog _dialog;

        public SeedlingsViewModel(ISeedlingsService seedlings, IUserDialog dialog)
        {
            _seedlingsService = seedlings;
            _dialog = dialog;
            _SeedlingsView = new CollectionViewSource
            {
                SortDescriptions =
                {
                    new SortDescription(nameof(SeedlingsFromViewModel.Culture), ListSortDirection.Ascending),
                    new SortDescription(nameof(SeedlingsFromViewModel.Sort), ListSortDirection.Ascending),
                    new SortDescription(nameof(SeedlingsFromViewModel.Producer), ListSortDirection.Ascending)

                },
                GroupDescriptions =
                {
                    new PropertyGroupDescription(nameof(SeedlingsFromViewModel.Sort))
                }

                

            };
            _SeedlingsView.Filter += _SeedsViewSource_Filter;

            
        }

        #region Свойства

        #region FilterSeeds - Фильтрация по культуре

        public ICollectionView SeedlingsView => _SeedlingsView?.View;
        private readonly CollectionViewSource _SeedlingsView;

        #region SeedlingFilter : string - Искомое слово

        /// <summary>Искомое слово</summary>
        private string _SeedlingFilter = "-Выбрать все-";

        /// <summary>Искомое слово</summary>
        public string SeedlingFilter
        {
            get => _SeedlingFilter;
            set
            {
                if (Set(ref _SeedlingFilter, value))
                {
                    SeedlingsView.Refresh();
                }
            }
        }

        #endregion
        private void _SeedsViewSource_Filter(object sender, FilterEventArgs e)
        {
            if (!(e.Item is SeedlingsFromViewModel seedling) || string.IsNullOrEmpty(SeedlingFilter) || SeedlingFilter == "-Выбрать все-") return;
            if (!seedling.Culture.Contains(SeedlingFilter, StringComparison.OrdinalIgnoreCase))
                e.Accepted = false;
        }

        #endregion

        #region Seeds : ObservableCollection<Seedling> - Коллекция рассады

        /// <summary>Коллекция рассады</summary>
        private ObservableCollection<Seedling> _Seedlings;

        /// <summary>Коллекция рассады</summary>
        public ObservableCollection<Seedling> Seedlings
        {
            get => _Seedlings;
            set => Set(ref _Seedlings, value);
        }
        #endregion

        #region SelectedSeedlingsViewItem : SeedlingsFromViewModel - Выбранный объект SeedlingsView

        /// <summary>Выбранный объект SeedlingsView</summary>
        private SeedlingsFromViewModel _SelectedSeedlingsViewItem;

        /// <summary>Выбранный объект SeedlingsView</summary>
        public SeedlingsFromViewModel SelectedSeedlingsViewItem
        {
            get => _SelectedSeedlingsViewItem;
            set
            {
                Set(ref _SelectedSeedlingsViewItem, value);
                SelectedItem = value != null ? Seedlings.First(s => s.Id == value.Id) : null;

            }
        }

        #endregion

        #region SelectedItem : Seedling - Выбранный объект

        /// <summary>Выбранный объект</summary>
        private Seedling _SelectedItem;


        /// <summary>Выбранный объект</summary>
        public Seedling SelectedItem
        {
            get => _SelectedItem;
            set => Set(ref _SelectedItem, value);
        }

        #endregion

        #region EditedItem : Seedling - Редактируемый объект

        /// <summary>Редактируемый объект</summary>
        private Seedling _EditedItem = new()
        {
            Plant = new Plant()

            {
                PlantCulture = new PlantCulture(),
                PlantSort = new PlantSort()
                {
                    Producer = new Producer()
                }

            },

            SeedlingInfo = new SeedlingInfo()

        };


        /// <summary>Редактируемый объект</summary>
        public Seedling EditedItem
        {
            get => _EditedItem;
            set => Set(ref _EditedItem, value);
        }

        #endregion

        #region ListCulture : List<string> - Список культур

        /// <summary>Список культур</summary>
        private List<string> _ListCulture = new List<string> { "-Выбрать все-" };

        /// <summary>Список культур</summary>
        public List<string> ListCulture
        {
            get => _ListCulture;
            set => Set(ref _ListCulture, value);
        }


        #endregion



        #endregion


        #region Методы

        #region Метод загрузки рассады
        private async Task LoadSeedling()
        {
            var seedlingsQuery = _seedlingsService.Seedlings
                    .Select(seedlings => new SeedlingsFromViewModel
                    {
                        Id = seedlings.Id,
                        Culture = seedlings.Plant.PlantCulture.Name,
                        Sort = seedlings.Plant.PlantSort.Name,
                        Producer = seedlings.Plant.PlantSort.Producer.Name,
                        Amount = seedlings.SeedlingInfo.SeedlingNumber,
                        GerminationData = seedlings.SeedlingInfo.GerminationDate,
                        IsQuarantine = seedlings.SeedlingInfo.QuarantineStartDate != null && seedlings.SeedlingInfo.QuarantineStopDate == null,
                        LandingData = seedlings.SeedlingInfo.LandingDate,
                        QuenchingDate = seedlings.SeedlingInfo.QuenchingDate

                    })
                    .OrderBy(c => c.Culture)
                    .ThenBy(s => s.Sort)
                    .ThenBy(p => p.Producer)
                ;

            Seedlings = new ObservableCollection<Seedling>(await _seedlingsService.Seedlings.ToArrayAsync());
            _SeedlingsView.Source = await seedlingsQuery.ToArrayAsync();

            OnPropertyChanged(nameof(SeedlingsView));

        }

        #endregion

        #region Метод загрузки списка культур

        private void LoadListCulture()
        {
            var listCultureQuery = _seedlingsService.Seedlings
                .Select(seedlings => seedlings.Plant.PlantCulture.Name)
                .Distinct()
                .OrderBy(s => s);
            ListCulture.AddRange(listCultureQuery.ToListAsync().Result);
           

        }

        #endregion

        #endregion


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
            if (Seedlings != null) return;
            await LoadSeedling();
            LoadListCulture();

        }
        #endregion

        #region Command SeedlingsChoiceClassCommand - Команда для выбора растений по классам

        /// <summary> Команда для выбора растений по классам </summary>
        private ICommand _SeedlingsChoiceClassCommand;

        /// <summary> Команда для выбора растений по классам </summary>
        public ICommand SeedlingsChoiceClassCommand => _SeedlingsChoiceClassCommand
            ??= new LambdaCommandAsync(OnSeedlingsChoiceClassCommandExecuted, CanSeedlingsChoiceClassCommandExecute);

        /// <summary> Проверка возможности выполнения - Команда для выбора растений по классам </summary>
        private bool CanSeedlingsChoiceClassCommandExecute(object p) => true;

        /// <summary> Логика выполнения - Команда для выбора растений по классам </summary>
        private async Task OnSeedlingsChoiceClassCommandExecuted(object p)
        {

            var seedlingsQuery = p.ToString() != "Выбрать все"
                    ? _seedlingsService.Seedlings

                    .Where(seedlings => seedlings.Plant.PlantCulture.Class == p.ToString())
                    .Select(seedlings => new SeedlingsFromViewModel
                    {
                        Id = seedlings.Id,
                        Culture = seedlings.Plant.PlantCulture.Name,
                        Sort = seedlings.Plant.PlantSort.Name,
                        Producer = seedlings.Plant.PlantSort.Producer.Name,
                        Amount = seedlings.SeedlingInfo.SeedlingNumber,
                        GerminationData = seedlings.SeedlingInfo.GerminationDate,
                        IsQuarantine = seedlings.SeedlingInfo.QuarantineStartDate != null && seedlings.SeedlingInfo.QuarantineStopDate == null,
                        LandingData = seedlings.SeedlingInfo.LandingDate,
                        QuenchingDate = seedlings.SeedlingInfo.QuenchingDate

                    })
                    .OrderBy(c => c.Culture)
                    .ThenBy(s => s.Sort)
                    .ThenBy(p => p.Producer)

                    : _seedlingsService.Seedlings
                    .Select(seedlings => new SeedlingsFromViewModel
                    {
                        Id = seedlings.Id,
                        Culture = seedlings.Plant.PlantCulture.Name,
                        Sort = seedlings.Plant.PlantSort.Name,
                        Producer = seedlings.Plant.PlantSort.Producer.Name,
                        Amount = seedlings.SeedlingInfo.SeedlingNumber,
                        GerminationData = seedlings.SeedlingInfo.GerminationDate,
                        IsQuarantine = seedlings.SeedlingInfo.QuarantineStartDate != null && seedlings.SeedlingInfo.QuarantineStopDate == null,
                        LandingData = seedlings.SeedlingInfo.LandingDate,
                        QuenchingDate = seedlings.SeedlingInfo.QuenchingDate

                    })
                    .OrderBy(c => c.Culture)
                    .ThenBy(s => s.Sort)
                    .ThenBy(p => p.Producer)

                ;
            _SeedlingsView.Source = await seedlingsQuery.ToArrayAsync();
            OnPropertyChanged(nameof(SeedlingsView));
        }

        #endregion

        #endregion


    }
}
