$ErrorActionPreference = 'Stop'
$scriptPath = Join-Path $PSScriptRoot '../../scripts/Deploy-Bonfire.ps1'
$testRoot = Join-Path $PSScriptRoot ('../../artifacts/deploy-tests/' + [guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($testRoot) | Out-Null
$script:passed = 0

function Write-FixtureFile($Root, $Relative, $Content) {
    $path = Join-Path $Root $Relative
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($path)) | Out-Null
    [IO.File]::WriteAllText($path, $Content)
}
function Assert-True($Condition, $Message) {
    if (-not $Condition) { throw $Message }
}
function New-Fixture {
    $root = Join-Path $testRoot ([guid]::NewGuid().ToString('N'))
    $source = Join-Path $root 'publish'
    $target = Join-Path $root 'installed'
    Write-FixtureFile $source 'Bonfire.exe' 'new executable'
    Write-FixtureFile $source 'nested/library.dll' 'new library'
    Write-FixtureFile $source 'appsettings.json' 'new settings'
    Write-FixtureFile $target 'Bonfire.exe' 'old executable'
    Write-FixtureFile $target 'BonfireDB.db' 'user database'
    Write-FixtureFile $target 'BonfireDB.db-wal' 'user wal'
    Write-FixtureFile $target 'BonfireDB.db-shm' 'user shm'
    Write-FixtureFile $target 'appsettings.json' 'user settings'
    Write-FixtureFile $target 'Reports/report.xlsx' 'user report'
    Write-FixtureFile $target 'backups/previous/Bonfire.exe' 'previous backup'
    return @{ Source = $source; Target = $target }
}
function Expect-Failure($Action) {
    $failed = $false
    try { & $Action | Out-Null } catch { $failed = $true }
    Assert-True $failed 'Ожидался отказ.'
}
function Test-Case($Name, $Action) {
    & $Action
    $script:passed++
    Write-Host "PASS: $Name"
}

Test-Case 'Резервная копия предшествует замене; данные и настройки неизменны' {
    $f = New-Fixture
    Write-FixtureFile $f.Source 'BonfireDB.db' 'DO NOT DEPLOY'
    Write-FixtureFile $f.Source 'BonfireDB.db-wal' 'DO NOT DEPLOY'
    Write-FixtureFile $f.Source 'nested/other.sqlite3' 'DO NOT DEPLOY'
    Write-FixtureFile $f.Source 'Reports/report.xlsx' 'DO NOT DEPLOY'
    $result = & $scriptPath -PublishDirectory $f.Source -Destination $f.Target
    Assert-True ([IO.File]::ReadAllText((Join-Path $f.Target 'Bonfire.exe')) -eq 'new executable') 'Версия не обновлена.'
    Assert-True ([IO.File]::ReadAllText((Join-Path $result.BackupPath 'Bonfire.exe')) -eq 'old executable') 'Нет старой версии.'
    foreach ($relative in @('BonfireDB.db', 'BonfireDB.db-wal', 'BonfireDB.db-shm', 'appsettings.json', 'Reports/report.xlsx')) {
        Assert-True ((Get-FileHash -LiteralPath (Join-Path $f.Target $relative)).Hash -eq
            (Get-FileHash -LiteralPath (Join-Path $result.BackupPath $relative)).Hash) "Изменены данные: $relative"
    }
    Assert-True (-not (Test-Path -LiteralPath (Join-Path $f.Target 'nested/other.sqlite3'))) 'Развёрнута чужая БД.'
    Assert-True (-not (Test-Path -LiteralPath (Join-Path $result.BackupPath 'backups'))) 'Бэкапы скопированы рекурсивно.'
    Assert-True (Test-Path -LiteralPath (Join-Path $result.BackupPath '.backup-complete')) 'Бэкап не подтверждён.'
}
Test-Case 'Старые файлы приложения переносятся в бэкап, служебные и пользовательские остаются' {
    $f = New-Fixture
    foreach ($relative in @('obsolete.dll', 'old.pdb', 'Bonfire.deps.json', 'Bonfire.runtimeconfig.json', 'createdump.exe', 'ru/Old.resources.dll', 'runtimes/win-x64/native/old.dll')) {
        Write-FixtureFile $f.Target $relative 'old binary'
    }
    foreach ($relative in @('runner-install/runner.dll', 'Install-BonfireRunner.ps1', 'Reports/custom.dll', 'notes.txt')) {
        Write-FixtureFile $f.Target $relative 'keep'
    }
    $result = & $scriptPath -PublishDirectory $f.Source -Destination $f.Target
    foreach ($relative in @('obsolete.dll', 'old.pdb', 'Bonfire.deps.json', 'Bonfire.runtimeconfig.json', 'createdump.exe', 'ru/Old.resources.dll', 'runtimes/win-x64/native/old.dll')) {
        Assert-True (-not (Test-Path -LiteralPath (Join-Path $f.Target $relative))) "Остался старый файл: $relative"
        Assert-True ([IO.File]::ReadAllText((Join-Path $result.BackupPath $relative)) -eq 'old binary') "Нет файла в бэкапе: $relative"
    }
    Assert-True (-not (Test-Path -LiteralPath (Join-Path $f.Target 'ru'))) 'Остался пустой каталог локализации.'
    foreach ($relative in @('runner-install/runner.dll', 'Install-BonfireRunner.ps1', 'Reports/custom.dll', 'notes.txt')) {
        Assert-True ([IO.File]::ReadAllText((Join-Path $f.Target $relative)) -eq 'keep') "Изменён служебный файл: $relative"
    }
}
Test-Case 'Откат возвращает также устаревшие библиотеки' {
    $f = New-Fixture
    Write-FixtureFile $f.Target 'ru/Old.resources.dll' 'old library'
    Write-FixtureFile $f.Target 'nested' 'blocks directory'
    Expect-Failure { & $scriptPath -PublishDirectory $f.Source -Destination $f.Target }
    Assert-True ([IO.File]::ReadAllText((Join-Path $f.Target 'ru/Old.resources.dll')) -eq 'old library') 'Старая библиотека не возвращена.'
    Assert-True ([IO.File]::ReadAllText((Join-Path $f.Target 'Bonfire.exe')) -eq 'old executable') 'EXE не возвращён.'
}
Test-Case 'Занятая устаревшая библиотека запрещает замену версии' {
    $f = New-Fixture
    Write-FixtureFile $f.Target 'obsolete.dll' 'locked library'
    $stream = [IO.File]::Open((Join-Path $f.Target 'obsolete.dll'), 'Open', 'Read', 'Read')
    try { Expect-Failure { & $scriptPath -PublishDirectory $f.Source -Destination $f.Target } }
    finally { $stream.Dispose() }
    Assert-True ([IO.File]::ReadAllText((Join-Path $f.Target 'Bonfire.exe')) -eq 'old executable') 'Версия изменена при занятой старой DLL.'
}
Test-Case 'Повторное развёртывание сохраняет отдельные версии' {
    $f = New-Fixture
    $first = & $scriptPath -PublishDirectory $f.Source -Destination $f.Target
    Write-FixtureFile $f.Source 'Bonfire.exe' 'next executable'
    $second = & $scriptPath -PublishDirectory $f.Source -Destination $f.Target
    Assert-True ($first.BackupPath -ne $second.BackupPath) 'Повторное использование бэкапа.'
    Assert-True ([IO.File]::ReadAllText((Join-Path $second.BackupPath 'Bonfire.exe')) -eq 'new executable') 'Неверная промежуточная версия.'
}
Test-Case 'WhatIf не создаёт и не заменяет файлы' {
    $f = New-Fixture
    $before = @(Get-ChildItem -LiteralPath $f.Target -Recurse -Force).Count
    & $scriptPath -PublishDirectory $f.Source -Destination $f.Target -WhatIf | Out-Null
    Assert-True (@(Get-ChildItem -LiteralPath $f.Target -Recurse -Force).Count -eq $before) 'WhatIf записал файлы.'
    Assert-True ([IO.File]::ReadAllText((Join-Path $f.Target 'Bonfire.exe')) -eq 'old executable') 'WhatIf обновил версию.'
}
Test-Case 'Неполная публикация и вложенный источник отклоняются до замены' {
    $f = New-Fixture
    Expect-Failure { & $scriptPath -PublishDirectory (Join-Path $f.Source 'nested') -Destination $f.Target }
    Expect-Failure { & $scriptPath -PublishDirectory $f.Target -Destination $f.Target }
    Assert-True ([IO.File]::ReadAllText((Join-Path $f.Target 'Bonfire.exe')) -eq 'old executable') 'Версия изменена при отказе.'
}
Test-Case 'Занятая БД запрещает обновление' {
    $f = New-Fixture
    $stream = [IO.File]::Open((Join-Path $f.Target 'BonfireDB.db'), 'Open', 'ReadWrite', 'None')
    try { Expect-Failure { & $scriptPath -PublishDirectory $f.Source -Destination $f.Target } }
    finally { $stream.Dispose() }
    Assert-True ([IO.File]::ReadAllText((Join-Path $f.Target 'Bonfire.exe')) -eq 'old executable') 'Версия изменена при занятой БД.'
}
Test-Case 'Ошибка резервного копирования запрещает замену файлов' {
    $f = New-Fixture
    $stream = [IO.File]::Open((Join-Path $f.Target 'Reports/report.xlsx'), 'Open', 'ReadWrite', 'None')
    try { Expect-Failure { & $scriptPath -PublishDirectory $f.Source -Destination $f.Target } }
    finally { $stream.Dispose() }
    Assert-True ([IO.File]::ReadAllText((Join-Path $f.Target 'Bonfire.exe')) -eq 'old executable') 'Версия изменена без бэкапа.'
}
Test-Case 'Первая установка получает настройки, но не чужую БД' {
    $f = New-Fixture
    $target = Join-Path $testRoot ([guid]::NewGuid().ToString('N'))
    Write-FixtureFile $f.Source 'BonfireDB.db' 'DO NOT DEPLOY'
    & $scriptPath -PublishDirectory $f.Source -Destination $target | Out-Null
    Assert-True (Test-Path -LiteralPath (Join-Path $target 'appsettings.json')) 'Нет начальной конфигурации.'
    Assert-True (-not (Test-Path -LiteralPath (Join-Path $target 'BonfireDB.db'))) 'Скопирована БД из публикации.'
}
Test-Case 'Ошибка замены восстанавливает прежние файлы из бэкапа' {
    $f = New-Fixture
    Write-FixtureFile $f.Target 'nested' 'file blocks directory creation'
    Expect-Failure { & $scriptPath -PublishDirectory $f.Source -Destination $f.Target }
    Assert-True ([IO.File]::ReadAllText((Join-Path $f.Target 'Bonfire.exe')) -eq 'old executable') 'Старая версия не восстановлена.'
    Assert-True ([IO.File]::ReadAllText((Join-Path $f.Target 'BonfireDB.db')) -eq 'user database') 'БД изменилась при откате.'
    Assert-True (-not (Test-Path -LiteralPath (Join-Path $f.Target '.bonfire-deploy.lock'))) 'Блокировка не снята.'
}
Test-Case 'Параллельное развёртывание отклоняется без удаления чужой блокировки' {
    $f = New-Fixture
    Write-FixtureFile $f.Target '.bonfire-deploy.lock' 'another deployment'
    Expect-Failure { & $scriptPath -PublishDirectory $f.Source -Destination $f.Target }
    Assert-True (Test-Path -LiteralPath (Join-Path $f.Target '.bonfire-deploy.lock')) 'Удалена чужая блокировка.'
    Assert-True ([IO.File]::ReadAllText((Join-Path $f.Target 'Bonfire.exe')) -eq 'old executable') 'Версия изменилась.'
}
Write-Host "Развёртывание: $script:passed проверок пройдено. Фикстуры: $testRoot"
