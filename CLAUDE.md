# Bonfire — Система управления огородом

## Язык общения

Всегда общаться с пользователем на **русском языке**.

## Назначение

Десктопное WPF-приложение для ведения записей об огороде: учёт семян, рассады, производителей, лунного календаря и визуальное планирование огорода.

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
│   │   └── GardenPlan/        # Планировщик огорода (GardenPlanView, элементы управления)
│   ├── ViewModels/            # VM: MainWindow, Seeds, Seedlings, LibraryEditor, GardenPlan
│   ├── Models/                # DTO между слоями (SeedsFromViewModel и др.)
│   ├── Services/              # Бизнес-логика (ISeedsService, ISeedlingsService, IGardenService)
│   ├── Infrastructure/        # Commands (LambdaCommand, LambdaCommandAsync), Converters
│   ├── Templates/             # DataGrid-стили, Behaviors
│   ├── Data/                  # DbInitializer, DbRegistrator, PlantClassList
│   └── App.xaml.cs            # Точка входа, настройка IHost
│
├── BonfireDB/                 # Библиотека данных (net10.0)
│   ├── Context/DbBonfire.cs   # EF Core DbContext
│   ├── Entities/              # Доменные сущности + базовые классы
│   │   └── GardenPlanning/    # Сущности планирования огорода
│   ├── Repository/            # DbRepository<T> + специализированные репозитории
│   └── Migrations/            # 9 EF-миграций (история с 2023-10)
│
└── Tests/                     # XUnit-проекты (net10.0-windows)
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

### Семена и рассада

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

### Планирование огорода (`BonfireDB/Entities/GardenPlanning/`)

| Сущность | Описание |
|---|---|
| `GardenPlan` | План огорода на год (содержит список `Garden`) |
| `GardenPlot` | Абстрактный контейнер с реальными размерами и холстом (TPH) |
| `Garden` | Участок огорода — корневой контейнер, ссылается на `GardenPlan` |
| `Greenhouse` | Теплица — наследует `GardenPlot`, позиционируется на родительском участке |
| `GardenElement` | Абстрактный элемент, размещаемый на `GardenPlot` (TPH) |
| `Bed` | Грядка (поле `Orientation`) |
| `ColdFrame` | Парник (поле `CoverMaterial`) |
| `FlowerBed` | Цветник (поле `Shape`) |
| `OpenGroundArea` | Открытый грунт |
| `PlantingSpot` | Ячейка в сетке посадок элемента, может содержать `SeedlingInfo` |

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
- **ImplicitUsings** включён в BonfireDB и Tests; в **Bonfire** — отключён (нужны явные `using`).
- Тесты покрывают валидацию `SeedsFromViewModel`, модели, сервисы, репозитории и VM.

### Планировщик огорода

- **Паттерн State** — состояния элементов и ячеек реализованы через полиморфизм (без enum):
  - `GardenElementState` (Planned → Prepared → Active → Fallow / Resting → Archived)
  - `PlantingSpotState` (Empty → Reserved / Planted → Harvested / Dead)
  - Хранится как строка `StateTypeName` в БД; `[NotMapped] State` вычисляется через фабрику `From(typeName)`.
- **Паттерн Composite** — `Greenhouse : GardenPlot` сама является контейнером и одновременно позиционируется на родительском участке через `X, Y, DisplayWidth, DisplayHeight`.
- **TPH (Table Per Hierarchy)**:
  - `GardenPlots` — дискриминатор `PlotType` (`"Garden"` / `"Greenhouse"`).
  - `GardenElements` — дискриминатор `ElementType` (`"Bed"` / `"ColdFrame"` / `"FlowerBed"` / `"OpenGroundArea"`).
- **Ограничение `IRepository<T>`** — убран `new()` из generic-ограничения (абстрактный `GardenElement` не может его удовлетворить). `GardenElementRepository` реализует интерфейс напрямую без `DbRepository<T>`.
- **Конвертеры** (`Bonfire/Infrastructure/Converters/`):
  - `NullToVisibilityConverter` / `NotNullToVisibilityConverter`
  - `StringToBrushConverter` — преобразует hex-строку в `SolidColorBrush`
- **Масштаб** — при создании участка/теплицы: `CanvasWidth = WidthMeters × scale` (по умолчанию `scale = 40` пикс/м).
- **EF Tools** — глобальный инструмент `dotnet-ef` скомпилирован под `net8.0` и при поиске Design-сборки выбирает `Microsoft.EntityFrameworkCore.Design 8.0.0` (у него есть `lib/net8.0`) вместо `10.0.8` (только `lib/net10.0`). Решение: **оба** проекта — `BonfireDB` и `Bonfire` — должны явно ссылаться на `Microsoft.EntityFrameworkCore.Design 10.0.8` с `PrivateAssets=all`. Тогда пакет попадает в `deps.json` стартового проекта и инструмент берёт нужную версию. Миграции также применяются автоматически при старте через `db.Database.MigrateAsync()`. Для инструментов добавлена `DbBonfireDesignTimeFactory`.

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
- В проекте **Bonfire** нет `ImplicitUsings` — все `using` в `.cs`-файлах пишутся явно.
- Если абстрактный тип не может удовлетворить `new()` constraint, реализуйте `IRepository<T>` напрямую (как `GardenElementRepository`).
- Новые состояния для State Pattern добавляются в `BonfireDB/Entities/GardenPlanning/States/` или `SpotStates/`, и регистрируются в фабрике `GardenElementState.From()` / `PlantingSpotState.From()`.
