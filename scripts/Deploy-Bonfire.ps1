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
    if ((Get-FileHash -LiteralPath $Source -Algorithm SHA256).Hash -ne
        (Get-FileHash -LiteralPath $Target -Algorithm SHA256).Hash) {
        throw "Не совпала контрольная сумма: $Target"
    }
}

$targetRoot = Get-FullPath $Destination
if ($targetRoot -eq ([IO.Path]::GetPathRoot($targetRoot)).TrimEnd('\', '/')) {
    throw 'Укажите папку приложения, а не корень диска или сетевого ресурса.'
}

if (-not $PublishDirectory) {
    if ($WhatIfPreference) {
        Write-Host "План: Release win-x64 (self-contained), резервная копия в $targetRoot\backups, обновление $targetRoot. БД и существующие настройки сохраняются."
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
    # Проверяем доступ до резервного копирования и первой замены.
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
        foreach ($file in $files) {
            $relative = $file.FullName.Substring($sourceRoot.Length + 1)
            $attempted.Add($relative)
            Copy-Checked $file.FullName (Join-Path $targetRoot $relative)
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
