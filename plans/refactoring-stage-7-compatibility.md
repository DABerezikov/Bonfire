# Аудит удаления совместимости этапа 7

## Семена и рассада

Проверки вызова Add/Update/Remove моком удалены вместе с неиспользуемыми EF-API.
Они проверяли внутренний механизм, который больше не является контрактом приложения.
Ни один тест актуальных операций не исключён и не пропущен.

| Прежние операции | Актуальные доказательства поведения |
| --- | --- |
| MakeASeed, новый/существующий граф | SeedAcquisitionIntegrationTests, ServiceUnitOfWorkIntegrationTests: полный граф, повторное использование растения, разные годы/производители, остатки |
| UpdateSeed / DeleteSeed | SeedEditorIntegrationTests, SeedDeletionIntegrationTests: пакет, общий сорт, причина, атомарный отказ/сохранность истории |
| UpdateSort/Culture/Producer | SortEditingIntegrationTests, CultureEditingIntegrationTests, ProducerEditingIntegrationTests |
| MakeASeedling / UpdateSeedling | SowingIntegrationTests, SeedlingEditingIntegrationTests, ServiceUnitOfWorkIntegrationTests: самостоятельность партии, FK без дубликатов |
| AddSeedlingInfo / UpdateSeedlingInfo | SeedlingLifecycleIntegrationTests: всходы, существующие/новые пересадки, история |
| MarkSeedlingInfosDeadAsync | SeedlingLifecycleIntegrationTests: гибель, причина, единая запись, отказ БД |
| DeleteSeedling / ReturnSeedsFromSeedling | SeedlingDeletionIntegrationTests: Discard / CorrectSowing, возврат, отсутствующий пакет, rollback |

Старые чтения EF в проверках сохранения заменены явно названными LoadAsync/LoadAllAsync
тестовой SQLite-фикстуры: это независимое чтение БД для arrange/assert, не новый
продуктовый API. Рабочие чтения проверяются SeedReadingTests/SeedlingReadingTests.

Удалённые проверки реализации (точный перечень):

- `SeedsServiceTests.MakeASeed_NewCulture_AddsCulture`
- `SeedsServiceTests.MakeASeed_ExistingCulture_SkipsCultureAdd`
- `SeedsServiceTests.MakeASeed_NewProducer_AddsProducer`
- `SeedsServiceTests.MakeASeed_NewSort_AddsSort`
- `SeedsServiceTests.MakeASeed_NewPlant_AddsPlant`
- `SeedsServiceTests.MakeASeed_ExistingPlant_SkipsPlantAdd`
- `SeedsServiceTests.MakeASeed_NewSeedsInfo_AddsSeedsInfo`
- `SeedsServiceTests.MakeASeed_SetsBidirectionalRelationship`
- `SeedsServiceTests.UpdateSeed_CallsRepositoryUpdateAsync`
- `SeedsServiceTests.UpdateSeed_ReturnsSameSeed`
- `SeedsServiceTests.DeleteSeed_CallsRepositoryRemoveAsync`
- `SeedsServiceTests.UpdateSort_CallsRepositoryUpdateAsync`
- `SeedsServiceTests.UpdateCulture_CallsRepositoryUpdateAsync`
- `SeedsServiceTests.UpdateProducer_CallsRepositoryUpdateAsync`
- `SeedlingsServiceTests.MakeASeedling_AddsSeedlingGraphAndSavesOnce`
- `SeedlingsServiceTests.UpdateSeedling_CallsRepositoryUpdate`
- `SeedlingsServiceTests.DeleteSeedling_CallsRepositoryRemove`
- `SeedlingsServiceTests.AddSeedlingInfo_CallsRepositoryAdd`
- `SeedlingsServiceTests.UpdateSeedlingInfo_NoReplants_OnlyUpdates`
- `SeedlingsServiceTests.UpdateSeedlingInfo_WithNewReplant_AddsReplant`
- `SeedlingsServiceTests.UpdateSeedlingInfo_WithExistingReplant_SkipsAdd`
- `SeedlingsServiceTests.UpdateSeedlingInfo_MixedReplants_AddsOnlyNew`
- `SeedlingsServiceTests.MarkSeedlingInfosDeadAsync_SetsIsDeadAndNoteOnEachInfo`
- `SeedlingsServiceTests.MarkSeedlingInfosDeadAsync_UpdatesInfosAndSeedlingThenSavesOnce`


## Планировщик

Удалены EF CRUD/полная запись графа/прямые переходы и сетка без производственных
вызовов. 32 старые проверки реализации заменяются существующими более подробными
GardenCreationWritingTests, GardenObjectCreationWritingTests, GridResizingIntegrationTests,
GardenStateWritingIntegrationTests, *PropertiesWritingTests и *DeletionWritingTests.
Сквозной GardenLifecycleIntegrationTests переведён на актуальные запросы/результаты.
Сортировка/фильтрация/граф чтения остаются проверены реальным провайдером.
Отдельного UI редактирования плана и произвольной записи даты уборки не было;
удаление этих общих EF-методов не вводит и не отменяет пользовательских команд.

Точный перечень удалённых проверок старого механизма:

- `GardenServiceTests.CreatePlanAsync_CallsPlansAddAsync`
- `GardenServiceTests.CreatePlanAsync_SetsCorrectNameAndYear`
- `GardenServiceTests.DeletePlanAsync_CallsPlansRemoveAsyncWithPlanId`
- `GardenServiceTests.CreateGardenAsync_CallsGardensAddAsync`
- `GardenServiceTests.CreateGardenAsync_SetsCanvasFromMetersAndDefaultScale`
- `GardenServiceTests.CreateGardenAsync_SetsCanvasFromMetersAndCustomScale`
- `GardenServiceTests.CreateGardenAsync_SetsGardenPlanId`
- `GardenServiceTests.AddGreenhouseAsync_CallsGreenhousesAddAsync`
- `GardenServiceTests.AddGreenhouseAsync_SetsDisplayWidthAndHeight`
- `GardenServiceTests.AddGreenhouseAsync_SetsCanvasWidthAndHeight`
- `GardenServiceTests.ChangeElementStateAsync_ValidTransition_UpdatesStateTypeName`
- `GardenServiceTests.ChangeElementStateAsync_ValidTransition_CallsElementsUpdateAsync`
- `GardenServiceTests.ChangeElementStateAsync_InvalidTransition_ThrowsInvalidOperationException`
- `GardenServiceTests.ChangeElementStateAsync_InvalidTransition_DoesNotCallUpdateAsync`
- `GardenServiceTests.RebuildGridAsync_ActiveState_ThrowsInvalidOperationException`
- `GardenServiceTests.RebuildGridAsync_ActiveState_DoesNotCallElementsUpdateAsync`
- `GardenServiceTests.RebuildGridAsync_PlannedState_SetsGridRowsAndColumns`
- `GardenServiceTests.RebuildGridAsync_PlannedState_CallsElementsUpdateAsync`
- `GardenServiceTests.RebuildGridAsync_RemovesOutOfBoundsSpots`
- `GardenServiceTests.RebuildGridAsync_CreatesNewSpotsForMissingCells`
- `GardenServiceTests.RebuildGridAsync_DoesNotDuplicateExistingSpots`
- `GardenServiceTests.ClearSpotAsync_ResetsStateTypeName`
- `GardenServiceTests.ClearSpotAsync_ClearsSeedlingInfoId`
- `GardenServiceTests.ClearSpotAsync_ClearsPlantedDate`
- `GardenServiceTests.ClearSpotAsync_ClearsHarvestDate`
- `GardenServiceTests.ClearSpotAsync_CallsSpotsUpdateAsync`
- `GardenServiceTests.ChangeSpotStateAsync_TransitionsState`
- `GardenServiceTests.ChangeSpotStateAsync_WithLabel_SetsNote`
- `GardenServiceTests.ChangeSpotStateAsync_WithoutLabel_DoesNotChangeNote`
- `GardenServiceTests.ChangeSpotStateAsync_WithDate_SetsPlantedDate`
- `GardenServiceTests.ChangeSpotStateAsync_WithoutDate_DoesNotChangePlantedDate`
- `GardenServiceTests.ChangeSpotStateAsync_CallsSpotsUpdateAsync`
