# Bonfire — Система управления огородом

## Назначение

Десктопное WPF-приложение для ведения записей об огороде: учёт семян, рассады, производителей и лунного календаря.

## Технологический стек

| Компонент | Технология |
|---|---|
| UI | WPF (.NET 10, net10.0-windows) |
| БД | SQLite через Entity Framework Core 10 |
| DI / хостинг | Microsoft.Extensions.Hosting 10 |
| Маппинг | AutoMapper 13 |
| Excel | EPPlus 7 |
| Иконки | FontAwesome5 |
| Логирование | NLog 5 |
| Лунный календарь | MoonCalendar (проект в решении, `MoonCalendar/MoonPhase.cs`) |
| Тесты | XUnit 2 |

## Структура решения

```
Bonfire.sln
├── MoonCalendar/              # Класс-библиотека расчётов лунного календаря (net10.0)
├── Bonfire/                   # WPF-приложение (net10.0-windows)
│   ├── Views/                 # XAML-окна и ресурсы
│   ├── ViewModels/            # VM: MainWindow, Seeds, Seedlings, LibraryEditor
│   ├── Models/                # DTO между слоями (SeedsFromViewModel и др.)
│   ├── Services/              # Бизнес-логика (ISeedsService, ISeedlingsService)
│   ├── Infrastructure/        # Commands (LambdaCommand, LambdaCommandAsync)
│   ├── Templates/             # DataGrid-стили, Behaviors
│   ├── Data/                  # DbInitializer, DbRegistrator, PlantClassList
│   └── App.xaml.cs            # Точка входа, настройка IHost
│
├── BonfireDB/                 # Библиотека данных (net10.0)
│   ├── Context/DbBonfire.cs   # EF Core DbContext
│   ├── Entities/              # Доменные сущности + базовые классы
│   ├── Repository/            # DbRepository<T> + специализированные репозитории
│   └── Migrations/            # 8 EF-миграций (история с 2023-10)
│
└── Tests/SeedsFromViewModel.Tests/   # XUnit (net10.0-windows)
```

## Архитектура

**Паттерн: MVVM + Repository + DI**

```
View (XAML) ──binds──> ViewModel ──calls──> Service ──calls──> Repository ──> DbContext ──> SQLite
```

- `ViewModel` наследует `Base/ViewModel.cs` (INotifyPropertyChanged)
- Команды: `LambdaCommand` / `LambdaCommandAsync` через `ICommand`
- DI регистрируется в: `ViewModelRegister`, `ServiceRegister`, `RepositoryRegister`, `DbRegistrator`
- Навигация: один `MainWindow` со сменой контента через ViewModel

## Доменная модель

| Сущность | Описание |
|---|---|
| `Plant` | Растение (ссылки на `PlantCulture`, `PlantSort`) |
| `PlantCulture` | Культура (Помидор, Огурец …) с классом растения |
| `PlantSort` | Сорт, привязан к `Producer` |
| `Producer` | Производитель семян |
| `Seed` | Пакет семян (ссылка на `Plant`, `SeedsInfo`) |
| `SeedsInfo` | Метаданные семян (дата, вес, кол-во, годность) |
| `Seedling` | Рассада (вес, кол-во, ссылка на `Seed`) |
| `SeedlingInfo` | Запись о рассаде (дата посева, гибель `IsDead`) |
| `Treatment` | Обработка растения |
| `Replanting` | Пересадка |

## Конфигурация

`Bonfire/appsettings.json`:
```json
{
  "Database": {
    "Type": "SQLite",
    "ConnectionStrings": {
      "SQLite": "Data Source=BonfireDB.db"
    }
  }
}
```

База данных: файл `BonfireDB.db` рядом с exe-шником. Миграции применяются автоматически при старте через `DbInitializer`.

## Важные особенности

- **MoonCalendar** — собственный проект в решении (`MoonCalendar/MoonPhase.cs`), нет внешних зависимостей. Единственный публичный класс `MoonPhase` реализует расчёты лунного дня, фазы, расстояния, зодиака.
- **Комментарии и UI** — на русском языке.
- **Nullable** включён во всех проектах.
- **ImplicitUsings** включён в BonfireDB и Tests.
- Тесты покрывают только валидацию `SeedsFromViewModel` (вес, количество, дата годности).

## Первоначальная настройка (новый клон)

```powershell
# Активировать git-хук запуска тестов перед коммитом
git config core.hooksPath .githooks
```

Хук `.githooks/pre-commit` блокирует коммит если `dotnet test` упал.  
Обойти (не рекомендуется): `git commit --no-verify`

## Команды разработки

```powershell
# Сборка
dotnet build Bonfire.sln

# Запуск тестов
dotnet test Tests/SeedsFromViewModel.Tests/SeedsFromViewModel.Tests.csproj

# Новая EF-миграция (из корня решения)
dotnet ef migrations add <Name> --project BonfireDB --startup-project Bonfire

# Применить миграции вручную
dotnet ef database update --project BonfireDB --startup-project Bonfire
```

## Соглашения

- Новые сущности добавляются в `BonfireDB/Entities/`, регистрируются в `DbBonfire.cs` как `DbSet<T>`.
- Для каждой новой сущности создаётся репозиторий в `BonfireDB/Repository/` и регистрируется в `RepositoryRegister`.
- Сервисы в `Bonfire/Services/` работают через интерфейсы (`Interfaces/`), конкретные реализации регистрируются в `ServiceRegister`.
- VM-модели (DTO) хранятся в `Bonfire/Models/` и называются `*FromViewModel`.
- Для маппинга entity ↔ DTO использовать AutoMapper-профили.
