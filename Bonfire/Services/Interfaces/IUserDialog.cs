using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Controls;

namespace Bonfire.Services.Interfaces
{
    public interface IUserDialog
    {
        
            /// <summary>Открыть диалога выбора файла для чтения</summary>
            /// <param name="title">Заголовок диалогового окна</param>
            /// <param name="filter">Фильтр файлов диалога</param>
            /// <param name="defaultFilePath">Путь к файлу по умолчанию</param>
            /// <returns>Выбранный файл, либо null, если диалог был отменён</returns>
            FileInfo? OpenFile(string title, string filter = "Исходные файлы (*.pdf, *.xls, *.xlsx)|*.pdf; *.xls; *.xlsx|" +
                                                             " PDF(*.pdf)|*.pdf| Excel(*.xls,*.xlsx)|*.xls;*.xlsx|" +
                                                             " Все файлы (*.*)|*.*", string? defaultFilePath = null);

            /// <summary>Открыть диалога выбора файла для записи</summary>
            /// <param name="title">Заголовок диалогового окна</param>
            /// <param name="filter">Фильтр файлов диалога</param>
            /// <param name="defaultFilePath">Путь к файлу по умолчанию</param>
            /// <returns>Выбранный файл, либо null, если диалог был отменён</returns>
            FileInfo? SaveFile(string title, string filter = "Все файлы (*.*)|*.*", string? defaultFilePath = null);

            /// <summary>Диалог с текстовым вопросом и вариантами выбора Yes/No</summary>
            /// <param name="text">Заголовок окна диалога</param>
            /// <param name="title">Текст в окне диалога</param>
            /// <returns>Истина, если был сделан выбор Yes</returns>
            bool YesNoQuestion(string text, string title = "Вопрос...");

            /// <summary>Диалог с текстовым вопросом и вариантами выбора Ok/Cancel</summary>
            /// <param name="text">Заголовок окна диалога</param>
            /// <param name="title">Текст в окне диалога</param>
            /// <returns>Истина, если был сделан выбор Ok</returns>
            bool OkCancelQuestion(string text, string title = "Вопрос...");

            /// <summary>Диалог с информацией</summary>
            /// <param name="text">Заголовок окна диалога</param>
            /// <param name="title">Текст в окне диалога</param>
            void Information(string text, string title = "Вопрос...");

            /// <summary>Диалог с предупреждением</summary>
            /// <param name="text">Заголовок окна диалога</param>
            /// <param name="title">Текст в окне диалога</param>
            void Warning(string text, string title = "Вопрос...");

            /// <summary>Диалог с ошибкой</summary>
            /// <param name="text">Заголовок окна диалога</param>
            /// <param name="title">Текст в окне диалога</param>
            void Error(string text, string title = "Вопрос...");

            ///// <summary>Диалог создания или редактирования садовода</summary>
            ///// <param name="gardener">Садовод</param>
            //bool CreateOrEditGardener(Gardener gardener);

            //bool CreateOrEditParcel(Parcel parcel, IRepository<Gardener> gardenerRepository);
        
    }
}
