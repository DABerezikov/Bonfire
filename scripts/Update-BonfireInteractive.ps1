[CmdletBinding()]
param(
    [string]$PublishDirectory,
    [string]$Destination = '\\MONOBLOCK\Bonfire',
    [string]$Version = 'manual',
    [ValidateRange(1, 600)][int]$TimeoutSeconds = 300
)

function Test-BonfireInteractiveSession {
    return [Environment]::UserInteractive -and ([Diagnostics.Process]::GetCurrentProcess().SessionId -ne 0)
}

function Resolve-BonfireUpdateDirectory([string]$Destination) {
    if ($Destination -match '^\\\\([^\\]+)\\([^\\]+)(.*)$' -and $Matches[1] -eq $env:COMPUTERNAME) {
        $shareName = $Matches[2]
        $suffix = $Matches[3].TrimStart('\')
        $shares = @(Get-CimInstance Win32_Share -ErrorAction Stop | Where-Object { $_.Name -eq $shareName })
        if ($shares.Count -ne 1 -or -not $shares[0].Path) { throw 'Не удалось определить локальную папку сетевого ресурса.' }
        if ($suffix) { return Join-Path $shares[0].Path $suffix }
        return $shares[0].Path
    }
    return [IO.Path]::GetFullPath($Destination)
}

function Request-BonfireUpdateApproval($Destination, $Version, $TimeoutSeconds) {
    if ([Threading.Thread]::CurrentThread.ApartmentState -ne 'STA') { throw 'Для диалога запустите PowerShell с -STA.' }
    Add-Type -AssemblyName PresentationFramework
    $reader = [Xml.XmlReader]::Create((Join-Path $PSScriptRoot 'UpdateConfirmation.xaml'))
    try { $window = [Windows.Markup.XamlReader]::Load($reader) }
    finally { $reader.Dispose() }
    $window.DataContext = [pscustomobject]@{ Version = "Версия: $Version"; Destination = "Установка: $Destination" }
    $window.FindName('Approve').Add_Click({ $window.DialogResult = $true })
    $timer = [Windows.Threading.DispatcherTimer]::new()
    $timer.Interval = [TimeSpan]::FromSeconds($TimeoutSeconds)
    $timer.Add_Tick({ $timer.Stop(); $window.DialogResult = $false })
    $timer.Start()
    try { return $window.ShowDialog() -eq $true }
    finally { $timer.Stop() }
}

function Close-BonfireForUpdate($Destination) {
    $executable = [IO.Path]::GetFullPath((Join-Path $Destination 'Bonfire.exe'))
    $session = [Diagnostics.Process]::GetCurrentProcess().SessionId
    foreach ($process in [Diagnostics.Process]::GetProcessesByName('Bonfire')) {
        try {
            if ($process.HasExited) { continue }
            try { $processPath = $process.MainModule.FileName }
            catch { throw 'Не удалось проверить путь работающего Bonfire. Закройте приложение вручную.' }
            # Запуск с UNC и локального пути относится к одной установке.
            $processPath = Resolve-BonfireUpdateDirectory $processPath
            if (-not $processPath.Equals($executable, [StringComparison]::OrdinalIgnoreCase)) { continue }
            if ($process.SessionId -ne $session) { throw 'Bonfire открыт в другом сеансе Windows. Закройте его перед обновлением.' }
            if (-not $process.CloseMainWindow() -or -not $process.WaitForExit(30000)) {
                throw 'Bonfire не завершился штатно за 30 секунд. Обновление отменено; процесс принудительно не завершается.'
            }
        }
        finally { $process.Dispose() }
    }
}

function Invoke-BonfireDeployment($PublishDirectory, $Destination) {
    return & (Join-Path $PSScriptRoot 'Deploy-Bonfire.ps1') -PublishDirectory $PublishDirectory -Destination $Destination
}

function Invoke-BonfireInteractiveUpdate {
    [CmdletBinding(SupportsShouldProcess = $true)]
    param(
        [Parameter(Mandatory = $true)][string]$PublishDirectory,
        [string]$Destination = '\\MONOBLOCK\Bonfire',
        [string]$Version = 'manual',
        [ValidateRange(1, 600)][int]$TimeoutSeconds = 300
    )
    $ErrorActionPreference = 'Stop'
    if (-not (Test-Path -LiteralPath (Join-Path $PublishDirectory 'Bonfire.exe') -PathType Leaf)) { throw 'Нет полной публикации Bonfire.exe.' }
    if (-not (Test-BonfireInteractiveSession)) { throw 'Нужен интерактивный сеанс пользователя для подтверждения обновления.' }
    $directory = Resolve-BonfireUpdateDirectory $Destination
    if (-not $PSCmdlet.ShouldProcess($directory, 'Запросить подтверждение, штатно закрыть Bonfire, создать бэкап и обновить')) { return }
    if (-not (Request-BonfireUpdateApproval $directory $Version $TimeoutSeconds)) {
        return [pscustomobject]@{ Status = 'Cancelled'; Destination = $directory }
    }
    Close-BonfireForUpdate $directory
    $result = Invoke-BonfireDeployment $PublishDirectory $directory
    $manifest = @{ Commit = $Version; InstalledAt = [DateTimeOffset]::Now.ToString('O'); BackupPath = $result.BackupPath }
    $manifest | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $directory '.bonfire-version.json') -Encoding UTF8
    return [pscustomobject]@{ Status = 'Deployed'; Destination = $directory; BackupPath = $result.BackupPath; Version = $Version }
}

if ($MyInvocation.InvocationName -ne '.') {
    $ErrorActionPreference = 'Stop'
    $result = Invoke-BonfireInteractiveUpdate -PublishDirectory $PublishDirectory -Destination $Destination -Version $Version -TimeoutSeconds $TimeoutSeconds
    if ($result.Status -eq 'Cancelled') { Write-Host 'Пользователь отложил обновление или не ответил за отведённое время.'; exit 2 }
    $result
}
