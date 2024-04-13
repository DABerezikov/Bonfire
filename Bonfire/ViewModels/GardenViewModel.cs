using Bonfire.Services.Interfaces;
using Bonfire.ViewModels.Base;
using BonfireDB.Entities;
using System.Threading.Tasks;
using System.Windows.Input;
using Bonfire.Infrastructure.Commands;
using System.Collections.ObjectModel;
using AutoMapper;
using System.Windows.Controls;
using Bonfire.Templates;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Rectangle = System.Windows.Shapes.Rectangle;

namespace Bonfire.ViewModels
{
    public class GardenViewModel : ViewModel
    {
        private readonly ISeedlingsService _SeedlingsService;
        private readonly ISeedsService _SeedsService;
        private readonly ISeedbedService _SeedBedService;
        private readonly IUserDialog _UserDialog;
        private readonly IMapper _Mapper;
        private DrawBehavior? _Draw;

        public GardenViewModel(ISeedlingsService seedlings, ISeedsService seedsService, IUserDialog dialog, IMapper mapper)
        {
            _SeedlingsService = seedlings;
            _SeedsService = seedsService;
           
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
        private ObservableCollection<Rectangle> _Seedbeds = new();

        /// <summary>Коллекция рассады</summary>
        public ObservableCollection<Rectangle> SeedBeds
        {
            get => _Seedbeds;
            set => Set(ref _Seedbeds, value);
        }
        #endregion





        #region MousePosition : Point - Коллекция рассады

        /// <summary>Коллекция рассады</summary>
        private System.Windows.Point _MousePosition = new();

        /// <summary>Коллекция рассады</summary>
        public System.Windows.Point MousePosition
        {
            get => _MousePosition;
            set => Set(ref _MousePosition, value);
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



        #endregion


        #region Методы

        private void DrawRectangle()
        {
            var rect = new Rectangle
            {
                Width = 100,
                Height = 100,
                Fill = Brushes.Transparent,
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };

            // Установка позиции прямоугольника на Canvas
            Canvas.SetLeft(rect, MousePosition.X);
            Canvas.SetTop(rect, MousePosition.Y);

            SeedBeds.Add(rect);

            OnPropertyChanged(nameof(SeedBeds));
        }

        #endregion


        #region Комманды

        #region Command LoadDataCommand - Команда для рисования прямоугольника

        /// <summary> Команда для рисования прямоугольника </summary>
        private ICommand _LoadDataCommand;

        /// <summary> Команда для рисования прямоугольника </summary>
        public ICommand LoadDataCommand => _LoadDataCommand
            ??= new LambdaCommandAsync(OnLoadDataCommandExecuted, CanLoadDataCommandExecute);

        /// <summary> Проверка возможности выполнения - Команда для рисования прямоугольника </summary>
        private bool CanLoadDataCommandExecute() => true;

        /// <summary> Логика выполнения - Команда для рисования прямоугольника </summary>
        private async Task OnLoadDataCommandExecuted()
        {

           


        }
        #endregion

       

        #region Command MouseMoveCommand - Команда для рисования прямоугольника

        /// <summary> Команда для рисования прямоугольника </summary>
        private ICommand _MouseMoveCommand;

        /// <summary> Команда для рисования прямоугольника </summary>
        public ICommand MouseMoveCommand => _MouseMoveCommand
            ??= new LambdaCommandAsync(OnMouseMoveCommandExecuted, CanMouseMoveCommandExecute);

        /// <summary> Проверка возможности выполнения - Команда для рисования прямоугольника </summary>
        private bool CanMouseMoveCommandExecute(object p) => true;

        /// <summary> Логика выполнения - Команда для рисования прямоугольника </summary>
        private async Task OnMouseMoveCommandExecuted(object p)
        {

            MousePosition = p is Canvas canvas ? Mouse.GetPosition(canvas) : default;
           

        }
        #endregion

        #region Command MouseDownCommand - Команда для рисования прямоугольника

        /// <summary> Команда для рисования прямоугольника </summary>
        private ICommand _MouseDownCommand;

        /// <summary> Команда для рисования прямоугольника </summary>
        public ICommand MouseDownCommand => _MouseDownCommand
            ??= new LambdaCommandAsync(OnMouseDownCommandExecuted, CanMouseDownCommandExecute);

        /// <summary> Проверка возможности выполнения - Команда для рисования прямоугольника </summary>
        private bool CanMouseDownCommandExecute(object p) => true;

        /// <summary> Логика выполнения - Команда для рисования прямоугольника </summary>
        private async Task OnMouseDownCommandExecuted(object p)
        {
            DrawRectangle();
            


        }
        #endregion

        #region Command MouseUpCommand - Команда для рисования прямоугольника

        /// <summary> Команда для рисования прямоугольника </summary>
        private ICommand _MouseUpCommand;

        /// <summary> Команда для рисования прямоугольника </summary>
        public ICommand MouseUpCommand => _MouseUpCommand
            ??= new LambdaCommandAsync(OnMouseUpCommandExecuted, CanMouseUpCommandExecute);

        /// <summary> Проверка возможности выполнения - Команда для рисования прямоугольника </summary>
        private bool CanMouseUpCommandExecute(object p) => true;

        /// <summary> Логика выполнения - Команда для рисования прямоугольника </summary>
        private async Task OnMouseUpCommandExecuted(object p)
        {




        }
        #endregion



        #endregion


    }
}
