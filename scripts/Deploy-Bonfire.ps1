<#
.SYNOPSIS
Публикует Bonfire и обновляет установленную версию после проверенного бэкапа.
.EXAMPLE
.\scripts\Deploy-Bonfire.ps1 -WhatIf
.EXAMPLE
.\scripts\Deploy-Bonfire.ps1
.EXAMPLE
.\scripts\Deploy-Bonfire.ps1 -PublishDirectory .\artifacts\my-publish
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string]$Destination = '\\MONOBLOCK\Bonfire',
    [string]$PublishDirectory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-FullPath([string]$Path) {
    return [IO.Path]::GetFullPath($Path).TrimEnd('\', '/')
}

function Test-Inside([string]$Child, [string]$Parent) {
    return $Child.Equals($Parent, [StringComparison]::OrdinalIgnoreCase) -or
        $Child.StartsWith($Parent + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)
}

function Test-Database([string]$Name) {
    return $Name -match '(?i)\.(db|db3|sqlite|sqlite3)(-(wal|shm|journal))?$'
}

function Test-ApplicationFile([string]$Relative) {
    $relativePath = $Relative.Replace('\', '/')
    # Только бинарники установки; файлы runner, отчёты и произвольные документы сохраняются.
    return $relativePath -match '^(?i:[^/]+\.(dll|pdb)|Bonfire\.exe|createdump\.exe|[^/]+\.(deps|runtimeconfig)\.json)$' -or
        $relativePath -match '^(?i:[a-z]{2}(?:-[a-z]{2,4})?/[^/]+\.resources\.dll|runtimes/.+\.(dll|pdb))$'
}

function Get-Inventory([string]$Root, [switch]$Installed) {
    foreach ($entry in Get-ChildItem -LiteralPath $Root -Force) {
        if ($Installed -and ($entry.Name -eq 'backups' -or $entry.Name -eq '.bonfire-deploy.lock')) { continue }
        if ($entry.Attributes -band [IO.FileAttributes]::ReparsePoint) {
            throw "Символические ссылки и junction не поддерживаются: $($entry.FullName)"
        }
        if ($entry.PSIsContainer) {
            Get-Inventory $entry.FullName
        }
        else { $entry }
    }
}

function Copy-Checked([string]$Source, [string]$Target) {
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($Target)) | Out-Null
    [IO.File]::Copy($Source, $Target, $true)
    # Документ, открытый редактором с разрешённым чтением, не требует закрытия ради бэкапа.
    $sourceStream = [IO.File]::Open($Source, 'Open', 'Read', 'ReadWrite')
    try { $sourceHash = (Get-FileHash -InputStream $sourceStream -Algorithm SHA256).Hash }
    finally { $sourceStream.Dispose() }
    if ($sourceHash -ne (Get-FileHash -LiteralPath $Target -Algorithm SHA256).Hash) {
        throw "Не совпала контрольная сумма: $Target"
    }
}


function Remove-OldBackups([string]$TargetRoot, [string]$CurrentBackup) {
    $backupsRoot = Get-FullPath (Join-Path $TargetRoot 'backups')
    if (-not (Test-Path -LiteralPath $backupsRoot -PathType Container)) { return }
    if (-not (Test-Inside $backupsRoot $TargetRoot) -or
        ((Get-Item -LiteralPath $backupsRoot -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
        throw 'Небезопасный каталог резервных копий; очистка отменена.'
    }
    $completed = @(Get-ChildItem -LiteralPath $backupsRoot -Directory -Force | Where-Object {
        $_.Name -match '^\d{8}-\d{6}-\d{3}-[0-9a-fA-F]{32}$' -and
        -not ($_.Attributes -band [IO.FileAttributes]::ReparsePoint) -and
        (Test-Path -LiteralPath (Join-Path $_.FullName '.backup-complete') -PathType Leaf)
    })
    # Текущая проверенная копия сохраняется даже при переводе часов назад.
    $otherCopiesToKeep = if ($CurrentBackup) { 2 } else { 3 }
    $obsolete = @($completed | Where-Object { $_.FullName -ne $CurrentBackup } |
        Sort-Object Name -Descending | Select-Object -Skip $otherCopiesToKeep)
    foreach ($directory in $obsolete) {
        try {
            $path = Get-FullPath $directory.FullName
            if (-not (Test-Inside $path $backupsRoot) -or
                -not ([IO.Path]::GetDirectoryName($path)).Equals($backupsRoot, [StringComparison]::OrdinalIgnoreCase)) {
                throw "Каталог находится вне backups: $path"
            }
            # Проверяем всё дерево до рекурсивного удаления: ссылки и занятые файлы запрещены.
            $inventory = @(Get-Inventory $path)
            foreach ($file in $inventory) {
                $probe = [IO.File]::Open($file.FullName, 'Open', 'Read', 'None')
                $probe.Dispose()
            }
            Remove-Item -LiteralPath $path -Recurse -Force
            Write-Host "Удалена старая резервная копия: $path"
        }
        catch {
            Write-Warning "Не удалось удалить старую резервную копию $($directory.FullName): $_"
        }
    }
}

$targetRoot = Get-FullPath $Destination
# Штатный ресурс сам является папкой приложения, в том числе при ручном обновлении по SMB.
$isDefaultApplicationShare = $targetRoot -ieq '\\MONOBLOCK\Bonfire'
if ($targetRoot -eq ([IO.Path]::GetPathRoot($targetRoot)).TrimEnd('\', '/') -and -not $isDefaultApplicationShare) {
    throw 'Укажите папку приложения, а не корень диска или сетевого ресурса.'
}

if (-not $PublishDirectory) {
    if ($WhatIfPreference) {
        Write-Host "План: Release win-x64 (self-contained), резервная копия в $targetRoot\backups, обновление $targetRoot; после успеха — хранение трёх последних завершённых резервных копий. БД и существующие настройки сохраняются."
        return
    }
    $repository = Get-FullPath (Join-Path $PSScriptRoot '..')
    $PublishDirectory = Join-Path $repository ('artifacts/deploy-publish/' + [guid]::NewGuid().ToString('N'))
    Push-Location $repository
    try {
        & dotnet publish (Join-Path $repository 'Bonfire/Bonfire.csproj') -p:PublishProfile=FolderProfile -o $PublishDirectory --nologo
        if ($LASTEXITCODE -ne 0) { throw "dotnet publish завершился с кодом $LASTEXITCODE. Установка не изменена." }
    }
    finally { Pop-Location }
}

$sourceRoot = Get-FullPath $PublishDirectory
if ((Test-Inside $sourceRoot $targetRoot) -or (Test-Inside $targetRoot $sourceRoot)) {
    throw 'Каталоги публикации и установки должны быть раздельными и не вложенными друг в друга.'
}
if (-not (Test-Path -LiteralPath (Join-Path $sourceRoot 'Bonfire.exe') -PathType Leaf)) {
    throw "Публикация не содержит Bonfire.exe: $sourceRoot"
}
foreach ($root in @($sourceRoot, $targetRoot)) {
    if ((Test-Path -LiteralPath $root) -and
        ((Get-Item -LiteralPath $root -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
        throw "Каталог не должен быть ссылкой: $root"
    }
}
$payload = @(Get-Inventory $sourceRoot | Where-Object {
    $relative = $_.FullName.Substring($sourceRoot.Length + 1)
    -not (Test-Database $_.Name) -and $relative -notmatch '^(?i:backups|Reports|Logs)([\\/]|$)' -and
        $_.Name -ne '.bonfire-deploy.lock'
})
if (-not $PSCmdlet.ShouldProcess($targetRoot, 'Создать и проверить резервную копию; обновить файлы Bonfire, сохранив БД и настройки')) { return }

[IO.Directory]::CreateDirectory($targetRoot) | Out-Null
$lockPath = Join-Path $targetRoot '.bonfire-deploy.lock'
$deploymentLock = $null
$databaseLocks = [Collections.Generic.List[IO.FileStream]]::new()
$backupPath = $null
try {
    # CreateNew исключает одновременные запуски даже с разных компьютеров.
    $deploymentLock = [IO.File]::Open($lockPath, 'CreateNew', 'ReadWrite', 'None')
    $installed = @(Get-Inventory $targetRoot -Installed)
    foreach ($database in $installed | Where-Object { Test-Database $_.Name }) {
        # Разрешаем чтение для бэкапа, запрещаем запись в SQLite на время обновления.
        $databaseLocks.Add([IO.File]::Open($database.FullName, 'Open', 'Read', 'Read'))
    }
    $files = @($payload | Where-Object {
        $relative = $_.FullName.Substring($sourceRoot.Length + 1)
        -not (($_.Name -like 'appsettings*.json' -or $_.Extension -eq '.config') -and
            (Test-Path -LiteralPath (Join-Path $targetRoot $relative)))
    })
    $oldApplicationFiles = @($installed | Where-Object {
        Test-ApplicationFile $_.FullName.Substring($targetRoot.Length + 1)
    })
    # Проверяем доступ до резервного копирования и первой замены.
    foreach ($file in $oldApplicationFiles) {
        $probe = [IO.File]::Open($file.FullName, 'Open', 'ReadWrite', 'None')
        $probe.Dispose()
    }
    foreach ($file in $files) {
        $path = Join-Path $targetRoot $file.FullName.Substring($sourceRoot.Length + 1)
        if (Test-Path -LiteralPath $path) {
            $probe = [IO.File]::Open($path, 'Open', 'ReadWrite', 'None')
            $probe.Dispose()
        }
    }
    if ($installed.Count -gt 0) {
        $backupsRoot = Join-Path $targetRoot 'backups'
        if ((Test-Path -LiteralPath $backupsRoot) -and
            ((Get-Item -LiteralPath $backupsRoot -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
            throw 'Каталог backups не должен быть ссылкой.'
        }
        $backupPath = Join-Path $backupsRoot ((Get-Date -Format 'yyyyMMdd-HHmmss-fff') + '-' + [guid]::NewGuid().ToString('N'))
        [IO.Directory]::CreateDirectory($backupPath) | Out-Null
        foreach ($file in $installed) {
            Copy-Checked $file.FullName (Join-Path $backupPath $file.FullName.Substring($targetRoot.Length + 1))
        }
        [IO.File]::WriteAllText((Join-Path $backupPath '.backup-complete'), [DateTimeOffset]::Now.ToString('O'))
        Write-Host "Резервная копия проверена: $backupPath"
    }
    $attempted = [Collections.Generic.List[string]]::new()
    try {
        foreach ($file in $oldApplicationFiles) {
            $relative = $file.FullName.Substring($targetRoot.Length + 1)
            $attempted.Add($relative)
            # Проверенная копия уже находится в завершённом бэкапе; убираем оригинал.
            [IO.File]::Delete($file.FullName)
        }
        foreach ($file in $files) {
            $relative = $file.FullName.Substring($sourceRoot.Length + 1)
            if (-not $attempted.Contains($relative)) { $attempted.Add($relative) }
            Copy-Checked $file.FullName (Join-Path $targetRoot $relative)
        }
        foreach ($file in $oldApplicationFiles) {
            $directory = [IO.Path]::GetDirectoryName($file.FullName)
            while ((Test-Inside (Get-FullPath $directory) $targetRoot) -and
                -not (Get-FullPath $directory).Equals($targetRoot, [StringComparison]::OrdinalIgnoreCase)) {
                if (-not [IO.Directory]::Exists($directory) -or
                    @(Get-ChildItem -LiteralPath $directory -Force).Count -gt 0) { break }
                # Удаляется только пустой каталог внутри проверенной папки установки.
                [IO.Directory]::Delete($directory, $false)
                $directory = [IO.Path]::GetDirectoryName($directory)
            }
        }
    }
    catch {
        $copyError = $_
        foreach ($relative in $attempted) {
            $path = Join-Path $targetRoot $relative
            if ($backupPath -and (Test-Path -LiteralPath (Join-Path $backupPath $relative) -PathType Leaf)) {
                Copy-Checked (Join-Path $backupPath $relative) $path
            }
            elseif (Test-Inside (Get-FullPath $path) $targetRoot) {
                if (Test-Path -LiteralPath $path -PathType Leaf) { Remove-Item -LiteralPath $path }
            }
        }
        throw "Обновление отменено, заменённые файлы восстановлены. Причина: $copyError"
    }
    try { Remove-OldBackups $targetRoot $backupPath }
    catch { Write-Warning "Обновление выполнено, но очистка старых резервных копий не завершена: $_" }
    Write-Host "Bonfire обновлён: $targetRoot"
    [pscustomobject]@{ Destination = $targetRoot; BackupPath = $backupPath; FilesUpdated = $files.Count }
}
finally {
    foreach ($stream in $databaseLocks) { $stream.Dispose() }
    if ($null -ne $deploymentLock) {
        $deploymentLock.Dispose()
        Remove-Item -LiteralPath $lockPath
    }
}
