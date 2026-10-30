# Установка runner на MONOBLOCK

Запустите `Install-BonfireRunner.ps1` **непосредственно на MONOBLOCK**, войдя под
тем пользователем Windows, который работает в Bonfire. Установка не требует
пароля Windows и не создаёт службу. Не используйте вход под другой учётной записью
администратора: автозапуск привязан к пользователю, запустившему установщик.

Откройте Windows PowerShell x64 и выполните:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File "\\MONOBLOCK\Bonfire\Install-BonfireRunner.ps1" -WhatIf
powershell -NoProfile -ExecutionPolicy Bypass -File "\\MONOBLOCK\Bonfire\Install-BonfireRunner.ps1"
```

Установщик скачивает официальный Windows x64 runner 2.338.2, проверяет SHA-256,
сверенный с GitHub Release API, и запрашивает временный **registration token**. Получите его
на странице [Settings → Actions → Runners → New self-hosted runner](https://github.com/DABerezikov/Bonfire/settings/actions/runners/new),
выбрав Windows / x64. Используйте значение после `--token` в команде `config`.
Токен действует один час; PAT не нужен. По согласованному варианту вставьте токен
на MONOBLOCK в поле в начале `Install-BonfireRunner.ps1`:

```powershell
$script:RegistrationTokenText = 'ВСТАВЬТЕ_ТОКЕН_ЗДЕСЬ'
```

После установки очистите это поле. Если оно пустое, остаётся скрытый запрос ввода.
В задачу Windows и командную строку токен не попадает: передаётся дочернему
процессу через временную переменную окружения, после регистрации она очищается.
Собственные служебные
credentials GitHub runner хранит в своём каталоге по штатным правилам.

Результат:

- Каталог: `%LOCALAPPDATA%\Bonfire\GitHubRunner`, отдельно от приложения и бэкапов.
- Runner: `MONOBLOCK-Bonfire`, репозиторий `DABerezikov/Bonfire`.
- Метки: `self-hosted`, `Windows`, `X64`, `bonfire-monoblock`, `interactive`.
- Задача `Bonfire-GitHubRunner-<SID>` запускается при входе этого пользователя,
  в его интерактивном сеансе, с обычными правами; первый запуск происходит сразу.
- Повторный запуск установщика использует существующую регистрацию. Чужие
  каталоги, службы и задачи не перезаписываются.

После установки проверьте **Idle** в GitHub → Settings → Actions → Runners.
При выходе пользователя из Windows runner недоступен. При блокировке экрана
задачи могут выполняться, но диалог будет доступен только после разблокировки.
Задачу можно остановить или отключить через Планировщик заданий. Для удаления
регистрации используйте штатный `config.cmd remove` с отдельным removal token
из GitHub; файлы и задачу удаляйте после остановки runner.

Если загрузка на MONOBLOCK обрывается, установщик берёт готовый архив из
`runner-install\actions-runner-win-x64-2.338.2.zip` рядом со скриптом, если он там
есть. Другой готовый архив можно указать явно:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File "\\MONOBLOCK\Bonfire\Install-BonfireRunner.ps1" -RunnerArchive "C:\Downloads\actions-runner-win-x64-2.338.2.zip"
```

SHA-256 проверяется и у локального архива. При сетевой загрузке используется
`curl.exe`, если он доступен; иначе — Windows PowerShell с TLS 1.2. Проверка
сертификатов остаётся включённой. После оборванной загрузки повторите запуск:
оставшийся ZIP не блокирует установку. Неизвестные файлы в каталоге runner
по-прежнему запрещают его перезапись.

Нужен доступ MONOBLOCK к GitHub по HTTPS. SDK .NET 10 и Git нужны для будущего
workflow сборки; установщик их не устанавливает. Workflow должен использовать
`runs-on: [self-hosted, Windows, X64, bonfire-monoblock, interactive]` и выполнять
развёртывания последовательно.

**Согласованная политика обновления:** если Bonfire закрыт, обновлять без диалога.
Если открыт — запрашивать подтверждение пользователя на MONOBLOCK перед закрытием;
отказ или отсутствие ответа не разрешают обновление. Принудительное завершение
процесса не разрешено. Проверяется путь исполняемого файла этой установки;
другие копии Bonfire не закрываются. Этот установщик
настраивает runner. Workflow `.github/workflows/deploy-monoblock.yml` собирает
и тестирует приложение на Windows GitHub, передаёт публикацию на MONOBLOCK и
вызывает `Update-BonfireInteractive.ps1`. Диалог оформлен в `UpdateConfirmation.xaml`.
При открытом приложении отказ или отсутствие ответа за 5 минут отменяют обновление.
Согласие разрешает только штатное закрытие; если Bonfire не закрылся за 30 секунд,
запись не начинается. Установка сама по себе не запускает новое приложение и
не применяет миграции БД. Результат записывается в `.bonfire-version.json`.

Проверка установщика без регистрации, скачивания runner и создания задач:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tests/Deployment/Runner.Tests.ps1
```

Основание: [установка GitHub runner](https://docs.github.com/en/actions/how-tos/manage-runners/self-hosted-runners/add-runners),
[интерактивный запуск задач Windows](https://learn.microsoft.com/en-us/powershell/module/scheduledtasks/new-scheduledtaskprincipal).
