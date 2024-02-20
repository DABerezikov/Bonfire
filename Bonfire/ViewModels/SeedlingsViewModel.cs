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
using Bonfire.Services;

namespace Bonfire.ViewModels
{
    public class SeedlingsViewModel : ViewModel
    {
        private readonly ISeedlingsService _seedlingsService;
        private readonly ISeedsService _seedsService;
        private readonly IUserDialog _dialog;

        public SeedlingsViewModel(ISeedlingsService seedlings, ISeedsService seedsService, IUserDialog dialog)
        {
            _seedlingsService = seedlings;
            _seedsService = seedsService;
            _dialog = dialog;
            _SeedlingsView = new CollectionViewSource
            {
                SortDescriptions =
                {
                    new SortDescription(nameof(ConcreteSeedlingFromViewModel.Culture), ListSortDirection.Ascending),
                    new SortDescription(nameof(ConcreteSeedlingFromViewModel.Sort), ListSortDirection.Ascending),
                    new SortDescription(nameof(ConcreteSeedlingFromViewModel.Producer), ListSortDirection.Ascending)

                },
                GroupDescriptions =
                {
                    new PropertyGroupDescription(nameof(ConcreteSeedlingFromViewModel.Sort))
                }
                
                

            };
            _SeedlingsView.Filter += _SeedsViewSource_Filter;

            _PlantListView = new CollectionViewSource
            {
                SortDescriptions =
                {
                    new SortDescription(nameof(PlantFromViewModel.Culture), ListSortDirection.Ascending),
                    new SortDescription(nameof(PlantFromViewModel.Sort), ListSortDirection.Ascending),
                    new SortDescription(nameof(PlantFromViewModel.Producer), ListSortDirection.Ascending)

                }
            };

            _PlantListView.Filter += _PlantListView_Filter;

            _CultureListView = new CollectionViewSource
            {
                SortDescriptions =
                {
                    new SortDescription(nameof(CultureFromViewModel.Name), ListSortDirection.Ascending)
                }
            };

            _CultureListView.Filter += _CultureListView_Filter;

            _SortListView = new CollectionViewSource
            {
                SortDescriptions =
                {
                    new SortDescription(nameof(SortFromSeedlingsViewModel.Sort), ListSortDirection.Ascending)
                }
            
            };

            _SortListView.Filter += _SortListView_Filter;

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
            if (!(e.Item is ConcreteSeedlingFromViewModel seedling) || string.IsNullOrEmpty(SeedlingFilter) || SeedlingFilter == "-Выбрать все-") return;
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

        #region SelectedConcreteSeedlingViewItem : ConcreteSeedlingFromViewModel - Выбранный объект SeedlingsView

        /// <summary>Выбранный объект SeedlingsView</summary>
        private ConcreteSeedlingFromViewModel _SelectedConcreteSeedlingViewItem;

        /// <summary>Выбранный объект SeedlingsView</summary>
        public ConcreteSeedlingFromViewModel SelectedConcreteSeedlingViewItem
        {
            get => _SelectedConcreteSeedlingViewItem;
            set
            {
                Set(ref _SelectedConcreteSeedlingViewItem, value);
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

            SeedlingInfos = new List<SeedlingInfo>()

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

        #region Логика кнопок выбора источника рассады

        #region SeedlingSource : string - Результат выбора источника рассады

        /// <summary>Результат выбора источника рассады</summary>
        private string _SeedlingSource;

        /// <summary>Результат выбора источника рассады</summary>
        private string SeedlingSource
        {
            get => _SeedlingSource;
            set => Set(ref _SeedlingSource, value);
        }

        #endregion

        #region IsSold : bool - Выбор способа  получения рассады - куплено

        /// <summary>Выбор способа  получения рассады - куплено</summary>
        private bool _IsSold;

        /// <summary>Выбор способа  получения рассады - куплено</summary>
        public bool IsSold
        {
            get => _IsSold;
            set
            {
                if (Set(ref _IsSold, value))
                    SeedlingSource = "Куплено";

            }
        }

        #endregion

        #region IsDonated : bool - Выбор способа  получения рассады - подарено

        /// <summary>Выбор способа  получения рассады - подарено</summary>
        private bool _IsDonated;

        /// <summary>Выбор способа  получения рассады - подарено</summary>
        public bool IsDonated
        {
            get => _IsDonated;
            set
            {
                if (Set(ref _IsDonated, value))
                    SeedlingSource = "Подарено";

            }
        }

        #endregion

        #region IsSeeds : bool - Выбор способа  получения рассады - из семян

        /// <summary>Выбор способа  получения рассады - из семян</summary>
        private bool _IsSeeds;

        /// <summary>Выбор способа  получения рассады - из семян</summary>
        public bool IsSeeds
        {
            get => _IsSeeds;
            set
            {
                if (Set(ref _IsSeeds, value))
                    SeedlingSource = "Из семян";

            }
        }

        #endregion

        #endregion

        #region Выбор единиц измерения для добавления рассады

        #region AddSizeList : List<string> - Список единиц измерения

        /// <summary>Список единиц измерения</summary>
        private List<string> _AddSizeList = new() { "Граммы", "Штуки" };

        /// <summary>Список единиц измерения</summary>
        public List<string> AddSizeList
        {
            get => _AddSizeList;
            set => Set(ref _AddSizeList, value);
        }

        #endregion

        #region AddSize : string - Выбранная единица измерения для добавления рассады

        /// <summary>Выбранная единица измерения для добавления рассады</summary>
        private string _AddSize;

        /// <summary>Выбранная единица измерения для добавления рассады</summary>
        public string AddSize
        {
            get => _AddSize;
            set => Set(ref _AddSize, value);
        }

        #endregion


        #endregion

        #region Выбор растения для добавления семян

        public ICollectionView PlantListView => _PlantListView?.View;
        private readonly CollectionViewSource _PlantListView;


        private void _PlantListView_Filter(object sender, FilterEventArgs e)
        {
            if (!(e.Item is PlantFromViewModel plant ) || (string.IsNullOrEmpty(AddCulture) && string.IsNullOrEmpty(AddSort))) return;
            if (!string.IsNullOrEmpty(AddSort))
            {
                if (string.IsNullOrEmpty(AddCulture))
                {
                    if (!plant.Sort.Equals(AddSort, StringComparison.OrdinalIgnoreCase))
                        e.Accepted = false;
                }
                else
                {
                  
                    if (!(plant.Culture.Equals(AddCulture, StringComparison.OrdinalIgnoreCase) &&
                          plant.Sort.Equals(AddSort, StringComparison.OrdinalIgnoreCase)))
                        e.Accepted = false;
                }
            }
            else 
            {
                if (!plant.Culture.Equals(AddCulture, StringComparison.OrdinalIgnoreCase))
                    e.Accepted = false;

            }
        }

        #region AddPlantList : List<string> - Список растений для добавления семян

        /// <summary>Список культур для добавления семян</summary>
        private ObservableCollection<PlantFromViewModel> _AddPlantList = new();

        /// <summary>Список культур для добавления семян</summary>
        public ObservableCollection<PlantFromViewModel> AddPlantList
        {
            get => _AddPlantList;
            set => Set(ref _AddPlantList, value);
        }

        #endregion

        #region AddProducer : string - Выбранный сорт для добавления семян

        /// <summary>Выбранный сорт для добавления семян</summary>
        private string _AddProducer;

        /// <summary>Выбранный сорт для добавления семян</summary>
        public string AddProducer
        {
            get => _AddProducer;
            set
            {
                if (Set(ref _AddProducer, value))
                    PlantListView.Refresh();
            }
        }

        #endregion

        #endregion

        #region Выбор культуры для добавления семян

        public ICollectionView CultureListView => _CultureListView?.View;
        private readonly CollectionViewSource _CultureListView;


        private void _CultureListView_Filter(object sender, FilterEventArgs e)
        {
            if (!(e.Item is CultureFromViewModel culture) || string.IsNullOrEmpty(AddCulture)) return;

            if (!culture.Name.Contains(AddCulture, StringComparison.OrdinalIgnoreCase))
                e.Accepted = false;
        }

        #region AddCultureList : List<string> - Список культур для добавления семян

        /// <summary>Список культур для добавления семян</summary>
        private ObservableCollection<CultureFromViewModel> _AddCultureList = new();

        /// <summary>Список культур для добавления семян</summary>
        public ObservableCollection<CultureFromViewModel> AddCultureList
        {
            get => _AddCultureList;
            set => Set(ref _AddCultureList, value);
        }

        #endregion

        #region AddCulture : string - Выбранная культура для добавления семян

        /// <summary>Выбранная культура для добавления семян</summary>
        private string _AddCulture;

        /// <summary>Выбранная культура для добавления семян</summary>
        public string AddCulture
        {
            get => _AddCulture;
            set
            {
                if (!Set(ref _AddCulture, value)) return;
                CultureListView.Refresh();
                SortListView.Refresh();
                PlantListView.Refresh();
            }
        }

        #endregion

        #endregion

        #region Выбор сорта для добавления семян

        public ICollectionView SortListView => _SortListView?.View;
        private readonly CollectionViewSource _SortListView;


        private void _SortListView_Filter(object sender, FilterEventArgs e)
        {
            if (!(e.Item is SortFromSeedlingsViewModel sort) ||
                (string.IsNullOrEmpty(AddSort) && string.IsNullOrEmpty(AddCulture))) return;
            if (string.IsNullOrEmpty(AddCulture)) return;
            if (!string.IsNullOrEmpty(AddSort))
            {
                if (!(sort.Culture.Contains(AddCulture, StringComparison.OrdinalIgnoreCase) && sort.Sort.Contains(AddSort, StringComparison.OrdinalIgnoreCase)))
                    e.Accepted = false;
            }
            else
            {
                if (!sort.Culture.Contains(AddCulture, StringComparison.OrdinalIgnoreCase))
                    e.Accepted = false;

            }
           
            

            //if (!sort.Sort.Contains(AddSort, StringComparison.OrdinalIgnoreCase))
            //    e.Accepted = false;

            

        }

        #region AddSortList : List<string> - Список культур для добавления семян

        /// <summary>Список сортов для добавления семян</summary>
        private ObservableCollection<SortFromSeedlingsViewModel> _AddSortList = new();

        /// <summary>Список сортов для добавления семян</summary>
        public ObservableCollection<SortFromSeedlingsViewModel> AddSortList
        {
            get => _AddSortList;
            set => Set(ref _AddSortList, value);
        }

        #endregion

        #region AddSort : string - Выбранный сорт для добавления семян

        /// <summary>Выбранный сорт для добавления семян</summary>
        private string _AddSort;

        /// <summary>Выбранный сорт для добавления семян</summary>
        public string AddSort
        {
            get => _AddSort;
            set
            {
                if (!Set(ref _AddSort, value)) return;
                SortListView.Refresh();
                PlantListView.Refresh();
            }
        }

        #endregion

       

        #endregion

       


        #endregion


        #region Методы

        #region Метод загрузки рассады
        private async Task LoadSeedling()
        {
            var seedlingsQuery = _seedlingsService.Seedlings
                    .Select(seedlings => new Seedling()
                    {
                        Id = seedlings.Id,
                        Plant = seedlings.Plant,
                        Wight = seedlings.Wight,
                        Quantity = seedlings.Quantity,
                        SeedlingInfos = seedlings.SeedlingInfos
                        

                    })
                    .OrderBy(c => c.Plant.PlantCulture.Name)
                    .ThenBy(s => s.Plant.PlantSort.Name)
                    .ThenBy(p => p.Plant.PlantSort.Producer.Name)
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
            var addListCulture = _seedsService.Seeds
                .Select(seeds => new CultureFromViewModel
                {
                    Id = seeds.Plant.PlantCulture.Id,
                    Name = seeds.Plant.PlantCulture.Name
                }).AsEnumerable()
                .Distinct(s => s.Name)
                .OrderBy(s => s.Name);
            AddCultureList.AddRange(addListCulture.ToList());
            _CultureListView.Source = AddCultureList;
            OnPropertyChanged(nameof(CultureListView));

        }

        #endregion

        #region Метод загрузки списка растений

        private void LoadListPlant()
        {

            var addListPlant = _seedsService.Seeds
                .Select(seeds => new PlantFromViewModel
                {
                    Id = seeds.Plant.Id,
                    Culture = seeds.Plant.PlantCulture.Name,
                    Sort = seeds.Plant.PlantSort.Name,
                    Producer = seeds.Plant.PlantSort.Producer.Name,
                    ExpirationDate = seeds.SeedsInfo.ExpirationDate
                }).AsEnumerable()
                .OrderBy(s=>s.Culture);

            AddPlantList.AddRange(addListPlant.ToList());
            _PlantListView.Source = AddPlantList;
            OnPropertyChanged(nameof(PlantListView));
           
        }

        #endregion
        
        #region Метод загрузки списка сортов

        private void LoadListSort()
        {

            var addListSort = _seedsService.Seeds
                .Select(seeds => new SortFromSeedlingsViewModel
                {
                    Id = seeds.Plant.PlantSort.Id,
                    Sort = seeds.Plant.PlantSort.Name,
                    Culture = seeds.Plant.PlantCulture.Name
                }).AsEnumerable()
                .Distinct(s=>s.Sort)
                .OrderBy(s => s.Sort);

            AddSortList.AddRange(addListSort.ToList());
            _SortListView.Source = AddSortList;
            OnPropertyChanged(nameof(SortListView));

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
            LoadListPlant();
            LoadListSort();
            AddProducer = null;
            AddSort = null;


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
                    .Select(seedlings => new Seedling()
                    {
                        Id = seedlings.Id,
                        Plant = seedlings.Plant,
                        Wight = seedlings.Wight,
                        Quantity = seedlings.Quantity,
                        SeedlingInfos = seedlings.SeedlingInfos


                    })
                    .OrderBy(c => c.Plant.PlantCulture.Name)
                    .ThenBy(s => s.Plant.PlantSort.Name)
                    .ThenBy(p => p.Plant.PlantSort.Producer.Name)

                    : _seedlingsService.Seedlings
                        .Select(seedlings => new Seedling()
                        {
                            Id = seedlings.Id,
                            Plant = seedlings.Plant,
                            Wight = seedlings.Wight,
                            Quantity = seedlings.Quantity,
                            SeedlingInfos = seedlings.SeedlingInfos


                        })
                        .OrderBy(c => c.Plant.PlantCulture.Name)
                        .ThenBy(s => s.Plant.PlantSort.Name)
                        .ThenBy(p => p.Plant.PlantSort.Producer.Name)

                ;
            _SeedlingsView.Source = await seedlingsQuery.ToArrayAsync();
            OnPropertyChanged(nameof(SeedlingsView));
        }

        #endregion

        #endregion


    }
}
