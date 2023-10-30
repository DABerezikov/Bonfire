using Bonfire.Services.Interfaces;
using Microsoft.Win32;
using System.IO;
using System.Windows.Controls;
using System.Windows;
using System;
using System.Linq;

namespace Bonfire.Services
{

    public class UserDialog : IUserDialog
    {
        /// <summary>Активное окно приложения</summary>
        protected static Window? ActiveWindow =>
            Application.Current.Windows.Cast<Window>().FirstOrDefault(w => w.IsActive);

        /// <summary>Окно с фокусом ввода</summary>
        protected static Window? FocusedWindow =>
            Application.Current.Windows.Cast<Window>().FirstOrDefault(w => w.IsFocused);

        /// <summary>Текущее окно приложения</summary>
        protected static Window? CurrentWindow => FocusedWindow ?? ActiveWindow;


        /// <summary>Открыть диалога выбора файла для чтения</summary>
        /// <param name="title">Заголовок диалогового окна</param>
        /// <param name="filter">Фильтр файлов диалога</param>
        /// <param name="defaultFilePath">Путь к файлу по умолчанию</param>
        /// <returns>Выбранный файл, либо null, если диалог был отменён</returns>
        public virtual FileInfo? OpenFile(string title, string filter =
            "Исходные файлы (*.pdf, *.xls, *.xlsx)|*.pdf; *.xls; *.xlsx|" +
            " PDF(*.pdf)|*.pdf| Excel(*.xls,*.xlsx)|*.xls;*.xlsx|" +
            " Все файлы (*.*)|*.*", string? defaultFilePath = null)
        {
            var dialog = new OpenFileDialog
            {
                Title = title,
                RestoreDirectory = true,
                Filter = filter ?? throw new ArgumentNullException(nameof(filter)),
            };
            if (defaultFilePath is { Length: > 0 })
                dialog.FileName = defaultFilePath;

            return dialog.ShowDialog(CurrentWindow) == true
                ? new(dialog.FileName)
                : defaultFilePath is null
                    ? null
                    : new(defaultFilePath);
        }

        /// <summary>Открыть диалога выбора файла для записи</summary>
        /// <param name="title">Заголовок диалогового окна</param>
        /// <param name="filter">Фильтр файлов диалога</param>
        /// <param name="defaultFilePath">Путь к файлу по умолчанию</param>
        /// <returns>Выбранный файл, либо null, если диалог был отменён</returns>
        public virtual FileInfo? SaveFile(string title, string filter = "Все файлы (*.*)|*.*",
            string? defaultFilePath = null)
        {
            var dialog = new SaveFileDialog
            {
                Title = title,
                RestoreDirectory = true,
                Filter = filter ?? throw new ArgumentNullException(nameof(filter)),
            };
            if (defaultFilePath is { Length: > 0 })
                dialog.FileName = defaultFilePath;

            return dialog.ShowDialog(CurrentWindow) == true
                ? new(dialog.FileName)
                : defaultFilePath is null
                    ? null
                    : new(defaultFilePath);
        }

        /// <summary>Диалог с текстовым вопросом и вариантами выбора Yes/No</summary>
        /// <param name="text">Заголовок окна диалога</param>
        /// <param name="title">Текст в окне диалога</param>
        /// <returns>Истина, если был сделан выбор Yes</returns>
        public virtual bool YesNoQuestion(string text, string title = "Вопрос...")
        {
            var result = CurrentWindow is null
                ? MessageBox.Show(text, title, MessageBoxButton.YesNo, MessageBoxImage.Question)
                : MessageBox.Show(CurrentWindow, text, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
            return result == MessageBoxResult.Yes;
        }

        /// <summary>Диалог с текстовым вопросом и вариантами выбора Ok/Cancel</summary>
        /// <param name="text">Заголовок окна диалога</param>
        /// <param name="title">Текст в окне диалога</param>
        /// <returns>Истина, если был сделан выбор Ok</returns>
        public virtual bool OkCancelQuestion(string text, string title = "Вопрос...")
        {
            var result = CurrentWindow is null
                ? MessageBox.Show(text, title, MessageBoxButton.OKCancel, MessageBoxImage.Question)
                : MessageBox.Show(CurrentWindow, text, title, MessageBoxButton.OKCancel, MessageBoxImage.Question);
            return result == MessageBoxResult.OK;
        }

        /// <summary>Диалог с информацией</summary>
        /// <param name="text">Заголовок окна диалога</param>
        /// <param name="title">Текст в окне диалога</param>
        public virtual void Information(string text, string title = "Вопрос...")
        {
            if (CurrentWindow is null)
                MessageBox.Show(text, title, MessageBoxButton.OK, MessageBoxImage.Information);
            else
                MessageBox.Show(CurrentWindow, text, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>Диалог с предупреждением</summary>
        /// <param name="text">Заголовок окна диалога</param>
        /// <param name="title">Текст в окне диалога</param>
        public virtual void Warning(string text, string title = "Вопрос...")
        {
            if (CurrentWindow is null)
                MessageBox.Show(text, title, MessageBoxButton.OK, MessageBoxImage.Warning);
            else
                MessageBox.Show(CurrentWindow, text, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        /// <summary>Диалог с ошибкой</summary>
        /// <param name="text">Заголовок окна диалога</param>
        /// <param name="title">Текст в окне диалога</param>
        public virtual void Error(string text, string title = "Вопрос...")
        {
            if (CurrentWindow is null)
                MessageBox.Show(text, title, MessageBoxButton.OK, MessageBoxImage.Error);
            else
                MessageBox.Show(CurrentWindow, text, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        //public virtual bool CreateOrEditGardener(Gardener gardener)
        //{
        //    var tempGardener = gardener;
        //    var viewModel = new EditGardenerViewModel(tempGardener);
        //    var window = new EditGardenerWindow { DataContext = viewModel };
        //    var result = window.ShowDialog();
        //    return result ?? false;
        //}

        //public virtual bool CreateOrEditParcel(Parcel parcel, IRepository<Gardener> gardenerRepository)
        //{
        //    var tempParcel = parcel;
        //    var viewModel = new EditParcelViewModel(tempParcel, gardenerRepository);
        //    var window = new EditParcelWindow { DataContext = viewModel };
        //    var result = window.ShowDialog();
        //    return result ?? false;


    }
}




