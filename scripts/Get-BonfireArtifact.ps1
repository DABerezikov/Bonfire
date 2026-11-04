[CmdletBinding()]
param([string]$Repository = $env:GITHUB_REPOSITORY, [long]$RunId = $env:GITHUB_RUN_ID,
    [string]$Destination = './artifacts/publish', [string]$CacheDirectory,
    [int]$Attempts = 3, [int]$AttemptTimeoutSeconds = 120)

function Get-BonfireArtifactMetadata($Repository, $RunId, $Name) {
    if (-not $env:GH_TOKEN) { return [pscustomobject]@{Success=$false; Error='Не задан GH_TOKEN.'} }
    $headers = @{Authorization=('Bearer ' + $env:GH_TOKEN); Accept='application/vnd.github+json'; 'User-Agent'='Bonfire-deploy'}
    try {
        $response = Invoke-RestMethod -Uri "https://api.github.com/repos/$Repository/actions/runs/$RunId/artifacts?per_page=100" -Headers $headers -TimeoutSec 15 -ErrorAction Stop
    }
    catch { return [pscustomobject]@{Success=$false; Error='Не удалось прочитать список артефактов GitHub за 15 секунд.'} }
    $matches = @($response.artifacts | Where-Object { $_.name -eq $Name -and -not $_.expired })
    if ($matches.Count -ne 1) { return [pscustomobject]@{Success=$false; Error='Не найден единственный действующий артефакт текущего запуска.'} }
    return [pscustomobject]@{Success=$true; Asset=[pscustomobject]@{Id=$matches[0].id; Digest=$matches[0].digest; Size=$matches[0].size_in_bytes}}
}

function Get-BonfireArtifactUrl($Repository, $Id) {
    $request = [Net.HttpWebRequest]::Create("https://api.github.com/repos/$Repository/actions/artifacts/$Id/zip")
    $request.AllowAutoRedirect = $false
    $request.Timeout = 15000; $request.ReadWriteTimeout = 15000
    $request.UserAgent = 'Bonfire-deploy'; $request.Accept = 'application/vnd.github+json'
    $request.Headers['Authorization'] = 'Bearer ' + $env:GH_TOKEN
    try {
        $response = $request.GetResponse()
        try {
            $uri = $null
            if ([int]$response.StatusCode -ne 302 -or
                -not [Uri]::TryCreate($response.Headers['Location'], [UriKind]::Absolute, [ref]$uri) -or $uri.Scheme -ne 'https') {
                return [pscustomobject]@{Success=$false; Error='GitHub не вернул безопасную ссылку скачивания.'}
            }
            return [pscustomobject]@{Success=$true; Url=$uri.AbsoluteUri}
        }
        finally { $response.Dispose() }
    }
    catch [Net.WebException] {
        if ($_.Exception.Response) { $_.Exception.Response.Dispose() }
        return [pscustomobject]@{Success=$false; Error='Не удалось обновить ссылку скачивания GitHub за 15 секунд.'}
    }
}

function Invoke-BonfireArtifactTransfer($Uri, $Path, $TimeoutSeconds) {
    $address = $null
    if (-not [Uri]::TryCreate($Uri, [UriKind]::Absolute, [ref]$address)) { return 64 }
    # HTTP разрешён только для изолированных loopback-тестов транспорта; API всегда требует HTTPS.
    if ($address.Scheme -ne 'https' -and -not ($address.Scheme -eq 'http' -and $address.IsLoopback)) { return 64 }
    $curl = Get-Command curl.exe -ErrorAction SilentlyContinue
    if (-not $curl) { return 127 }
    $protocol = if ($address.Scheme -eq 'https') { '=https' } else { '=http' }
    $info = [Diagnostics.ProcessStartInfo]::new()
    $info.FileName = $curl.Source
    $info.Arguments = "--disable --config - --silent --fail --location --proto $protocol --proto-redir =https --connect-timeout 15 --max-time $TimeoutSeconds --speed-limit 16384 --speed-time 30 --continue-at - --write-out %{http_code}"
    $info.UseShellExecute = $false; $info.CreateNoWindow = $true
    $info.RedirectStandardInput = $true; $info.RedirectStandardOutput = $true
    $info.EnvironmentVariables.Remove('GH_TOKEN')
    $info.EnvironmentVariables.Remove('GITHUB_TOKEN')
    $process = [Diagnostics.Process]::new(); $process.StartInfo = $info
    try {
        # .NET Framework берёт кодировку stdin из Console.InputEncoding уже при Start.
        # В PowerShell 5.1 нет ProcessStartInfo.StandardInputEncoding; исключаем BOM явно.
        $originalInputEncoding = [Console]::InputEncoding
        try {
            [Console]::InputEncoding = [Text.UTF8Encoding]::new($false)
            $started = $process.Start()
        }
        finally { [Console]::InputEncoding = $originalInputEncoding }
        if (-not $started) { return 127 }
        # URL с временным SAS передаётся через stdin, не попадает в argv и журнал.
        $escapedUrl = $Uri.Replace('\', '\\').Replace('"', '\"')
        $escapedPath = $Path.Replace('\', '\\').Replace('"', '\"')
        $process.StandardInput.WriteLine('url = "' + $escapedUrl + '"')
        $process.StandardInput.WriteLine('output = "' + $escapedPath + '"')
        $process.StandardInput.Close()
        if (-not $process.WaitForExit(($TimeoutSeconds + 5) * 1000)) {
            $process.Kill(); $process.WaitForExit(); return 28
        }
        $httpCode = $process.StandardOutput.ReadToEnd().Trim()
        if ($httpCode -eq '416') { return 33 }
        return $process.ExitCode
    }
    finally { $process.Dispose() }
}

function Wait-BonfireArtifactRetry { Start-Sleep -Seconds 2 }

function Expand-BonfireVerifiedArtifact($Archive, $Destination) {
    $parent = [IO.Path]::GetDirectoryName($Destination)
    [IO.Directory]::CreateDirectory($parent) | Out-Null
    $stage = Join-Path $parent ('.bonfire-extract-' + [guid]::NewGuid().ToString('N'))
    try {
        Expand-Archive -LiteralPath $Archive -DestinationPath $stage -ErrorAction Stop
        if (-not (Test-Path -LiteralPath (Join-Path $stage 'Bonfire.exe') -PathType Leaf)) {
            return [pscustomobject]@{Success=$false; Error='Проверенный архив не содержит Bonfire.exe.'}
        }
        if ([IO.Directory]::Exists($Destination)) { [IO.Directory]::Delete($Destination, $false) }
        [IO.Directory]::Move($stage, $Destination)
        return [pscustomobject]@{Success=$true; Destination=$Destination; Error=''}
    }
    catch { return [pscustomobject]@{Success=$false; Error='Не удалось распаковать проверенный архив в отдельную папку; установка не начата.'} }
}

function Receive-BonfireArtifact {
    param([string]$Repository, [long]$RunId, [string]$Destination, [string]$CacheDirectory,
        [int]$Attempts = 3, [int]$AttemptTimeoutSeconds = 120)
    if ($Repository -notmatch '^[a-zA-Z0-9_.-]+/[a-zA-Z0-9_.-]+$' -or $RunId -le 0 -or
        [string]::IsNullOrWhiteSpace($Destination) -or $Attempts -lt 1 -or $Attempts -gt 3 -or
        $AttemptTimeoutSeconds -lt 1 -or $AttemptTimeoutSeconds -gt 120) {
        return [pscustomobject]@{Success=$false; Error='Неверные параметры загрузки.'}
    }
    $output = [IO.Path]::GetFullPath($Destination)
    if ((Test-Path -LiteralPath $output -PathType Leaf) -or
        ((Test-Path -LiteralPath $output -PathType Container) -and @(Get-ChildItem -LiteralPath $output -Force).Count -gt 0)) {
        return [pscustomobject]@{Success=$false; Error='Папка публикации должна отсутствовать или быть пустой.'}
    }
    if (-not $CacheDirectory) {
        $temporary = if ($env:RUNNER_TEMP) { $env:RUNNER_TEMP } else { [IO.Path]::GetTempPath() }
        $CacheDirectory = Join-Path $temporary 'bonfire-download'
    }
    $cache = [IO.Path]::GetFullPath($CacheDirectory)
    if ((Test-Path -LiteralPath $cache) -and ((Get-Item -LiteralPath $cache -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
        return [pscustomobject]@{Success=$false; Error='Кеш не должен быть ссылкой.'}
    }
    [IO.Directory]::CreateDirectory($cache) | Out-Null
    $asset = $null; $lastError = 'Скачивание не завершено.'
    for ($attempt = 1; $attempt -le $Attempts; $attempt++) {
        if ($null -eq $asset) {
            $metadata = Get-BonfireArtifactMetadata $Repository $RunId 'bonfire-win-x64'
            if (-not $metadata.Success) {
                $lastError = $metadata.Error
                if ($attempt -lt $Attempts) { Wait-BonfireArtifactRetry }
                continue
            }
            $id = 0L; $size = 0L
            if (-not [long]::TryParse([string]$metadata.Asset.Id, [ref]$id) -or $id -le 0 -or
                -not [long]::TryParse([string]$metadata.Asset.Size, [ref]$size) -or $size -le 0 -or
                $metadata.Asset.Digest -notmatch '^sha256:[a-fA-F0-9]{64}$') {
                return [pscustomobject]@{Success=$false; Error='GitHub не предоставил корректные id, size и SHA-256 артефакта.'}
            }
            $asset = $metadata.Asset
            $digest = $asset.Digest.Substring(7).ToLowerInvariant()
            $archive = Join-Path $cache ("$id-$digest.zip")
            $part = $archive + '.part'
        }
        foreach ($file in @($archive, $part)) {
            if (-not (Test-Path -LiteralPath $file -PathType Leaf)) { continue }
            if ((Get-Item -LiteralPath $file -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) {
                return [pscustomobject]@{Success=$false; Error='Файл кеша не должен быть ссылкой.'}
            }
            $length = (Get-Item -LiteralPath $file).Length
            if ($length -eq $size -and (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash.ToLowerInvariant() -eq $digest) {
                if ($file -eq $part) { [IO.File]::Move($part, $archive) }
                return Expand-BonfireVerifiedArtifact $archive $output
            }
            if ($file -eq $archive -or $length -ge $size) { [IO.File]::Delete($file) }
        }
        $link = Get-BonfireArtifactUrl $Repository $id
        if (-not $link.Success) { $lastError = $link.Error }
        else {
            $offset = if (Test-Path -LiteralPath $part) { (Get-Item -LiteralPath $part).Length } else { 0 }
            Write-Host "Попытка $attempt/$Attempts, продолжение с $offset из $size байт, предел $AttemptTimeoutSeconds секунд."
            $code = Invoke-BonfireArtifactTransfer $link.Url $part $AttemptTimeoutSeconds
            if ($code -eq 0 -and (Test-Path -LiteralPath $part) -and
                (Get-Item -LiteralPath $part).Length -eq $size -and
                (Get-FileHash -LiteralPath $part -Algorithm SHA256).Hash.ToLowerInvariant() -eq $digest) {
                [IO.File]::Move($part, $archive)
                return Expand-BonfireVerifiedArtifact $archive $output
            }
            $lastError = "Передача не завершена или SHA-256 не совпал (curl $code)."
            if ($code -eq 0 -or $code -eq 33) {
                # Удаляется только собственный файл с ключом id + digest, никогда каталог.
                if (Test-Path -LiteralPath $part -PathType Leaf) { [IO.File]::Delete($part) }
            }
        }
        Write-Host $lastError
        if ($attempt -lt $Attempts) { Wait-BonfireArtifactRetry }
    }
    return [pscustomobject]@{Success=$false; Error="$lastError Попытки исчерпаны; установка не начата."}
}

if ($MyInvocation.InvocationName -ne '.') {
    $ErrorActionPreference = 'Stop'
    try {
        $result = Receive-BonfireArtifact -Repository $Repository -RunId $RunId -Destination $Destination -CacheDirectory $CacheDirectory -Attempts $Attempts -AttemptTimeoutSeconds $AttemptTimeoutSeconds
    }
    catch { $result = [pscustomobject]@{Success=$false; Error='Системная ошибка доступа к файлам или запуска curl; установка не начата.'} }
    if (-not $result.Success) { Write-Error $result.Error -ErrorAction Continue; exit 1 }
    Write-Host "Публикация проверена и подготовлена: $($result.Destination)"
}
