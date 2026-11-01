# Этап 6. Сценарии Application и пересчёт

Статус: план. Зависимости: этапы [2](02-libraries.md)–[5](05-storage.md).
[Общий план](README.md).

## Цель и файлы

Соединить справочники, чистые расчёты, Луну и хранение без EF/WPF в Application.
Расчёт является просмотром; запись — отдельная явная команда.

Новые: `Bonfire.Application/Planning/` — ReadPlantingPlans, CreatePlantingPlan,
CalculatePlannedBatch, SavePlannedBatch, RecalculatePlannedBatch,
DeletePlannedBatch, DeletePlantingPlan, порты и DTO.
Новые фасад/маппер: `Bonfire/Services/PlanningService.cs`,
`Interfaces/IPlanningService.cs`, `Bonfire/Models/Mappers/PlanningMapper.cs`.
Изменить: `Bonfire/Services/ServiceRegister.cs`, `BonfireDB/RepositoryRegister.cs`.
Проверки: `Tests/Services.Tests/PlanningApplicationTests.cs`, CompositionBehaviorTests.

## Контракты и результаты

- Calculate принимает независимый ввод и загружает актуальную справку через порты.
  Возвращает Result<PlanningPreviewFromViewModel>, без записи.
- Неполный, но структурно корректный ввод возвращает preview с MissingInput,
  без ложного рассчитанного окна. Ошибка диапазона/доступа возвращает Failure.
- Preview содержит InputFingerprint/версию параметров. Изменившийся ввод нельзя
  сохранить со старым preview как будто расчёт ему соответствует.
- Сохранение повторно валидирует вход и актуальность источников/версии; при
  изменении справки предлагает новый preview либо сохраняет явно выбранный
  исторический снимок. Не подменяет профиль незаметно.
- Время расчёта берётся через существующий TimeProvider. Локальная дата региона
  определяется его зоной. Будущий срок можно пометить просроченным, но нельзя
  переносить на сегодня автоматически.

## Псевдокод

```text
Calculate(request, cancellation):
    ValidateIdentityAndDateWindow(request)
    context = await profileReader.LoadFresh(request.CultureId, request.SortId,
                                          request.RegionId, request.Environment)
    effective = ResolveParameters(request.Overrides, context)
    if effective.HasMissingRequiredInputs:
        return Success(IncompletePreview(context.Reference, effective.Diagnostics))
    schedule = Domain.Calculate(request, effective)
    events = await lunarEvents.Read(schedule.SearchWindow, context.TimeZone, cancellation)
    recommendations = Domain.Recommend(schedule, effective.LunarGroup, events)
    return Success(Preview(schedule, recommendations, context.Versions,
                           Fingerprint(request), timeProvider.GetUtcNow()))

Save(request):
    ValidatePreviewMatchesInput(request)
    RequireCompleteCoreCalculation(request)
    // ручные даты вне окон допустимы, неверный порядок событий — нет
    validated = PlannedBatch.Create(request.InputSnapshot, request.ChosenDates,
                                    request.CalculationSnapshot)
    return await writer.SaveAsync(validated, request.ExpectedVersion)

Recalculate(savedBatch, currentDraft):
    preview = await Calculate(currentDraft.WithCurrentReferences())
    proposal = Merge(preview,
        preserveManualDates=true, preserveManualSeedCount=true)
    return proposal  // запись только последующей командой Save
```

После пересчёта рекомендованная дата может смениться, но прежний ручной выбор
остаётся с актуальной диагностикой. Не переписывать редактируемый черновик
результатом запроса с более старым Fingerprint. Справка всегда содержит два условия,
рабочий расчёт относится только к выбранному условию партии.

## Факты и границы первой версии

После ответа пользователя явно подтверждено: пока только планирование. Чтение
фактических всходов и автопересчёт по ним не включать в Calculate/Recalculate.
Пользователь по-прежнему может вручную менять плановые параметры.

Calculate ветвится по способу: для Transplant требует окно высадки и Age,
для DirectSow — окно посева без Age. Результат содержит только применимые этапы,
а также прогноз зрелости при известном V от всходов. Подсказки отсутствующих данных
и fingerprint учитывают способ. Смена способа не переносит окно высадки в посев
и не принимает устаревший preview. Добавить сценарные проверки обоих вариантов.

План можно открыть независимо от наличия пакета семян. Его создание, копирование,
пересчёт и удаление не вызывают SowSeeds или PlantInSpot.
Автоматическая отметка выполнения, синхронизация плана с фактическими всходами
и атомарный «посев из плана» требуют дополнительного контракта и не реализуются
путём последовательных несогласованных вызовов SavePlan → SowSeeds.
Если в UI будет переход к существующему посеву, он только открывает независимый
ввод с предложенными датой/количеством и требует выбора реального пакета.
Сам переход не считается выполнением плана.

## Последовательность и проверки

1. Red/Green: fake-порты, сценарий полного расчёта, отсутствие записи при preview.
2. Неполная справка, несовпадение культуры/сорта, удалённый/архивный регион.
3. Лунный конфликт является успешным preview с диагностикой, а не исключением.
   Отказ поставщика даёт недоступность рекомендаций, сохраняя агрономический расчёт.
4. Сохранение ручной даты за окном, запрет даты пикировки раньше посева,
   сохранение выбранных режимов и их источников.
5. Поздний preview, изменение справки, конфликт версии, отмена через CancellationToken.
6. Пересчёт сохраняет ручные значения, не меняет БД до Save и не списывает семена.
7. DI: проверка всех новых портов существующим composition root,
   без захвата scoped DbContext в singleton-фасаде.

Гейт: Services.Tests и профильные Models.Tests. Готовность: каждый сценарий
доступен через интерфейс, сохранение отдельно от расчёта, ошибки и неполнота явные.

## Дополнение после окончательных ответов

Подключить вместимость по размерам грядки и обратный расчёт урожая (9), ручной
журнал сборов (10), многолетники и периоды (11). Ограничение интеграции с фактами
выше относится к реальным посевам/всходам/высадкам, а не к разрешённому журналу.
Сохранение сбора не вызывает SowSeeds/PlantInSpot, не списывает семена и не
переписывает исходный прогноз. Генерации повторных волн и импорта Excel нет.
