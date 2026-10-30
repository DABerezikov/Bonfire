$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '../../scripts/Update-BonfireInteractive.ps1')
$script:passed = 0
$testRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot ('../../artifacts/update-tests/' + [guid]::NewGuid().ToString('N'))))
function Assert-True($Value, $Message) { if (-not $Value) { throw $Message } }
function Expect-Failure($Action) {
    $failed = $false
    try { & $Action | Out-Null } catch { $failed = $true }
    Assert-True $failed 'Ожидался отказ.'
}
function Test-Case($Name, $Action) { & $Action; $script:passed++; Write-Host "PASS: $Name" }
function New-Fixture {
    $root = Join-Path $testRoot ([guid]::NewGuid().ToString('N'))
    [IO.Directory]::CreateDirectory((Join-Path $root 'publish')) | Out-Null
    [IO.Directory]::CreateDirectory((Join-Path $root 'installed')) | Out-Null
    [IO.File]::WriteAllText((Join-Path $root 'publish/Bonfire.exe'), 'new')
    $script:approved = $true
    $script:closed = $false
    $script:written = $false
    $script:interactive = $true
    $script:closeFails = $false
    return @{ Source = (Join-Path $root 'publish'); Target = (Join-Path $root 'installed') }
}
function Test-BonfireInteractiveSession { return $script:interactive }
function Request-BonfireUpdateApproval($Destination, $Version, $TimeoutSeconds) { return $script:approved }
function Close-BonfireForUpdate($Destination) {
    if ($script:closeFails) { throw 'Приложение не закрылось.' }
    $script:closed = $true
}
function Invoke-BonfireDeployment($PublishDirectory, $Destination) {
    Assert-True $script:closed 'Установка началась до закрытия.'
    $script:written = $true
    return [pscustomobject]@{ Destination = $Destination; BackupPath = 'test-backup'; FilesUpdated = 1 }
}
Test-Case 'Отказ пользователя не закрывает приложение и не изменяет установку' {
    $f = New-Fixture
    $script:approved = $false
    $result = Invoke-BonfireInteractiveUpdate -PublishDirectory $f.Source -Destination $f.Target
    Assert-True ($result.Status -eq 'Cancelled' -and -not $script:closed -and -not $script:written) 'Отмена не сохранена.'
}
Test-Case 'Подтверждение закрывает приложение перед записью и сохраняет версию' {
    $f = New-Fixture
    $result = Invoke-BonfireInteractiveUpdate -PublishDirectory $f.Source -Destination $f.Target -Version 'abc1234'
    Assert-True ($result.Status -eq 'Deployed' -and $script:written) 'Установка не выполнена.'
    $version = Get-Content -LiteralPath (Join-Path $f.Target '.bonfire-version.json') -Raw | ConvertFrom-Json
    Assert-True ($version.Commit -eq 'abc1234') 'Версия не записана.'
}
Test-Case 'Отсутствие интерактивного сеанса запрещает обновление' {
    $f = New-Fixture
    $script:interactive = $false
    Expect-Failure { Invoke-BonfireInteractiveUpdate -PublishDirectory $f.Source -Destination $f.Target }
    Assert-True (-not $script:written -and -not $script:closed) 'Обновление запущено без пользователя.'
}
Test-Case 'Незакрытое приложение запрещает запись' {
    $f = New-Fixture
    $script:closeFails = $true
    Expect-Failure { Invoke-BonfireInteractiveUpdate -PublishDirectory $f.Source -Destination $f.Target }
    Assert-True (-not $script:written) 'Запись выполнена при открытом приложении.'
}
Test-Case 'Неверная публикация отклоняется до закрытия' {
    $f = New-Fixture
    Expect-Failure { Invoke-BonfireInteractiveUpdate -PublishDirectory $f.Target -Destination $f.Target }
    Assert-True (-not $script:closed -and -not $script:written) 'Приложение закрыто без публикации.'
}
Test-Case 'WhatIf не запрашивает закрытие и не изменяет файлы' {
    $f = New-Fixture
    Invoke-BonfireInteractiveUpdate -PublishDirectory $f.Source -Destination $f.Target -WhatIf | Out-Null
    Assert-True (-not $script:closed -and -not $script:written) 'WhatIf изменил установку.'
}
Write-Host "Интерактивное обновление: $script:passed проверок пройдено."
