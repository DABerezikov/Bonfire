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
using Bonfire.Services.Extensions;
using Microsoft.VisualBasic;
using MathCore.WPF;

namespace Bonfire.ViewModels
{
    public class SeedlingsViewModel : ViewModel
    {
        private readonly ISeedlingsService _seedlingsService;
        private readonly ISeedsService _seedsService;
        private readonly IUserDialog _userDialog;

        public SeedlingsViewModel(ISeedlingsService seedlings, ISeedsService seedsService, IUserDialog dialog)
        {
            _seedlingsService = seedlings;
            _seedsService = seedsService;
            _userDialog = dialog;
            _SeedlingsView = new CollectionViewSource
            {
                SortDescriptions =
                {
                    new SortDescription(nameof(Seedling.Plant.PlantCulture.Name), ListSortDirection.Ascending),
                    new SortDescription(nameof(Seedling.Plant.PlantSort.Name), ListSortDirection.Ascending),
                    new SortDescription(nameof(Seedling.Plant.PlantSort.Producer.Name), ListSortDirection.Ascending)

                },
                //GroupDescriptions =
                //{
                //    new PropertyGroupDescription(nameof(Seedling.Plant.PlantSort.Name))
                //}
                
                

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
        private Seedling _SelectedConcreteSeedlingViewItem;

        /// <summary>Выбранный объект SeedlingsView</summary>
        public Seedling SelectedConcreteSeedlingViewItem
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
                {
                    if (!string.IsNullOrWhiteSpace(AddCulture) && !string.IsNullOrWhiteSpace(AddSort))
                    {
                        if(string.IsNullOrWhiteSpace(AddProducer)) return;
                        CurrentPlant = AddPlantList.First(p => p.Producer + " " + p.ExpirationDate.Year == value
                                                            &&  p.Culture == AddCulture
                                                            && p.Sort == AddSort);

                    }
                    PlantListView.Refresh();
                }
                    
            }
        }

        #endregion
        

        #region CurrentPlant : PlantFromViewModel - Выбранное растение

        /// <summary>Выбранное растение</summary>
        private PlantFromViewModel _CurrentPlant;


        /// <summary>Выбранное растение</summary>
        public PlantFromViewModel CurrentPlant
        {
            get => _CurrentPlant;
            set
            {
                if (!Set(ref _CurrentPlant, value)) return;
                if (CurrentPlant == null) return;
                CurrentSeed = _seedsService.Seeds.First(s => s.Id == CurrentPlant.Id);

            }
        }

        #endregion

        #region CurrentSeed : Seed - Выбранные семена

        /// <summary>Выбранные семена</summary>
        private Seed _CurrentSeed;

        /// <summary>Выбранные семена</summary>
        public Seed CurrentSeed
        {
            get => _CurrentSeed;
            set
            {
                if (!Set(ref _CurrentSeed, value)) return;
                if (CurrentSeed == null) return;
                Plantable = CurrentSeed.SeedsInfo.AmountSeedsWeight > 0.0 ? CurrentSeed.SeedsInfo.AmountSeedsWeight : CurrentSeed.SeedsInfo.AmountSeeds;
                AddSize = CurrentSeed.SeedsInfo.AmountSeedsWeight > 0.0 ? "гр." : "шт.";
            }
        }

        #endregion

        #region Plantable : double - Доступно для посадки

        /// <summary>Доступно для посадки</summary>
        private double? _Plantable;

        /// <summary>Доступно для посадки</summary>
        public double? Plantable
        {
            get => _Plantable;
            set => Set(ref _Plantable, value);
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
                AddSort = string.Empty;
                AddProducer = string.Empty;
                CurrentSeed = null;
                CurrentPlant = null;
                Plantable = null;
                AddSize = string.Empty;
                AddQuantity = 0.0;

                if (string.IsNullOrWhiteSpace(AddCulture)) return;
                var list = AddSortList.Select(p => p).Where(p => p.Culture == AddCulture).ToList();
                if (list.Count == 1)
                {
                    AddSort = AddSortList.First(p => p.Culture == AddCulture).Sort;
                }

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
                if (string.IsNullOrWhiteSpace(AddCulture) && string.IsNullOrWhiteSpace(AddSort)) return;
                var list = AddPlantList.Select(p => p).Where(p=> p.Culture == AddCulture && p.Sort == AddSort).ToList();
                if (list.Count == 1)
                {
                    AddProducer = AddPlantList.First(p => p.Culture == AddCulture && p.Sort == AddSort).ToString();
                }
                SortListView.Refresh();
                PlantListView.Refresh();
            }
        }

        #endregion



        #endregion

        #region AddQuantityString : string - Количество посевов

        /// <summary>Количество посевов</summary>
        private string _AddQuantityString;

        /// <summary>Количество посевов</summary>
        public string AddQuantityString
        {
            get => _AddQuantityString;
            set
            {
               
                Set(ref _AddQuantityString, value);
                AddQuantity = AddQuantityString.DoubleParseAdvanced();
            }

        }
        #endregion


        #region AddQuantity : double - Количество посевов

        /// <summary>Количество посевов</summary>
        private double _AddQuantity;

        /// <summary>Количество посевов</summary>
        public double AddQuantity  
        {
            get => _AddQuantity;
            set
            {
                
                if (value > Plantable) value = (double)Plantable;
                Set(ref _AddQuantity, value);
            }

        }
        #endregion

        #region PlantingDate : DateTime - Дата высадки

        /// <summary>Дата высадки</summary>
        private DateTime _PlantingDate;

        /// <summary>Дата высадки</summary>
        public DateTime PlantingDate  
        {
            get => _PlantingDate;
            set
            {
                if (!Set(ref _PlantingDate, value)) return;
                MoonPhase = _seedlingsService.Lunar.GetMoonPhase(PlantingDate);
            } 
        }
        #endregion
        
        #region MoonPhase : string - Фаза Луны

        /// <summary>Фаза Луны</summary>
        private string _MoonPhase;

        /// <summary>Фаза Луны</summary>
        public string MoonPhase
        {
            get => GetPathImageMoonPhase(_MoonPhase);


            set => Set(ref _MoonPhase, value);
        }
        #endregion


        #endregion


        #region Методы

        #region Метод загрузки рассады
        private async Task LoadSeedling()
        {
            //var seedlingsQuery = _seedlingsService.Seedlings
            //        .Select(seedlings => new Seedling()
            //        {
            //            Id = seedlings.Id,
            //            Plant = seedlings.Plant,
            //            Wight = seedlings.Wight,
            //            Quantity = seedlings.Quantity,
            //            SeedlingInfos = seedlings.SeedlingInfos
                        

            //        })
            //        .OrderBy(c => c.Plant.PlantCulture.Name)
            //        .ThenBy(s => s.Plant.PlantSort.Name)
            //        .ThenBy(p => p.Plant.PlantSort.Producer.Name)
            //    ;

            Seedlings = new ObservableCollection<Seedling>(await _seedlingsService.Seedlings.ToArrayAsync().ConfigureAwait(false));
            //_SeedlingsView.Source = await seedlingsQuery.ToArrayAsync();
            _SeedlingsView.Source = Seedlings;

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
                })
                .AsEnumerable()
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
                    Id = seeds.Id,
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

        #region Метод для проверки заполнения полей

        private bool Verification()
        {
            var result = PlantingDate != default
                         && AddSize != string.Empty
                         && AddCulture != string.Empty
                         && AddProducer != string.Empty
                         && AddQuantity > 0.0
                         && AddSort != string.Empty
                         && SeedlingSource != string.Empty;

            return result;
        }

        #endregion

        #region Метод для поиска или создания растения

        private Plant GetPlant()
        {

           return _seedsService.Seeds.First(s=>s.Id == CurrentPlant.Id).Plant;

           
        }

        #endregion

        #region Метод для поиска или создания информации о семенах

        //private (Seed?, SeedsInfo?) GetOrCreateSeedInfo()
        //{
        //var quantity = AddQuantityInPac.DoubleParseAdvanced();
        //var quantityPac = AddQuantityPac.DoubleParseAdvanced();
        //var costPack = AddCostPack.DecimalParseAdvanced();

        //if (AddProducerList.Contains(c => c.Name == AddProducer))
        //{
        //    var seed = Seeds
        //        .Find(s =>
        //            s.SeedsInfo.ExpirationDate.Year == AddBestBy.Year
        //            && s.Plant.PlantSort.Producer.Name == AddProducer
        //            && s.Plant.PlantCulture.Name == AddCulture
        //            && s.Plant.PlantSort.Name == AddSort);
        //    if (seed != null)
        //    {
        //        if (AddSize != "Граммы")
        //        {
        //            seed.SeedsInfo.AmountSeeds += quantity * quantityPac;

        //        }
        //        else
        //        {

        //            seed.SeedsInfo.AmountSeedsWeight += quantity * quantityPac;
        //        }
        //        seed.SeedsInfo.PurchaseDate = DateTime.Now;
        //        seed.SeedsInfo.Note = AddNote;
        //        seed.SeedsInfo.CostPack = costPack;

        //        return (seed, null);

        //    }

        //}

        //var seedInfo = new SeedsInfo
        //{
        //    ExpirationDate = AddBestBy,
        //    Note = AddNote,
        //    PurchaseDate = DateTime.Now,
        //    SeedSource = SeedSource,
        //    CostPack = costPack
        //};

        //if (AddSize != "Граммы")
        //{
        //    seedInfo.QuantityPack = quantity;
        //    seedInfo.AmountSeeds = quantity * quantityPac;
        //}
        //else
        //{
        //    seedInfo.WeightPack = quantity;
        //    seedInfo.AmountSeedsWeight = quantity * quantityPac;
        //}

        //return (null, seedInfo);
        //}

        #endregion

        #region Метод для очистки полей

        private void ClearFieldSeedlingView()
        {
            PlantingDate = DateTime.Now;
            AddSize = string.Empty;
            IsSeeds = true;
            AddSort = string.Empty;
            AddProducer = string.Empty;
            CurrentSeed = null;
            CurrentPlant = null;
            Plantable = null;
            AddSize = string.Empty;
            AddQuantity = 0.0;


        }

        #endregion

        #region Метод для обновления коллекции семян

        private void UpdateCollectionViewSource(int id = -1)
        {
            var collection = new ObservableCollection<Seedling>(_seedlingsService.Seedlings.ToArray());
           
            _SeedlingsView.Source = collection;

            if (id != -1)
            {
                var current = collection.FirstOrDefault(s => s.Id == id);
                _SeedlingsView.View.MoveCurrentTo(current);
            }

            OnPropertyChanged(nameof(SeedlingsView));
           
        }

        #endregion
        
        #region Метод получения ссылки на изображение фазы Луны

        private string GetPathImageMoonPhase(string moonPhase)
        {
            return moonPhase switch
            {
                "Растущий серп" => "Image/MoonPhase_2.jpg",
                "Первая четверть" => "Image/MoonPhase_3.jpg",
                "Растущая луна" => "Image/MoonPhase_4.jpg",
                "Полнолуние" => "Image/MoonPhase_5.jpg",
                "Убывающая луна" => "Image/MoonPhase_6.jpg",
                "Третья четверть" => "Image/MoonPhase_7.jpg",
                "Убывающий месяц" => "Image/MoonPhase_8.jpg",
                _ => "Image/MoonPhase_1.jpg"
            };

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
            PlantingDate = DateTime.Now;


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
                        Weight = seedlings.Weight,
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
                            Weight = seedlings.Weight,
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

        #region Command AddOrCorrectSeedlingCommand - Команда для добавления или корректирования посадки

        /// <summary> Команда для добавления или корректирования посадки </summary>
        private ICommand _AddOrCorrectSeedlingCommand;

        /// <summary> Команда для добавления или корректирования посадки </summary>
        public ICommand AddOrCorrectSeedlingCommand => _AddOrCorrectSeedlingCommand
            ??= new LambdaCommandAsync(OnAddOrCorrectSeedlingCommandExecuted, CanAddOrCorrectSeedlingCommandExecute);

        /// <summary> Проверка возможности выполнения - Команда для добавления или корректирования посадки </summary>
        private bool CanAddOrCorrectSeedlingCommandExecute() => Verification();

        /// <summary> Логика выполнения - Команда для добавления или корректирования посадки </summary>
        private async Task OnAddOrCorrectSeedlingCommandExecuted()
        {
            var plant = GetPlant();
            var seedlingInfo = new SeedlingInfo
            {
                LandingDate = PlantingDate,
                LunarPhase = _seedlingsService.Lunar.GetMoonPhase(PlantingDate),
                SeedlingNumber = 0,
                SeedlingSource = SeedlingSource

            };

            var seedling = new Seedling
            {
                Plant = plant,
                SeedId = CurrentSeed.Id,
                SeedlingInfos = new List<SeedlingInfo> { seedlingInfo }
                
            };

            switch (AddSize)
            {
                case "гр.":
                    seedling.Weight = AddQuantity;
                    CurrentSeed.SeedsInfo.AmountSeedsWeight -= AddQuantity;
                    break;

                case "шт.":
                    seedling.Quantity = AddQuantity;
                    CurrentSeed.SeedsInfo.AmountSeeds -= AddQuantity;
                    break;
            }


            seedling = await _seedlingsService.MakeASeedling(seedling).ConfigureAwait(false);
            var seed = await _seedsService.UpdateSeed(CurrentSeed).ConfigureAwait(false);
            
            RemoveItemFromCollection(seed);
            ClearFieldSeedlingView();
            UpdateCollectionViewSource(seedling.Id);

        }

        private void RemoveItemFromCollection(Seed seed)
        {
            if (seed.SeedsInfo.AmountSeeds + seed.SeedsInfo.AmountSeedsWeight != 0) return;
            AddPlantList.Remove(AddPlantList.First(s => s.Producer == seed.Plant.PlantSort.Producer.Name
                                                        && s.Culture == seed.Plant.PlantCulture.Name
                                                        && s.Sort == seed.Plant.PlantSort.Name
                                                        && s.ExpirationDate == seed.SeedsInfo.ExpirationDate));
            _PlantListView.Source = AddPlantList;
            var list = AddPlantList.Select(s => s).Where(s => s.Producer == seed.Plant.PlantSort.Producer.Name
                                                              && s.Culture == seed.Plant.PlantCulture.Name
                                                              && s.Sort == seed.Plant.PlantSort.Name).ToList();
            if (list.Count == 0)
                AddSortList.Remove(AddSortList.First(s => s.Sort == seed.Plant.PlantSort.Name));
        }

        #endregion

        #region Command DeleteSeedlingCommand - Команда для удаления семян

        /// <summary> Команда для удаления семян </summary>
        private ICommand _DeleteSeedlingCommand;

        /// <summary> Команда для удаления семян </summary>
        public ICommand DeleteSeedlingCommand => _DeleteSeedlingCommand
            ??= new LambdaCommandAsync(OnDeleteSeedlingCommandExecuted, CanDeleteSeedlingCommandExecute);

        /// <summary> Проверка возможности выполнения - Команда для удаления семян </summary>
        private bool CanDeleteSeedlingCommandExecute() => SelectedItem != null;

        /// <summary> Логика выполнения - Команда для удаления семян </summary>
        private async Task OnDeleteSeedlingCommandExecuted()
        {
            if (!_userDialog.YesNoQuestion(
                    $"Вы уверены, что хотите удалить рассаду сорта - {SelectedItem.Plant.PlantSort.Name}",
                    "Удаление рассады")) return;

            var deleteSeedling = await _seedlingsService.DeleteSeedling(SelectedItem).ConfigureAwait(false);
            await UpdateSeed(deleteSeedling);
            Seedlings.Remove(deleteSeedling);
            
            UpdateCollectionViewSource();
        }

        private async Task UpdateSeed(Seedling deleteSeedling)
        {
            var updateSeed = _seedsService.Seeds.First(s => s.Id == SelectedItem.SeedId);
            updateSeed.SeedsInfo.AmountSeeds = deleteSeedling.Quantity;
            updateSeed.SeedsInfo.AmountSeedsWeight = deleteSeedling.Weight;
            await _seedsService.UpdateSeed(updateSeed);
        }

        #endregion

        #endregion


    }
}
