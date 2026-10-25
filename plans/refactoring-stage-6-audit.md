# Аудит этапа 6: посадки и план огорода

Дата: 25.10.2026. Проверяемая ветка: feature/refactoring-plans.

## Производственные пути записи

Все вызовы записи GardenPlanViewModel проверены через rg и вызывающий XAML.

| Сценарий UI | Application / порт |
| --- | --- |
| План: создать / удалить | CreateGardenPlan / DbGardenPlanCreationWriter; DeleteGardenPlan / DbGardenPlanDeletionWriter |
| Участок: создать / изменить / удалить | CreateGarden / DbGardenCreationWriter; EditGardenProperties / DbGardenPropertiesWriter; DeleteGarden / DbGardenDeletionWriter |
| Элемент/теплица: создать | CreateGardenObjects / DbGardenObjectCreationWriter |
| Элемент/теплица: свойства | EditElementProperties / DbElementPropertiesWriter; EditGreenhouseProperties / DbGreenhousePropertiesWriter |
| Удалить элемент/теплицу | DeleteGardenElement / DbElementDeletionWriter; DeleteGreenhouse / DbGreenhouseDeletionWriter |
| Геометрия жеста / блокировка | SaveGardenGeometry / DbGardenGeometryWriter; SetGardenLock / DbGardenLockWriter |
| Состояние элемента/теплицы/ячейки, очистка | ChangeGardenState / DbGardenStateWriter |
| Сетка | ResizeGrid / DbGridWriter |
| Посадка семян/рассады | PlantInSpot / DbPlantingWriter |

Запись принимает проверенные запросы, возвращает DTO внутри Result; порты читают
свежие данные в коротком Uow. UI применяет результат после успеха, черновики
независимы; сохранены история, каскады/Restrict, TPH, строки состояний и Composite.
GetPlansOrderedByYearDescAsync/GetGardensByPlanAsync пока возвращают EF-графы для
чтения на внешней границе; их замена и удаление старых API — этап 7.

## Итоговая регрессия

- LoadGardens: Red 2 failed подтвердил старый ответ другого плана и незакрытые
  ссылки при снятии выбора; теперь старый ответ игнорируется, собственные редакторы
  закрываются перед заменой списка. TRX: artifacts/stage6-audit-loading-red.
- Реальная SQLite: сохранённый план с двумя подтипами элементов, теплицей,
  внутренним парником и ячейкой/историей читается через производственные DI/репозитории.
  XAML показывает оба холста, редакторы и сетку. Строгая проверка DI включена.
- Рендеры WPF 1600×900 DIPs при 96/144 DPI: основной участок, теплица, сетка.
  PNG находятся в Tests/ViewModels.Tests/bin/Debug/net10.0-windows/visual/stage6/.
  Основной вид, оверлей теплицы и сетка просмотрены; первый рендер выявил ошибку
  фикстуры выбора (модель отсутствовала в Gardens), фикстура исправлена.
- Общий гейт, жесты и процессный startup/exit smoke — результаты в журнале.

## Сохранённые особенности и ограничения

Clamp до snap 15 DIPs/ToEven, минимум 15/150 при resize, разрешение исходных
коллизий и игнорирование Rotation сохранены. Создание проверяет свежие границы,
жесты не получили нового строгого ограничения края. Ноль/пустой текст не
нормализованы. Area=0 и отсутствие ячеек при начальной сетке 1×1 сохранены.

Гонка между чтением и SaveChanges не получила сериализуемой защиты. Рендер 144 DPI
проверяет компоновку/растеризацию, но не заменяет ручной перенос окна между
мониторами и системные диалоги. Coverlet повторно не запускался. Тесты используют
изолированные БД; схема, пакеты и рабочая БД не менялись. Предупреждения NuGet и
исторических миграций PRAGMA известны и не относятся к изменению.
