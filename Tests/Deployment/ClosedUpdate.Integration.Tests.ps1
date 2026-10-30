$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot ('../../artifacts/closed-update/' + [guid]::NewGuid().ToString('N'))))
$publish = Join-Path $root 'publish'
$target = Join-Path $root 'installed'
[IO.Directory]::CreateDirectory($publish) | Out-Null
[IO.Directory]::CreateDirectory($target) | Out-Null
[IO.File]::WriteAllText((Join-Path $publish 'Bonfire.exe'), 'new executable')
[IO.File]::WriteAllText((Join-Path $target 'Bonfire.exe'), 'old executable')
[IO.File]::WriteAllText((Join-Path $target 'BonfireDB.db'), 'user database')
$databaseHash = (Get-FileHash -LiteralPath (Join-Path $target 'BonfireDB.db')).Hash
& powershell.exe -NoProfile -STA -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot '../../scripts/Update-BonfireInteractive.ps1') -PublishDirectory $publish -Destination $target -Version 'closed-test' -TimeoutSeconds 1
if ($LASTEXITCODE -ne 0) { throw 'Закрытое приложение не обновилось без подтверждения.' }
$manifest = Get-Content -LiteralPath (Join-Path $target '.bonfire-version.json') -Raw | ConvertFrom-Json
if ($manifest.Commit -ne 'closed-test') { throw 'Не записана версия.' }
if ([IO.File]::ReadAllText((Join-Path $target 'Bonfire.exe')) -ne 'new executable') { throw 'Версия не обновлена.' }
if ([IO.File]::ReadAllText((Join-Path $manifest.BackupPath 'Bonfire.exe')) -ne 'old executable') { throw 'Бэкап не содержит старую версию.' }
if ((Get-FileHash -LiteralPath (Join-Path $target 'BonfireDB.db')).Hash -ne $databaseHash) { throw 'База данных изменена.' }
Write-Host 'CLOSED_UPDATE_INTEGRATION_OK: обновление без окна, бэкап и сохранение БД.'
