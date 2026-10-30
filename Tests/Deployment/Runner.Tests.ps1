$ErrorActionPreference = 'Stop'
$installer = Join-Path $PSScriptRoot '../../scripts/Install-BonfireRunner.ps1'
if (-not (Test-Path -LiteralPath $installer)) { throw 'Нет установщика runner.' }
. $installer
$verifyPackage = ${function:Test-BonfireRunnerPackage}
$savePackage = ${function:Save-BonfireRunnerPackage}
$readToken = ${function:Get-BonfireRunnerRegistrationToken}

$testRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot ('../../artifacts/runner-tests/' + [guid]::NewGuid().ToString('N'))))
$script:passed = 0
function Assert-True($Condition, $Message) { if (-not $Condition) { throw $Message } }
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
function New-Fixture {
    $script:configured = 0
    $script:task = $null
    $script:started = $false
    $script:downloaded = 0
    $script:badHash = $false
    $script:configFails = $false
    $script:downloadFails = $false
    $script:machine = 'MONOBLOCK'
    $root = Join-Path $testRoot ([guid]::NewGuid().ToString('N'))
    [IO.Directory]::CreateDirectory($root) | Out-Null
    return $root
}
# Внешние границы заменены: никаких скачиваний, регистрации GitHub или задач Windows.
function Get-BonfireRunnerContext {
    return @{ Computer = $script:machine; User = 'TEST\User'; Sid = 'S-1-5-21-123'; Interactive = $true; X64 = $true; Elevated = $false }
}
function Get-BonfireRunnerAsset {
    return @{ Name = 'runner.zip'; Url = 'https://github.com/actions/runner/releases/download/v-test/runner.zip'; Hash = 'fake' }
}
function Save-BonfireRunnerPackage($Asset, $Path) {
    $script:downloaded++
    [IO.File]::WriteAllText($Path, 'fake archive')
    if ($script:downloadFails) { throw 'Connection closed while downloading' }
}
function Test-BonfireRunnerPackage($Path, $ExpectedHash) {
    if ($script:badHash) { throw 'SHA-256 не совпадает.' }
}
function Expand-BonfireRunnerPackage($Archive, $Directory) {
    [IO.File]::WriteAllText((Join-Path $Directory 'config.cmd'), '@exit /b 0')
    [IO.File]::WriteAllText((Join-Path $Directory 'run.cmd'), '@exit /b 0')
}
function Register-BonfireRunner($Directory, $RepositoryUrl, $Name) {
    $script:configured++
    if ($script:configFails) { throw 'Регистрация отклонена.' }
    @{ gitHubUrl = $RepositoryUrl; agentName = $Name } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $Directory '.runner')
}
function Get-BonfireRunnerTask($Name) { return $null }
function Register-BonfireRunnerTask($Definition) { $script:task = $Definition }
function Start-BonfireRunnerTask($Name) { $script:started = $true }

Test-Case 'Установка регистрирует только текущего пользователя и интерактивный автозапуск' {
    $root = New-Fixture
    Install-BonfireRunner -RunnerDirectory (Join-Path $root 'runner') | Out-Null
    Assert-True ($script:configured -eq 1 -and $script:started) 'Runner не настроен/не запущен.'
    Assert-True ($script:task.LogonType -eq 'Interactive' -and $script:task.RunLevel -eq 'Limited') 'Неверный режим задачи.'
    Assert-True ($script:task.User -eq 'TEST\User') 'Выбран чужой пользователь.'
    Assert-True ($script:task.Arguments -match 'run.cmd' -and $script:task.Arguments -notmatch 'token') 'Неверный запуск задачи.'
}
Test-Case 'WhatIf не пишет файлы, не скачивает и не регистрирует runner' {
    $root = New-Fixture
    $directory = Join-Path $root 'runner'
    Install-BonfireRunner -RunnerDirectory $directory -WhatIf | Out-Null
    Assert-True (-not (Test-Path -LiteralPath $directory)) 'WhatIf создал каталог.'
    Assert-True ($script:downloaded -eq 0 -and $null -eq $script:task) 'WhatIf изменил систему.'
}
Test-Case 'Не MONOBLOCK — отказ до скачивания' {
    $root = New-Fixture
    $script:machine = 'OTHER'
    Expect-Failure { Install-BonfireRunner -RunnerDirectory (Join-Path $root 'runner') }
    Assert-True ($script:downloaded -eq 0) 'Скачивание началось на другом компьютере.'
}
Test-Case 'Неизвестное содержимое каталога не перезаписывается' {
    $root = New-Fixture
    [IO.File]::WriteAllText((Join-Path $root 'user.txt'), 'keep')
    Expect-Failure { Install-BonfireRunner -RunnerDirectory $root }
    Assert-True ($script:downloaded -eq 0) 'Начата установка поверх чужих файлов.'
}
Test-Case 'Неверный SHA-256 запрещает распаковку и регистрацию' {
    $root = New-Fixture
    $script:badHash = $true
    Expect-Failure { Install-BonfireRunner -RunnerDirectory (Join-Path $root 'runner') }
    Assert-True ($script:configured -eq 0 -and $null -eq $script:task) 'Архив с неверным хешем принят.'
}
Test-Case 'Ошибка регистрации не создаёт автозапуск' {
    $root = New-Fixture
    $script:configFails = $true
    Expect-Failure { Install-BonfireRunner -RunnerDirectory (Join-Path $root 'runner') }
    Assert-True ($null -eq $script:task -and -not $script:started) 'Автозапуск создан без регистрации.'
}
Test-Case 'Повторный запуск использует существующую регистрацию' {
    $root = New-Fixture
    $directory = Join-Path $root 'runner'
    Install-BonfireRunner -RunnerDirectory $directory | Out-Null
    Install-BonfireRunner -RunnerDirectory $directory | Out-Null
    Assert-True ($script:configured -eq 1 -and $script:downloaded -eq 1) 'Runner зарегистрирован повторно.'
}
Test-Case 'Регистрация другого репозитория не заменяется' {
    $root = New-Fixture
    [IO.File]::WriteAllText((Join-Path $root 'run.cmd'), '@exit /b 0')
    @{ gitHubUrl = 'https://github.com/other/repo'; agentName = 'MONOBLOCK-Bonfire' } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $root '.runner')
    Expect-Failure { Install-BonfireRunner -RunnerDirectory $root }
    Assert-True ($null -eq $script:task) 'Чужая регистрация принята.'
}
Test-Case 'Настоящая проверка SHA-256 принимает оригинал и отклоняет повреждение' {
    $root = New-Fixture
    $path = Join-Path $root 'archive.zip'
    [IO.File]::WriteAllText($path, 'original package')
    $hash = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
    & $verifyPackage $path $hash
    [IO.File]::WriteAllText($path, 'changed package')
    Expect-Failure { & $verifyPackage $path $hash }
}
Test-Case 'После оборванного скачивания установку можно повторить' {
    $root = New-Fixture
    $directory = Join-Path $root 'runner'
    $script:downloadFails = $true
    Expect-Failure { Install-BonfireRunner -RunnerDirectory $directory }
    $script:downloadFails = $false
    Install-BonfireRunner -RunnerDirectory $directory | Out-Null
    Assert-True ($script:configured -eq 1) 'Повторная установка не выполнена.'
}
Test-Case 'Локальный архив копируется без обращения к сети' {
    $root = New-Fixture
    $source = Join-Path $root 'provided.zip'
    $destination = Join-Path $root 'copy.zip'
    [IO.File]::WriteAllText($source, 'archive from another computer')
    & $savePackage @{ Name = 'offline.zip'; Url = 'invalid'; Hash = '' } $destination $source
    Assert-True ((Get-FileHash -LiteralPath $source).Hash -eq (Get-FileHash -LiteralPath $destination).Hash) 'Локальный архив изменён.'
}
Test-Case 'Токен из поля файла преобразуется в SecureString без запроса ввода' {
    $script:RegistrationTokenText = 'test-registration-token'
    try {
        $token = & $readToken
        try { Assert-True ($token -is [Security.SecureString] -and $token.Length -eq 23) 'Поле токена не использовано.' }
        finally { $token.Dispose() }
    }
    finally { $script:RegistrationTokenText = '' }
}
Write-Host "Runner: $script:passed проверок пройдено. Фикстуры: $testRoot"
