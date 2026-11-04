# Локальное обновление после push

Выбранная команда: git publish (локальный alias этого репозитория).

План:
1. Проверить master, чистоту отслеживаемых файлов и origin Bonfire. Взять блокировку
   на локальную публикацию, чтобы параллельные команды не выполняли две установки.
2. Push конкретного SHA в origin/master. При отказе — не собирать и не обновлять.
3. Архивировать именно этот коммит в отдельную временную папку. Проверить remote SHA.
4. На снимке выполнить полный Release-гейт, затем FolderProfile publish и SingleFile check.
   Перед установкой ещё раз проверить актуальность SHA на origin/master.
5. Штатный Update-BonfireInteractive: блокировки, бэкап, SHA файлов, сохранение БД/настроек,
   подтверждение закрытия открытой копии, запись .bonfire-version.json. Отказ сохраняет установку.
6. Удалить только собственный временный снимок после успеха; при отказе оставить для диагностики.
7. Отключить облачный deploy, оставить build/test/artifact. Установить alias локально.

Псевдокод:
```
state = repo(master, clean tracked, expected origin)
using local publish lock:
    push(state.sha) or failure(push=false)
    remote must equal state.sha
    snapshot = git archive(state.sha)
    test(snapshot) -> publish(snapshot, SourceRevisionId=sha) -> SingleFile check
    remote must still equal sha
    update(publish, sha) // existing script; no direct file copying
    cleanup owned temporary snapshot on success
```

Критерии: ошибки push/test/publish/новый remote SHA не приводят к установке;
используется сохранённый SHA даже при последующих правках/коммитах; пользовательская
БД не меняется; повторный вызов уже установленного SHA не создаёт новый бэкап.
Не требуется фоновая служба, глобальный перехват git или post-push hook.


Реализовано: scripts/Publish-Bonfire.ps1, alias git publish, cloud deploy удалён.
LocalPublish.Tests: 9 сценариев. LocalPublish.Git.Tests: настоящий git init/commit/archive,
проверки master/dirty/fetch/push URL, исключение untracked, параллельная блокировка,
неизменяемость снимка после следующего коммита и вызов alias из вложенной папки.
Ранее запущенный облачный workflow проверен: завершён Cancelled, install-lock отсутствует.
Временный снимок после отказа сохраняется, после успеха очищается только собственный GUID.


Первый сквозной вызов успешно отправил SHA и выполнил 1875 тестов снимка, затем
остановился на вложенной Git-фикстуре до публикации/установки. Выявлено различие
PowerShell location и Environment.CurrentDirectory: относительный git-common-dir
теперь разрешается от git rev-parse --show-toplevel. Добавлен прямой assert каталога
блокировки фикстуры. Рабочий репозиторий и его настройки проверены, изменений нет.
