<#
.SYNOPSIS
Устанавливает GitHub Actions runner на MONOBLOCK с запуском при входе пользователя.
.EXAMPLE
powershell -NoProfile -ExecutionPolicy Bypass -File .\Install-BonfireRunner.ps1 -WhatIf
.EXAMPLE
powershell -NoProfile -ExecutionPolicy Bypass -File .\Install-BonfireRunner.ps1
#>
[CmdletBinding()]
param(
    [string]$RunnerDirectory = (Join-Path $env:LOCALAPPDATA 'Bonfire\GitHubRunner'),
    [string]$RunnerArchive,
    [switch]$WhatIf
)

# Вставьте registration token между кавычками на MONOBLOCK; после установки очистите.
$script:RegistrationTokenText = ''

function Get-BonfireRunnerContext {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    return @{
        Computer = $env:COMPUTERNAME
        User = $identity.Name
        Sid = $identity.User.Value
        Interactive = [Environment]::UserInteractive -and ([Diagnostics.Process]::GetCurrentProcess().SessionId -ne 0)
        X64 = [Environment]::Is64BitProcess -and $env:PROCESSOR_ARCHITECTURE -eq 'AMD64'
    }
}

function Get-BonfireRunnerAsset {
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    # Версия и digest сверены с официальным actions/runner Release API 30.10.2026.
    # Фиксация пары исключает зависимость установщика от анонимного API rate limit.
    # Штатное автоматическое обновление самого runner остаётся включённым.
    return @{
        Name = 'actions-runner-win-x64-2.338.2.zip'
        Url = 'https://github.com/actions/runner/releases/download/v2.338.2/actions-runner-win-x64-2.338.2.zip'
        Hash = 'a1f17aa987fc8158bc7521d284912267a0d01b7c2c4684b33c86783b5e8221cc'
    }
}

function Save-BonfireRunnerPackage($Asset, $Path, [string]$LocalArchive) {
    if (-not $LocalArchive) {
        $cached = Join-Path $PSScriptRoot ('runner-install/' + $Asset.Name)
        if (Test-Path -LiteralPath $cached -PathType Leaf) { $LocalArchive = $cached }
    }
    if ($LocalArchive) {
        $source = (Resolve-Path -LiteralPath $LocalArchive -ErrorAction Stop).ProviderPath
        if ([IO.Path]::GetFullPath($source) -ne [IO.Path]::GetFullPath($Path)) {
            [IO.File]::Copy($source, $Path, $true)
        }
        return
    }
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    $curl = Get-Command curl.exe -ErrorAction SilentlyContinue
    try {
        if ($curl) {
            & $curl.Source --fail --location --retry 2 --connect-timeout 30 --max-time 600 --output $Path $Asset.Url
            if ($LASTEXITCODE -ne 0) { throw "curl завершился с кодом $LASTEXITCODE." }
        }
        else {
            Invoke-WebRequest -UseBasicParsing -Uri $Asset.Url -OutFile $Path -ErrorAction Stop
        }
    }
    catch {
        throw "Не удалось скачать runner: $($_.Exception.Message) Скачайте официальный архив на другом компьютере и укажите -RunnerArchive <путь-к-zip>. Повторный запуск разрешён; проверка сертификатов не отключается."
    }
}

function Get-BonfireRunnerRegistrationToken {
    if ($script:RegistrationTokenText) {
        return ConvertTo-SecureString $script:RegistrationTokenText -AsPlainText -Force
    }
    return Read-Host 'Registration token (ввод скрыт)' -AsSecureString
}

function Test-BonfireRunnerPackage($Path, $ExpectedHash) {
    if ((Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash -ne $ExpectedHash) {
        throw 'SHA-256 архива runner не совпадает. Архив не будет запущен.'
    }
}

function Expand-BonfireRunnerPackage($Archive, $Directory) {
    Expand-Archive -LiteralPath $Archive -DestinationPath $Directory
}

function Register-BonfireRunner($Directory, $RepositoryUrl, $Name) {
    Write-Host 'Откройте https://github.com/DABerezikov/Bonfire/settings/actions/runners/new'
    Write-Host 'Выберите Windows / x64. Нужен временный registration token из команды config, не PAT.'
    $token = Get-BonfireRunnerRegistrationToken
    if ($token.Length -eq 0) { throw 'Токен не введён.' }
    $previous = [Environment]::GetEnvironmentVariable('ACTIONS_RUNNER_INPUT_TOKEN', 'Process')
    $pointer = [IntPtr]::Zero
    Push-Location $Directory
    try {
        $pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($token)
        # Runner сам читает и маскирует ACTIONS_RUNNER_INPUT_TOKEN; в аргументах токена нет.
        $env:ACTIONS_RUNNER_INPUT_TOKEN = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer)
        & (Join-Path $Directory 'config.cmd') --unattended --url $RepositoryUrl --name $Name --labels 'bonfire-monoblock,interactive' --work '_work'
        if ($LASTEXITCODE -ne 0) { throw "Регистрация runner завершилась с кодом $LASTEXITCODE." }
    }
    finally {
        [Environment]::SetEnvironmentVariable('ACTIONS_RUNNER_INPUT_TOKEN', $previous, 'Process')
        if ($pointer -ne [IntPtr]::Zero) { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer) }
        $token.Dispose()
        Pop-Location
    }
}

function Get-BonfireRunnerTask($Name) {
    # Отсутствующая задача допустима; остальные ошибки не скрываются.
    return Get-ScheduledTask | Where-Object { $_.TaskName -eq $Name -and $_.TaskPath -eq '\' }
}

function Register-BonfireRunnerTask($Definition) {
    $action = New-ScheduledTaskAction -Execute $Definition.Executable -Argument $Definition.Arguments -WorkingDirectory $Definition.Directory
    $trigger = New-ScheduledTaskTrigger -AtLogOn -User $Definition.User
    $principal = New-ScheduledTaskPrincipal -UserId $Definition.User -LogonType Interactive -RunLevel Limited
    $settings = New-ScheduledTaskSettingsSet -MultipleInstances IgnoreNew -ExecutionTimeLimit ([TimeSpan]::Zero) -RestartCount 3 -RestartInterval (New-TimeSpan -Minutes 1) -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries
    Register-ScheduledTask -TaskName $Definition.Name -Action $action -Trigger $trigger -Principal $principal -Settings $settings -Description 'Bonfire: GitHub runner в сеансе пользователя; без службы и сохранённого пароля.' | Out-Null
}

function Start-BonfireRunnerTask($Name) {
    Start-ScheduledTask -TaskName $Name
}

function Install-BonfireRunner {
    [CmdletBinding(SupportsShouldProcess = $true)]
    param(
        [string]$RunnerDirectory = (Join-Path $env:LOCALAPPDATA 'Bonfire\GitHubRunner'),
        [string]$RunnerArchive
    )

    $ErrorActionPreference = 'Stop'
    $context = Get-BonfireRunnerContext
    if ($context.Computer -ne 'MONOBLOCK') { throw 'Запустите установщик непосредственно на MONOBLOCK.' }
    if (-not $context.Interactive) { throw 'Нужен интерактивный вход пользователя Windows, а не служба или удалённый job.' }
    if (-not $context.X64) { throw 'Нужен Windows x64 и 64-разрядный PowerShell.' }
    $directory = [IO.Path]::GetFullPath($RunnerDirectory).TrimEnd('\')
    if ($directory -notmatch '^[a-zA-Z]:\\' -or $directory -match '["''&|<>^%!\r\n]' -or $directory -match '^[a-zA-Z]:$') {
        throw 'Укажите отдельную локальную папку runner с простым путём, не сетевой ресурс.'
    }
    $configurationPath = Join-Path $directory '.runner'
    $repositoryUrl = 'https://github.com/DABerezikov/Bonfire'
    $runnerName = 'MONOBLOCK-Bonfire'
    $taskName = 'Bonfire-GitHubRunner-' + $context.Sid
    $asset = Get-BonfireRunnerAsset
    if (Test-Path -LiteralPath $directory) {
        if ((Get-Item -LiteralPath $directory -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) {
            throw 'Папка runner не должна быть ссылкой.'
        }
        if (-not (Test-Path -LiteralPath $configurationPath)) {
            $unknown = @(Get-ChildItem -LiteralPath $directory -Force | Where-Object {
                $_.Name -ne $asset.Name -or $_.PSIsContainer -or ($_.Attributes -band [IO.FileAttributes]::ReparsePoint)
            })
            if ($unknown.Count -gt 0) {
                throw 'Папка непустая и не содержит зарегистрированный runner. Выберите новую папку; старые файлы не удаляются.'
            }
        }
        else {
            $configuration = Get-Content -LiteralPath $configurationPath -Raw | ConvertFrom-Json
            if ($configuration.gitHubUrl -ne $repositoryUrl -or $configuration.agentName -ne $runnerName -or
                -not (Test-Path -LiteralPath (Join-Path $directory 'run.cmd')) -or
                (Test-Path -LiteralPath (Join-Path $directory '.service'))) {
                throw 'Существующий runner относится к другой конфигурации или службе. Он не будет изменён.'
            }
        }
    }
    $runPath = (Join-Path $directory 'run.cmd')
    $definition = @{
        Name = $taskName; User = $context.User; Sid = $context.Sid
        LogonType = 'Interactive'; RunLevel = 'Limited'; Directory = $directory
        Executable = (Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe')
        Arguments = '-NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -Command "& ''' + $runPath + '''; exit $LASTEXITCODE"'
    }
    $existingTask = Get-BonfireRunnerTask $taskName
    if ($existingTask -and (
        $existingTask.Principal.UserId -notin @($context.User, $context.Sid) -or
        [string]$existingTask.Principal.LogonType -ne 'Interactive' -or
        [string]$existingTask.Principal.RunLevel -ne 'Limited' -or
        @($existingTask.Actions).Count -ne 1 -or
        $existingTask.Actions[0].Execute -ne $definition.Executable -or
        $existingTask.Actions[0].Arguments -ne $definition.Arguments)) {
        throw 'Задача с таким именем уже существует с другой конфигурацией. Она не будет заменена.'
    }
    if (-not $PSCmdlet.ShouldProcess($directory, "Зарегистрировать $runnerName для $repositoryUrl; автозапуск при входе $($context.User)")) { return }
    if (-not (Test-Path -LiteralPath $configurationPath)) {
        [IO.Directory]::CreateDirectory($directory) | Out-Null
        $archive = Join-Path $directory $asset.Name
        Save-BonfireRunnerPackage $asset $archive $RunnerArchive
        Test-BonfireRunnerPackage $archive $asset.Hash
        Expand-BonfireRunnerPackage $archive $directory
        Register-BonfireRunner $directory $repositoryUrl $runnerName
        if (-not (Test-Path -LiteralPath $configurationPath)) { throw 'GitHub не создал файл регистрации runner.' }
        Remove-Item -LiteralPath $archive
    }
    if (-not $existingTask) { Register-BonfireRunnerTask $definition }
    if (-not $existingTask -or [string]$existingTask.State -ne 'Running') { Start-BonfireRunnerTask $taskName }
    Write-Host "Runner настроен: $runnerName. Задача: $taskName. Каталог: $directory"
    Write-Host 'Проверьте статус Idle в GitHub Settings → Actions → Runners. При выходе из Windows runner недоступен.'
    [pscustomobject]@{ RunnerDirectory = $directory; TaskName = $taskName; RepositoryUrl = $repositoryUrl }
}

if ($MyInvocation.InvocationName -ne '.') {
    $ErrorActionPreference = 'Stop'
    Install-BonfireRunner -RunnerDirectory $RunnerDirectory -RunnerArchive $RunnerArchive -WhatIf:$WhatIf
}
