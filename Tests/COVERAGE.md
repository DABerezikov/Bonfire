# Покрытие исполняемых строк

Числовая таблица ниже — исторический срез; после планирования посадок Coverlet
не пересчитывался. Новый гейт — 1757/1757, пять проектов. Сценарии и пределы
доказательств: [аудит планирования](../plans/planting-planning/implementation-audit.md).

Миграции, сгенерированный код и XAML исключены из числовой метрики. Code-behind включён.
Строка считается покрытой, если выполнена хотя бы одним тестовым проектом; проценты проектов не усредняются.

| Область | До | После |
|---|---:|---:|
| Весь runtime C# | 2020/5026 (40.2%) | 4430/5026 (88.1%) |
| Bonfire | 1616/4447 (36.3%) | 3902/4447 (87.7%) |
| Модели и маппинг | 571/744 (76.7%) | 709/744 (95.3%) |
| Сервисы | 503/920 (54.7%) | 774/920 (84.1%) |
| ViewModels | 482/2113 (22.8%) | 1994/2113 (94.4%) |
| Инфраструктура | 58/155 (37.4%) | 138/155 (89.0%) |
| WPF code-behind | 0/273 (0.0%) | 126/273 (46.2%) |
| BonfireDB | 368/473 (77.8%) | 425/473 (89.9%) |
| MoonCalendar | 36/106 (34.0%) | 103/106 (97.2%) |

## По файлам

| Файл | До | После |
|---|---:|---:|
| `Bonfire/App.xaml.cs` | 0/55 (0.0%) | 2/55 (3.6%) |
| `Bonfire/Data/DbInitializer.cs` | 0/29 (0.0%) | 29/29 (100.0%) |
| `Bonfire/Data/DbRegistrator.cs` | 0/17 (0.0%) | 17/17 (100.0%) |
| `Bonfire/Data/PlantClassList.cs` | 2/2 (100.0%) | 2/2 (100.0%) |
| `Bonfire/Infrastructure/CanvasConstants.cs` | 0/9 (0.0%) | 9/9 (100.0%) |
| `Bonfire/Infrastructure/CollisionHelper.cs` | 34/56 (60.7%) | 42/56 (75.0%) |
| `Bonfire/Infrastructure/Commands/Base/Command.cs` | 0/21 (0.0%) | 19/21 (90.5%) |
| `Bonfire/Infrastructure/Commands/Base/CommandAsync.cs` | 18/26 (69.2%) | 25/26 (96.2%) |
| `Bonfire/Infrastructure/Commands/Base/CommandExceptionHandler.cs` | 0/2 (0.0%) | 2/2 (100.0%) |
| `Bonfire/Infrastructure/Commands/CloseWindow.cs` | 0/3 (0.0%) | 3/3 (100.0%) |
| `Bonfire/Infrastructure/Commands/LambdaCommand.cs` | 0/6 (0.0%) | 6/6 (100.0%) |
| `Bonfire/Infrastructure/Commands/LambdaCommandAsync.cs` | 6/6 (100.0%) | 6/6 (100.0%) |
| `Bonfire/Infrastructure/Converters/NullToVisibilityConverter.cs` | 0/6 (0.0%) | 6/6 (100.0%) |
| `Bonfire/Infrastructure/Converters/PxToMetersConverter.cs` | 0/11 (0.0%) | 11/11 (100.0%) |
| `Bonfire/Infrastructure/Converters/StringToBrushConverter.cs` | 0/9 (0.0%) | 9/9 (100.0%) |
| `Bonfire/Models/AddSeedRequest.cs` | 12/12 (100.0%) | 12/12 (100.0%) |
| `Bonfire/Models/CultureEditModel.cs` | 17/24 (70.8%) | 23/24 (95.8%) |
| `Bonfire/Models/CultureFromViewModel.cs` | 3/3 (100.0%) | 3/3 (100.0%) |
| `Bonfire/Models/GardenElementFromViewModel.cs` | 87/112 (77.7%) | 107/112 (95.5%) |
| `Bonfire/Models/GardenFromViewModel.cs` | 12/17 (70.6%) | 17/17 (100.0%) |
| `Bonfire/Models/GreenhouseFromViewModel.cs` | 15/75 (20.0%) | 63/75 (84.0%) |
| `Bonfire/Models/Mappers/GardenPlanMapper.cs` | 67/104 (64.4%) | 96/104 (92.3%) |
| `Bonfire/Models/Mappers/LibraryMapper.cs` | 49/59 (83.1%) | 59/59 (100.0%) |
| `Bonfire/Models/Mappers/SeedMapper.cs` | 73/73 (100.0%) | 73/73 (100.0%) |
| `Bonfire/Models/Mappers/SeedlingMapper.cs` | 31/31 (100.0%) | 31/31 (100.0%) |
| `Bonfire/Models/PlantFromViewModel.cs` | 3/6 (50.0%) | 6/6 (100.0%) |
| `Bonfire/Models/PlantSourceItem.cs` | 4/7 (57.1%) | 6/7 (85.7%) |
| `Bonfire/Models/PlantingSpotFromViewModel.cs` | 34/37 (91.9%) | 34/37 (91.9%) |
| `Bonfire/Models/ProducerFromViewModel.cs` | 3/3 (100.0%) | 3/3 (100.0%) |
| `Bonfire/Models/SeedEditorOptions.cs` | 4/4 (100.0%) | 4/4 (100.0%) |
| `Bonfire/Models/SeedlingFromViewModel.cs` | 24/28 (85.7%) | 28/28 (100.0%) |
| `Bonfire/Models/SeedlingInfoFromViewModel.cs` | 2/6 (33.3%) | 6/6 (100.0%) |
| `Bonfire/Models/SeedsFromViewModel.cs` | 15/15 (100.0%) | 15/15 (100.0%) |
| `Bonfire/Models/SortEditModel.cs` | 110/121 (90.9%) | 116/121 (95.9%) |
| `Bonfire/Models/SortFromSeedlingsViewModel.cs` | 3/4 (75.0%) | 4/4 (100.0%) |
| `Bonfire/Models/SortFromSeedsViewModel.cs` | 3/3 (100.0%) | 3/3 (100.0%) |
| `Bonfire/Program.cs` | 0/8 (0.0%) | 0/8 (0.0%) |
| `Bonfire/Services/Extensions/ObservableCollectionExtensions.cs` | 4/4 (100.0%) | 4/4 (100.0%) |
| `Bonfire/Services/Extensions/StringExtensions.cs` | 16/16 (100.0%) | 16/16 (100.0%) |
| `Bonfire/Services/Extensions/ViewModelsExtensions.cs` | 7/7 (100.0%) | 7/7 (100.0%) |
| `Bonfire/Services/GardenService.cs` | 123/204 (60.3%) | 189/204 (92.6%) |
| `Bonfire/Services/Interfaces/IPlantingService.cs` | 10/10 (100.0%) | 10/10 (100.0%) |
| `Bonfire/Services/Interfaces/IReportService.cs` | 0/7 (0.0%) | 7/7 (100.0%) |
| `Bonfire/Services/LibraryService.cs` | 69/88 (78.4%) | 87/88 (98.9%) |
| `Bonfire/Services/PlantingService.cs` | 68/68 (100.0%) | 68/68 (100.0%) |
| `Bonfire/Services/ReportService.cs` | 0/171 (0.0%) | 171/171 (100.0%) |
| `Bonfire/Services/SeedlingAvailability.cs` | 2/2 (100.0%) | 2/2 (100.0%) |
| `Bonfire/Services/SeedlingsService.cs` | 50/54 (92.6%) | 50/54 (92.6%) |
| `Bonfire/Services/SeedsService.cs` | 154/183 (84.2%) | 154/183 (84.2%) |
| `Bonfire/Services/ServiceRegister.cs` | 0/9 (0.0%) | 9/9 (100.0%) |
| `Bonfire/Services/UserDialog.cs` | 0/97 (0.0%) | 0/97 (0.0%) |
| `Bonfire/Templates/BehaviorInStyleAttacher.cs` | 0/24 (0.0%) | 23/24 (95.8%) |
| `Bonfire/Templates/DoubleClickBehavior.cs` | 0/35 (0.0%) | 29/35 (82.9%) |
| `Bonfire/Templates/ScrollIntoViewBehavior.cs` | 0/30 (0.0%) | 22/30 (73.3%) |
| `Bonfire/Templates/SelectAllTextOnFocusBehavior.cs` | 0/23 (0.0%) | 18/23 (78.3%) |
| `Bonfire/Templates/SingleRowDetailsBehavior.cs` | 0/19 (0.0%) | 19/19 (100.0%) |
| `Bonfire/ViewModels/Base/SourceSelectionViewModel.cs` | 9/14 (64.3%) | 13/14 (92.9%) |
| `Bonfire/ViewModels/Base/ViewModel.cs` | 7/7 (100.0%) | 7/7 (100.0%) |
| `Bonfire/ViewModels/GardenPlanViewModel.cs` | 31/719 (4.3%) | 696/719 (96.8%) |
| `Bonfire/ViewModels/LibraryEditorViewModel.cs` | 70/172 (40.7%) | 160/172 (93.0%) |
| `Bonfire/ViewModels/MainWindowViewModel.cs` | 0/94 (0.0%) | 82/94 (87.2%) |
| `Bonfire/ViewModels/SeedlingsViewModel.cs` | 152/621 (24.5%) | 572/621 (92.1%) |
| `Bonfire/ViewModels/SeedsViewModel.cs` | 213/476 (44.7%) | 458/476 (96.2%) |
| `Bonfire/ViewModels/ViewModelLocator.cs` | 0/4 (0.0%) | 0/4 (0.0%) |
| `Bonfire/ViewModels/ViewModelRegister.cs` | 0/6 (0.0%) | 6/6 (100.0%) |
| `Bonfire/Views/GardenPlan/GardenElementControl.xaml.cs` | 0/70 (0.0%) | 48/70 (68.6%) |
| `Bonfire/Views/GardenPlan/GardenPlanView.xaml.cs` | 0/26 (0.0%) | 5/26 (19.2%) |
| `Bonfire/Views/GardenPlan/GreenhouseControl.xaml.cs` | 0/59 (0.0%) | 27/59 (45.8%) |
| `Bonfire/Views/GardenPlan/PlantingGridView.xaml.cs` | 0/4 (0.0%) | 4/4 (100.0%) |
| `Bonfire/Views/LibraryEditorWindow.xaml.cs` | 0/4 (0.0%) | 4/4 (100.0%) |
| `Bonfire/Views/SeedDetailsEditor.xaml.cs` | 0/12 (0.0%) | 8/12 (66.7%) |
| `Bonfire/Views/SeedlingsWindow.xaml.cs` | 0/31 (0.0%) | 4/31 (12.9%) |
| `Bonfire/Views/SeedsWindow.xaml.cs` | 0/57 (0.0%) | 26/57 (45.6%) |
| `Bonfire/Views/Windows/MainWindow.xaml.cs` | 0/10 (0.0%) | 0/10 (0.0%) |
| `BonfireDB/Context/DbBonfire.cs` | 62/62 (100.0%) | 62/62 (100.0%) |
| `BonfireDB/Context/DbBonfireDesignTimeFactory.cs` | 0/6 (0.0%) | 0/6 (0.0%) |
| `BonfireDB/DbRepository.cs` | 32/32 (100.0%) | 32/32 (100.0%) |
| `BonfireDB/Entities/Base/Entity.cs` | 1/1 (100.0%) | 1/1 (100.0%) |
| `BonfireDB/Entities/Base/NamedEntity.cs` | 1/1 (100.0%) | 1/1 (100.0%) |
| `BonfireDB/Entities/Base/Result.cs` | 16/37 (43.2%) | 36/37 (97.3%) |
| `BonfireDB/Entities/GardenPlanning/Bed.cs` | 1/1 (100.0%) | 1/1 (100.0%) |
| `BonfireDB/Entities/GardenPlanning/ColdFrame.cs` | 1/1 (100.0%) | 1/1 (100.0%) |
| `BonfireDB/Entities/GardenPlanning/FlowerBed.cs` | 0/1 (0.0%) | 1/1 (100.0%) |
| `BonfireDB/Entities/GardenPlanning/Garden.cs` | 3/3 (100.0%) | 3/3 (100.0%) |
| `BonfireDB/Entities/GardenPlanning/GardenElement.cs` | 22/23 (95.7%) | 23/23 (100.0%) |
| `BonfireDB/Entities/GardenPlanning/GardenPlan.cs` | 3/3 (100.0%) | 3/3 (100.0%) |
| `BonfireDB/Entities/GardenPlanning/GardenPlot.cs` | 7/7 (100.0%) | 7/7 (100.0%) |
| `BonfireDB/Entities/GardenPlanning/Greenhouse.cs` | 8/18 (44.4%) | 16/18 (88.9%) |
| `BonfireDB/Entities/GardenPlanning/PlantingSpot.cs` | 15/17 (88.2%) | 15/17 (88.2%) |
| `BonfireDB/Entities/GardenPlanning/SpotStates/DeadSpotState.cs` | 4/6 (66.7%) | 4/6 (66.7%) |
| `BonfireDB/Entities/GardenPlanning/SpotStates/EmptySpotState.cs` | 5/6 (83.3%) | 5/6 (83.3%) |
| `BonfireDB/Entities/GardenPlanning/SpotStates/HarvestedSpotState.cs` | 2/6 (33.3%) | 2/6 (33.3%) |
| `BonfireDB/Entities/GardenPlanning/SpotStates/PlantedSpotState.cs` | 4/6 (66.7%) | 4/6 (66.7%) |
| `BonfireDB/Entities/GardenPlanning/SpotStates/PlantingSpotState.cs` | 10/10 (100.0%) | 10/10 (100.0%) |
| `BonfireDB/Entities/GardenPlanning/SpotStates/ReservedSpotState.cs` | 2/6 (33.3%) | 2/6 (33.3%) |
| `BonfireDB/Entities/GardenPlanning/States/ActiveState.cs` | 4/5 (80.0%) | 4/5 (80.0%) |
| `BonfireDB/Entities/GardenPlanning/States/ArchivedState.cs` | 3/5 (60.0%) | 3/5 (60.0%) |
| `BonfireDB/Entities/GardenPlanning/States/FallowState.cs` | 3/5 (60.0%) | 3/5 (60.0%) |
| `BonfireDB/Entities/GardenPlanning/States/GardenElementState.cs` | 11/11 (100.0%) | 11/11 (100.0%) |
| `BonfireDB/Entities/GardenPlanning/States/PlannedState.cs` | 5/5 (100.0%) | 5/5 (100.0%) |
| `BonfireDB/Entities/GardenPlanning/States/PreparedState.cs` | 3/5 (60.0%) | 5/5 (100.0%) |
| `BonfireDB/Entities/GardenPlanning/States/RestingState.cs` | 3/5 (60.0%) | 3/5 (60.0%) |
| `BonfireDB/Entities/Plant.cs` | 2/2 (100.0%) | 2/2 (100.0%) |
| `BonfireDB/Entities/PlantCulture.cs` | 2/2 (100.0%) | 2/2 (100.0%) |
| `BonfireDB/Entities/PlantSort.cs` | 18/18 (100.0%) | 18/18 (100.0%) |
| `BonfireDB/Entities/Producer.cs` | 1/1 (100.0%) | 1/1 (100.0%) |
| `BonfireDB/Entities/Replanting.cs` | 2/4 (50.0%) | 2/4 (50.0%) |
| `BonfireDB/Entities/Seed.cs` | 3/3 (100.0%) | 3/3 (100.0%) |
| `BonfireDB/Entities/Seedling.cs` | 11/11 (100.0%) | 11/11 (100.0%) |
| `BonfireDB/Entities/SeedlingInfo.cs` | 10/17 (58.8%) | 12/17 (70.6%) |
| `BonfireDB/Entities/SeedsInfo.cs` | 11/11 (100.0%) | 11/11 (100.0%) |
| `BonfireDB/Entities/SortOption.cs` | 1/1 (100.0%) | 1/1 (100.0%) |
| `BonfireDB/Entities/Treatment.cs` | 0/4 (0.0%) | 1/4 (25.0%) |
| `BonfireDB/GardenElementRepository.cs` | 14/31 (45.2%) | 25/31 (80.6%) |
| `BonfireDB/GardenRepository.cs` | 9/9 (100.0%) | 9/9 (100.0%) |
| `BonfireDB/GreenhouseRepository.cs` | 0/7 (0.0%) | 7/7 (100.0%) |
| `BonfireDB/PlantRepository.cs` | 5/5 (100.0%) | 5/5 (100.0%) |
| `BonfireDB/PlantSortRepository.cs` | 4/4 (100.0%) | 4/4 (100.0%) |
| `BonfireDB/RepositoryRegister.cs` | 20/20 (100.0%) | 20/20 (100.0%) |
| `BonfireDB/SeedlingInfoRepository.cs` | 0/4 (0.0%) | 4/4 (100.0%) |
| `BonfireDB/SeedlingsRepository.cs` | 8/8 (100.0%) | 8/8 (100.0%) |
| `BonfireDB/SeedsRepository.cs` | 7/7 (100.0%) | 7/7 (100.0%) |
| `BonfireDB/UnitOfWork.cs` | 9/10 (90.0%) | 9/10 (90.0%) |
| `MoonCalendar/MoonPhase.cs` | 36/106 (34.0%) | 103/106 (97.2%) |
