# Устойчивое скачивание публикации

План: заменить download-artifact в deploy ограниченным скачиванием curl без изменения установки.
1. TDD: временные архивы и подменённые сетевые границы — обрыв, продолжение, свежая ссылка,
   неверный digest, исчерпание попыток, отказ Range, готовый проверенный кеш, непустой выход.
2. GitHub API: артефакт строго текущего run/name, обязательные digest/size; actions:read только deploy.
   API-запросы ограничены 15 секундами, bearer только api.github.com; redirect читается без перехода.
3. curl: 3 попытки по 120 секунд, соединение 15 секунд, скорость минимум 16 КиБ/с за 30 секунд;
   продолжение .part, обновление signed URL, сброс при неподдержанном Range/неверном SHA.
4. SHA-256 до распаковки; staging в родительской папке выхода и перенос после успешной распаковки.
   Непустой выход не перезаписывать. Ошибка — exit 1, установка не вызывается. Download step: 8 минут.
5. Проверить настоящий curl на локальном TCP-сервере и Windows PowerShell 5.1;
   workflow encoding, остальные проверки deployment, полный Release-гейт и документация.

Псевдокод:
```
asset = API(run, name) // id + digest + size
repeat <= 3:
    if cached bytes match digest: extract to new staging; publish staging; success
    url = API signed redirect(asset.id) // fresh; token stays on GitHub
    status = curl(url, resume(part), max=120, low_speed=30)
    if status == success and sha256(part) == asset.digest: stage + publish; success
    if checksum mismatch or range unsupported: discard only owned part
    retain partial bytes on timeout/connection failure
failure // destination and installed application untouched
```


Реализовано: scripts/Get-BonfireArtifact.ps1, шаг workflow с timeout=8 и actions:read,
изолированные ArtifactDownload.Tests.ps1/Server.ps1. Ожидаемые отказы — Success/Error,
технические ошибки API/файлов преобразуются на границе. Bearer не передаётся curl;
его конфигурация и SAS не выводятся в argv/журнал, пользовательский curlrc отключён.
11/11 сценариев в PowerShell 5.1. Подтверждено: curl может вернуть 0 на HTTP 416;
поэтому статус HTTP проверяется отдельно и приводит к сбросу partial.
Настоящие GitHub API metadata/redirect проверены без скачивания большого архива.
Deploy 20, Runner 12, InteractiveUpdate 9, ClosedUpdate integration и кодировка
workflow CP1251/CP1252 также прошли. Сетевые проблемы Azure не маскируются успехом.


Уточнение кодировки после CI: .NET Framework создаёт stdin StreamWriter из
Console.InputEncoding при Process.Start и может сразу послать BOM. Локальный OEM
этого не выявлял. Теперь на время Start задаётся UTF8Encoding(false), прежнее
значение восстанавливается в finally до записи конфигурации. Регрессия принудительно
включает UTF-8 с BOM, проверяет реальный curl, путь с кириллицей/пробелами и
восстановление исходной кодировки. Ошибка CI воспроизведена до правки, затем 11/11.
