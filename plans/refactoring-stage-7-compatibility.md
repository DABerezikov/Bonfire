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
