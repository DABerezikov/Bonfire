using Bonfire.Services.Interfaces;
using Bonfire.ViewModels.Base;
using BonfireDB.Entities;
using System.Threading.Tasks;
using System.Windows.Input;
using Bonfire.Infrastructure.Commands;
using System.Collections.ObjectModel;
using System.Drawing;
using AutoMapper;
using System.Windows.Controls;
using System.Windows.Media;
using Brushes = System.Windows.Media.Brushes;
using Rectangle = System.Windows.Shapes.Rectangle;
using Bonfire.Models;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using Point = System.Windows.Point;

namespace Bonfire.ViewModels
{
    public class GardenViewModel : ViewModel
    {
        private readonly ISeedlingsService _SeedlingsService;
        private readonly ISeedsService _SeedsService;
        private readonly ISeedbedsService _SeedBedService;
        private readonly IUserDialog _UserDialog;
        private readonly IMapper _Mapper;

        private Brush ButtonPressedColor = new SolidColorBrush(Colors.LightGray);
        private Brush ButtonUnPressedColor = new SolidColorBrush(Colors.Transparent);




        public GardenViewModel(ISeedlingsService seedlings, ISeedsService seedsService, ISeedbedsService seedbedsService, IUserDialog dialog, IMapper mapper)
        {
            _SeedlingsService = seedlings;
            _SeedsService = seedsService;
            _SeedBedService = seedbedsService;
            _UserDialog = dialog;
            _Mapper = mapper;
            

        }

        #region Свойства

       

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


        

        #region SeedBeds : ObservableCollection<Rectangle> - Коллекция рассады

        /// <summary>Коллекция рассады</summary>
        private ObservableCollection<SeedBedFromViewModel> _Seedbeds =
        [
            
        ];

        /// <summary>Коллекция рассады</summary>
        public ObservableCollection<SeedBedFromViewModel> SeedBeds
        {
            get => _Seedbeds;
            set => Set(ref _Seedbeds, value);
        }
        #endregion

        #region CurrentSeedBedFromViewModel : SeedBedFromViewModel - Коллекция рассады

        /// <summary>Коллекция рассады</summary>
        private SeedBedFromViewModel _CurrentSeedBedFromViewModel;

        /// <summary>Коллекция рассады</summary>
        public SeedBedFromViewModel CurrentSeedBedFromViewModel
        {
            get => _CurrentSeedBedFromViewModel;
            set => Set(ref _CurrentSeedBedFromViewModel, value);
        }
        #endregion





        #region MousePosition : Point - Положение мыши

        /// <summary>Положение мыши</summary>
        private Point _MousePosition ;

        /// <summary>Положение мыши</summary>
        public Point MousePosition
        {
            get => _MousePosition;
            set
            {
                Set(ref _MousePosition, value);
                if (IsMoveSeedBed)
                {
                    CurrentBed.Position = MousePosition;

                }
            }
        }

        #endregion

        #region CurrentBed : SeedBedFromViewModel - Выбранная грядка

        /// <summary>Выбранная грядка</summary>
        private SeedBedFromViewModel? _CurrentBed;

        /// <summary>Выбранная грядка</summary>
        public SeedBedFromViewModel? CurrentBed
        {
            get => _CurrentBed;
            set => Set(ref _CurrentBed, value);
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
        
        #region CurrentWidth : double - Введенная ширина

        /// <summary>Введенная ширина</summary>
        private double _CurrentWidth;


        /// <summary>Введенная ширина</summary>
        public double CurrentWidth
        {
            get => _CurrentWidth;
            set => Set(ref _CurrentWidth, value);
        }

        #endregion
        
        #region CurrentHeight : double - Введенная высота

        /// <summary>Введенная высота</summary>
        private double _CurrentHeight;


        /// <summary>Введенная высота</summary>
        public double CurrentHeight
        {
            get => _CurrentHeight;
            set => Set(ref _CurrentHeight, value);
        }

        #endregion
        
        #region IsCreateSeedBed : bool - Создать грядку

        /// <summary>Создать грядку</summary>
        private bool _IsCreateSeedBed;


        /// <summary>Создать грядку</summary>
        public bool IsCreateSeedBed
        {
            get => _IsCreateSeedBed;
            set
            {
                Set(ref _IsCreateSeedBed, value);
                CreateButtonPressedColor = IsCreateSeedBed ? ButtonPressedColor : ButtonUnPressedColor;
            }
        }

        #endregion
        
        #region IsMoveSeedBed : bool - Переместить грядку

        /// <summary>Переместить грядку</summary>
        private bool _IsMoveSeedBed;


        /// <summary>Переместить грядку</summary>
        public bool IsMoveSeedBed
        {
            get => _IsMoveSeedBed;
            set
            {
                Set(ref _IsMoveSeedBed, value);
                MoveButtonPressedColor = IsMoveSeedBed ? ButtonPressedColor : ButtonUnPressedColor;
            }
        }

        #endregion

        #region CreateButtonPressedColor : Color - Цвет нажатой кнопки

        /// <summary>Цвет нажатой кнопки</summary>
        private Brush _CreateButtonPressedColor = new SolidColorBrush(Colors.Transparent);


        /// <summary>Цвет нажатой кнопки</summary>
        public Brush CreateButtonPressedColor
        {
            get => _CreateButtonPressedColor;
            set => Set(ref _CreateButtonPressedColor, value);
        }

        #endregion
        #region MoveButtonPressedColor : Color - Цвет нажатой кнопки

        /// <summary>Цвет нажатой кнопки</summary>
        private Brush _MoveButtonPressedColor = new SolidColorBrush(Colors.Transparent);


        /// <summary>Цвет нажатой кнопки</summary>
        public Brush MoveButtonPressedColor
        {
            get => _MoveButtonPressedColor;
            set => Set(ref _MoveButtonPressedColor, value);
        }

        #endregion

        #region FillSeedbed : Brush - Цвет заливки грядки

        /// <summary>Цвет заливки грядки</summary>
        private Brush _FillSeedbed = new SolidColorBrush(Colors.Transparent);


        /// <summary>Цвет заливки грядки</summary>
        public Brush FillSeedbed
        {
            get => _FillSeedbed;
            set => Set(ref _FillSeedbed, value);
        }

        #endregion

        #region StrokeSeedbed : Brush - Цвет обводки грядки

        /// <summary>Цвет обводки грядки</summary>
        private Brush _StrokeSeedbed = new SolidColorBrush(Colors.Black);


        /// <summary>Цвет обводки грядки</summary>
        public Brush StrokeSeedbed
        {
            get => _StrokeSeedbed;
            set => Set(ref _StrokeSeedbed, value);
        }

        #endregion
        
        #region StrokeThicknessSeedbed : int - Толщина обводки грядки

        /// <summary>Толщина обводки грядки</summary>
        private int _StrokeThicknessSeedbed = 2;


        /// <summary>Толщина обводки грядки</summary>
        public int StrokeThicknessSeedbed
        {
            get => _StrokeThicknessSeedbed;
            set => Set(ref _StrokeThicknessSeedbed, value);
        }

        #endregion
        
        #region IsRegularSeedbed : bool - Толщина обводки грядки

        /// <summary>Толщина обводки грядки</summary>
        private bool _IsRegularSeedbed = true;


        /// <summary>Толщина обводки грядки</summary>
        public bool IsRegularSeedbed
        {
            get => _IsRegularSeedbed;
            set => Set(ref _IsRegularSeedbed, value);
        }

        #endregion




        #endregion


        #region Методы

        private void DrawSeedBed(Point point)
        {
            var seedBedFromViewModel = CreateSeedBedFromViewModel(point);

            CreateSeedbed(seedBedFromViewModel);

            SeedBeds.Add(seedBedFromViewModel);

            OnPropertyChanged(nameof(SeedBeds));

        }

        private SeedBedFromViewModel CreateSeedBedFromViewModel(Point point)
        {
            var seedBedFromViewModel = new SeedBedFromViewModel()
            {
                Position = point,
                Width = CurrentWidth,
                Height = CurrentHeight,
               
                Soil = new Soil()
            };
            return seedBedFromViewModel;
        }

        private void CreateSeedbed(SeedBedFromViewModel seedBedFromViewModel)
        {
            var seedbed = new Seedbed();

            _Mapper.Map(seedBedFromViewModel, seedbed);

            _SeedBedService.MakeASeedbed(seedbed);
        }

        private void ChoiсeBed()
        {
            foreach (var bed in SeedBeds)
            {
                if (!(MousePosition.X >= bed.Position.X) || !(MousePosition.X <= bed.Position.X + bed.Width) ||
                    !(MousePosition.Y >= bed.Position.Y) || !(MousePosition.Y <= bed.Position.Y + bed.Height)) continue;
                if (CurrentBed != null)
                    CurrentBed.IsSelected = false;
                CurrentBed = bed;
                CurrentBed.IsSelected = true;
            }
            if(CurrentBed !=null) CurrentBed.IsSelected = false;
        }

        #endregion


        #region Комманды

        #region Command LoadDataCommand - Команда для загрузки данных

        /// <summary> Команда для загрузки данных </summary>
        private ICommand? _LoadDataCommand;

        /// <summary> Команда для загрузки данных </summary>
        public ICommand LoadDataCommand => _LoadDataCommand
            ??= new LambdaCommandAsync(OnLoadDataCommandExecuted, CanLoadDataCommandExecute);

        /// <summary> Проверка возможности выполнения - Команда для загрузки данных </summary>
        private bool CanLoadDataCommandExecute() => true;

        /// <summary> Логика выполнения - Команда для загрузки данных </summary>
        private async Task OnLoadDataCommandExecuted()
        {

           


        }
        #endregion

       

        #region Command MouseMoveCommand - Команда для перемещения мыши

        /// <summary> Команда для перемещения мыши </summary>
        private ICommand? _MouseMoveCommand;

        /// <summary> Команда для перемещения мыши </summary>
        public ICommand MouseMoveCommand => _MouseMoveCommand
            ??= new LambdaCommandAsync(OnMouseMoveCommandExecuted, CanMouseMoveCommandExecute);

        /// <summary> Проверка возможности выполнения - Команда для перемещения мыши </summary>
        private bool CanMouseMoveCommandExecute(object p) => true;

        /// <summary> Логика выполнения - Команда для перемещения мыши </summary>
        private async Task OnMouseMoveCommandExecuted(object p)
        {

            MousePosition = p is Grid canvas ? Mouse.GetPosition(canvas) : default;
           

        }
        #endregion

        #region Command MouseDownCommand - Команда нажатия мыши

        /// <summary> Команда нажатия мыши </summary>
        private ICommand? _MouseDownCommand;

        /// <summary> Команда нажатия мыши </summary>
        public ICommand MouseDownCommand => _MouseDownCommand
            ??= new LambdaCommandAsync(OnMouseDownCommandExecuted, CanMouseDownCommandExecute);

        /// <summary> Проверка возможности выполнения - Команда нажатия мыши </summary>
        private bool CanMouseDownCommandExecute(object p) => true;

        /// <summary> Логика выполнения - Команда нажатия мыши </summary>
        private async Task OnMouseDownCommandExecuted(object p)
        {
            if (IsMoveSeedBed)
                IsMoveSeedBed = !IsMoveSeedBed;

            ChoiсeBed();


            if(CreateSeedBedCommand.CanExecute(this))
                CreateSeedBedCommand.Execute(this);
        }

       

        #endregion

        #region Command MouseUpCommand - Команда для отжатия мыши

        /// <summary> Команда для отжатия мыши </summary>
        private ICommand? _MouseUpCommand;

        /// <summary> Команда для отжатия мыши </summary>
        public ICommand MouseUpCommand => _MouseUpCommand
            ??= new LambdaCommandAsync(OnMouseUpCommandExecuted, CanMouseUpCommandExecute);

        /// <summary> Проверка возможности выполнения - Команда для отжатия мыши </summary>
        private bool CanMouseUpCommandExecute(object p) => true;

        /// <summary> Логика выполнения - Команда для отжатия мыши </summary>
        private async Task OnMouseUpCommandExecuted(object p)
        {




        }
        #endregion

        
        #region Command CreateSeedBedCommand - Команда для рисования грядки

        /// <summary> Команда для рисования грядки </summary>
        private ICommand? _CreateSeedBedCommand;

        /// <summary> Команда для рисования грядки </summary>
        public ICommand CreateSeedBedCommand => _CreateSeedBedCommand
            ??= new LambdaCommandAsync(OnCreateSeedBedCommandExecuted, CanCreateSeedBedCommandExecute);

        /// <summary> Проверка возможности выполнения - Команда для рисования грядки </summary>
        private bool CanCreateSeedBedCommandExecute() => IsCreateSeedBed;

        /// <summary> Логика выполнения - Команда для рисования грядки </summary>
        private async Task OnCreateSeedBedCommandExecuted()
        {
           DrawSeedBed(MousePosition);


        }
        #endregion
        
        #region Command IsCreateSeedBedCommand - Команда для выбора редима рисования грядки

        /// <summary> Команда для выбора редима рисования грядки </summary>
        private ICommand? _IsCreateSeedBedCommand;

        /// <summary> Команда для выбора редима рисования грядки </summary>
        public ICommand IsCreateSeedBedCommand => _IsCreateSeedBedCommand
            ??= new LambdaCommandAsync(OnIsCreateSeedBedCommandExecuted, CanIsCreateSeedBedCommandExecute);

        /// <summary> Проверка возможности выполнения - Команда для выбора редима рисования грядки </summary>
        private bool CanIsCreateSeedBedCommandExecute() => true;

        /// <summary> Логика выполнения - Команда для выбора редима рисования грядки </summary>
        private async Task OnIsCreateSeedBedCommandExecuted()
        {
            IsCreateSeedBed = !IsCreateSeedBed;


        }
        #endregion
        
        #region Command IsMoveSeedBedCommand - Команда для выбора редима перемещения грядки

        /// <summary> Команда для выбора редима перемещения грядки </summary>
        private ICommand? _IsMoveSeedBedCommand;

        /// <summary> Команда для выбора редима перемещения грядки </summary>
        public ICommand IsMoveSeedBedCommand => _IsMoveSeedBedCommand
            ??= new LambdaCommandAsync(OnIsMoveSeedBedCommandExecuted, CanIsMoveSeedBedCommandExecute);

        /// <summary> Проверка возможности выполнения - Команда для выбора редима перемещения грядки </summary>
        private bool CanIsMoveSeedBedCommandExecute() => CurrentBed != null;

        /// <summary> Логика выполнения - Команда для выбора редима перемещения грядки </summary>
        private async Task OnIsMoveSeedBedCommandExecuted()
        {
            IsMoveSeedBed = !IsMoveSeedBed;


        }
        #endregion



        #endregion


    }
}
