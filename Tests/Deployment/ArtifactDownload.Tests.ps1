$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '../../scripts/Get-BonfireArtifact.ps1')
$realTransfer = ${function:Invoke-BonfireArtifactTransfer}
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot ('../../artifacts/artifact-tests/' + [guid]::NewGuid().ToString('N'))))
[IO.Directory]::CreateDirectory($root) | Out-Null
$script:passed = 0
function Assert-True($Condition, $Message) { if (-not $Condition) { throw $Message } }
function Test-Case($Name, $Action) { & $Action; $script:passed++; Write-Host "PASS: $Name" }
function New-Fixture {
    $script:fixture = Join-Path $root ([guid]::NewGuid().ToString('N'))
    [IO.Directory]::CreateDirectory((Join-Path $script:fixture 'input')) | Out-Null
    [IO.File]::WriteAllText((Join-Path $script:fixture 'input/Bonfire.exe'), 'verified executable')
    Compress-Archive -LiteralPath (Join-Path $script:fixture 'input/Bonfire.exe') -DestinationPath (Join-Path $script:fixture 'source.zip')
    $script:bytes = [IO.File]::ReadAllBytes((Join-Path $script:fixture 'source.zip'))
    $script:asset = [pscustomobject]@{ Id=42; Digest=('sha256:' + (Get-FileHash (Join-Path $script:fixture 'source.zip')).Hash.ToLowerInvariant()); Size=$script:bytes.Length }
    $script:offsets = @(); $script:urls = @(); $script:mode = 'resume'; $script:links = 0
    $script:requested = @(); $script:timeouts = @()
    return @{Destination=(Join-Path $script:fixture 'publish'); CacheDirectory=(Join-Path $script:fixture 'cache')}
}
function Get-BonfireArtifactMetadata($Repository, $RunId, $Name) {
    $script:requested = @($Repository, $RunId, $Name)
    return [pscustomobject]@{Success=$true; Asset=$script:asset}
}
function Get-BonfireArtifactUrl($Repository, $Id) {
    $script:links++
    return [pscustomobject]@{Success=$true; Url="https://example.invalid/signed-$script:links"}
}
function Invoke-BonfireArtifactTransfer($Uri, $Path, $TimeoutSeconds) {
    $offset = if (Test-Path -LiteralPath $Path) { (Get-Item -LiteralPath $Path).Length } else { 0 }
    $script:offsets += $offset; $script:urls += $Uri; $script:timeouts += $TimeoutSeconds
    if ($script:mode -eq 'range' -and $script:offsets.Count -eq 1) {
        [IO.File]::WriteAllBytes($Path, $script:bytes[0..9]); return 33
    }
    if ($script:mode -eq 'bad') { [IO.File]::WriteAllText($Path, 'corrupt'); return 0 }
    if ($script:mode -in @('resume','expired') -and $script:offsets.Count -eq 1 -or $script:mode -eq 'timeout') {
        [IO.File]::WriteAllBytes($Path, $script:bytes[0..9]); if ($script:mode -eq 'expired') { return 22 }; return 28
    }
    $stream = [IO.File]::Open($Path, 'Append', 'Write', 'None')
    try { $stream.Write($script:bytes, $offset, $script:bytes.Length - $offset) }
    finally { $stream.Dispose() }
    return 0
}
function Wait-BonfireArtifactRetry { }
function Receive-TestArtifact($f) {
    Receive-BonfireArtifact -Repository 'owner/repo' -RunId 17 -Destination $f.Destination -CacheDirectory $f.CacheDirectory
}
Test-Case 'Обрыв продолжает файл со свежей ссылкой и проверяет содержимое' {
    $f = New-Fixture; $result = Receive-TestArtifact $f
    Assert-True $result.Success $result.Error
    Assert-True (($script:offsets -join ',') -eq '0,10') 'Частичный файл не продолжен.'
    Assert-True ($script:urls[0] -ne $script:urls[1]) 'Ссылка не обновлена.'
    Assert-True (($script:requested -join ',') -eq 'owner/repo,17,bonfire-win-x64') 'Читается чужой run/name.'
    Assert-True (($script:timeouts | Where-Object { $_ -ne 900 }).Count -eq 0) 'Не задан лимит попытки 900 секунд.'
    Assert-True ([IO.File]::ReadAllText((Join-Path $f.Destination 'Bonfire.exe')) -eq 'verified executable') 'Не распакован проверенный файл.'
}
Test-Case 'Пятнадцать минут на попытку разрешены, превышение лимита отвергается' {
    $f=New-Fixture; $script:mode='success'
    $result=Receive-BonfireArtifact -Repository 'owner/repo' -RunId 17 -Destination $f.Destination -CacheDirectory $f.CacheDirectory -AttemptTimeoutSeconds 900
    Assert-True $result.Success $result.Error
    Assert-True ($script:timeouts[0] -eq 900) 'Транспорт получил другой лимит.'
    $f=New-Fixture
    $result=Receive-BonfireArtifact -Repository 'owner/repo' -RunId 17 -Destination $f.Destination -CacheDirectory $f.CacheDirectory -AttemptTimeoutSeconds 901
    Assert-True (-not $result.Success -and $script:offsets.Count -eq 0) 'Превышение лимита не отклонено до сети.'
}
Test-Case 'Бюджеты workflow вмещают скачивание с повторами и установку' {
    $workflow=Get-Content -LiteralPath (Join-Path $PSScriptRoot '../../.github/workflows/deploy-monoblock.yml') -Raw -Encoding UTF8
    $deploy=($workflow -split '(?m)^  deploy:',2)[1]
    $jobLimit=[int][regex]::Match($deploy,'(?m)^    timeout-minutes: (\d+)').Groups[1].Value
    $downloadLimit=[int][regex]::Match($deploy,'(?m)^        timeout-minutes: (\d+)').Groups[1].Value
    Assert-True ($downloadLimit -ge (3*900/60+2)) 'Шаг обрывает три попытки по 15 минут без запаса на API и распаковку.'
    Assert-True ($jobLimit -ge $downloadLimit+10) 'Нет запаса на подтверждение, бэкап и установку.'
}
Test-Case 'Неверный SHA не распаковывается и не создаёт выход' {
    $f = New-Fixture; $script:mode='bad'; $result=Receive-TestArtifact $f
    Assert-True (-not $result.Success -and $script:offsets.Count -eq 3) 'Нет ограниченного отказа.'
    Assert-True (-not (Test-Path -LiteralPath $f.Destination)) 'Непроверенные данные опубликованы.'
    Assert-True (($script:offsets -join ',') -eq '0,0,0') 'Повреждённые байты продолжены.'
}
Test-Case 'Тайм-аут ограничен тремя попытками и сохраняет partial' {
    $f = New-Fixture; $script:mode='timeout'; $result=Receive-TestArtifact $f
    Assert-True (-not $result.Success -and $script:offsets.Count -eq 3) 'Число попыток не ограничено.'
    Assert-True (-not (Test-Path -LiteralPath $f.Destination)) 'Создан выход после отказа.'
    Assert-True (@(Get-ChildItem -LiteralPath $f.CacheDirectory -Filter '*.part').Count -eq 1) 'Partial потерян.'
}
Test-Case 'Отказ Range начинает следующую попытку с нуля' {
    $f=New-Fixture; $script:mode='range'; $result=Receive-TestArtifact $f
    Assert-True $result.Success $result.Error
    Assert-True (($script:offsets -join ',') -eq '0,0') 'Отказ Range не сбросил partial.'
}
Test-Case 'Истёкшая ссылка HTTP не уничтожает partial перед новой ссылкой' {
    $f=New-Fixture; $script:mode='expired'; $result=Receive-TestArtifact $f
    Assert-True $result.Success $result.Error
    Assert-True (($script:offsets -join ',') -eq '0,10') 'HTTP-ошибка уничтожила полученные байты.'
}
Test-Case 'Непустой выход не изменяется' {
    $f=New-Fixture; [IO.Directory]::CreateDirectory($f.Destination) | Out-Null
    [IO.File]::WriteAllText((Join-Path $f.Destination 'keep.txt'),'keep')
    $result=Receive-TestArtifact $f
    Assert-True (-not $result.Success -and $script:offsets.Count -eq 0) 'Начато скачивание поверх данных.'
    Assert-True ([IO.File]::ReadAllText((Join-Path $f.Destination 'keep.txt')) -eq 'keep') 'Старые данные изменены.'
}
Test-Case 'Отсутствующий digest запрещает передачу' {
    $f=New-Fixture; $script:asset.Digest=''; $result=Receive-TestArtifact $f
    Assert-True (-not $result.Success -and $script:offsets.Count -eq 0) 'Скачивается артефакт без digest.'
}
Test-Case 'Проверенный кеш не скачивается повторно' {
    $f=New-Fixture; $script:mode='success'; $result=Receive-TestArtifact $f
    Assert-True $result.Success $result.Error
    $script:offsets=@(); $script:urls=@()
    $f.Destination=Join-Path $script:fixture 'another-publish'; $result=Receive-TestArtifact $f
    Assert-True ($result.Success -and $script:offsets.Count -eq 0) 'Готовый проверенный кеш скачивается заново.'
}
Test-Case 'Даже верный digest не разрешает распаковку повреждённого ZIP' {
    $f=New-Fixture; $script:mode='success'; $script:bytes=[Text.Encoding]::UTF8.GetBytes('not zip')
    [IO.File]::WriteAllBytes((Join-Path $script:fixture 'source.zip'),$script:bytes)
    $script:asset.Size=$script:bytes.Length
    $script:asset.Digest='sha256:'+(Get-FileHash -LiteralPath (Join-Path $script:fixture 'source.zip')).Hash.ToLowerInvariant()
    $result=Receive-TestArtifact $f
    Assert-True (-not $result.Success -and -not (Test-Path -LiteralPath $f.Destination)) 'Повреждённый ZIP опубликован.'
}
Test-Case 'ZIP с выходом из каталога не публикуется' {
    $f=New-Fixture; $script:mode='success'; $archive=Join-Path $script:fixture 'escape.zip'
    $zip=[IO.Compression.ZipFile]::Open($archive,[IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($name in @('../escaped.txt','Bonfire.exe')) {
            $stream=$zip.CreateEntry($name).Open()
            try { $data=[Text.Encoding]::UTF8.GetBytes('unexpected'); $stream.Write($data,0,$data.Length) }
            finally { $stream.Dispose() }
        }
    }
    finally { $zip.Dispose() }
    $script:bytes=[IO.File]::ReadAllBytes($archive); $script:asset.Size=$script:bytes.Length
    $script:asset.Digest='sha256:'+(Get-FileHash -LiteralPath $archive).Hash.ToLowerInvariant()
    $result=Receive-TestArtifact $f
    Assert-True (-not $result.Success -and -not (Test-Path -LiteralPath (Join-Path $script:fixture 'escaped.txt'))) 'ZIP вышел из staging.'
}
Test-Case 'Настоящий curl продолжает Range, ограничивает timeout и не передаёт Authorization' {
    $originalInputEncoding = [Console]::InputEncoding
    $serverRoot=Join-Path $root 'server'; [IO.Directory]::CreateDirectory($serverRoot) | Out-Null
    $data=[byte[]](0..255); [IO.File]::WriteAllBytes((Join-Path $serverRoot 'data.bin'),$data)
    $serverScript = Join-Path $PSScriptRoot 'ArtifactDownload.Server.ps1'
    $serverProcess = Start-Process -FilePath "$env:SystemRoot\System32\WindowsPowerShell\v1.0\powershell.exe" -ArgumentList @('-NoProfile','-ExecutionPolicy','Bypass','-File',('"' + $serverScript + '"'),('"' + $serverRoot + '"')) -WindowStyle Hidden -PassThru
    try {
        [Console]::InputEncoding = [Text.UTF8Encoding]::new($true)
        $wait=[Diagnostics.Stopwatch]::StartNew()
        while (-not (Test-Path -LiteralPath (Join-Path $serverRoot 'port.txt')) -and $wait.Elapsed.TotalSeconds -lt 10) { Start-Sleep -Milliseconds 50 }
        Assert-True (Test-Path -LiteralPath (Join-Path $serverRoot 'port.txt')) 'TCP-сервер не запущен.'
        $port=[IO.File]::ReadAllText((Join-Path $serverRoot 'port.txt')); $part=Join-Path $serverRoot 'частичный файл.bin'
        $code=& $realTransfer "http://127.0.0.1:$port/resume" $part 3
        Assert-True ([Console]::InputEncoding.CodePage -eq 65001 -and [Console]::InputEncoding.GetPreamble().Length -eq 3) 'Кодировка консоли не восстановлена.'
        Assert-True ($code -eq 18 -and (Get-Item -LiteralPath $part).Length -eq 10) 'Обрыв не сохранил partial.'
        $code=& $realTransfer "http://127.0.0.1:$port/resume" $part 3
        Assert-True ($code -eq 0) 'Range не продолжен.'
        Assert-True ((Get-FileHash -LiteralPath $part).Hash -eq (Get-FileHash -LiteralPath (Join-Path $serverRoot 'data.bin')).Hash) 'После Range неверные байты.'
        [IO.File]::WriteAllBytes($part,$data[0..9])
        $code=& $realTransfer "http://127.0.0.1:$port/ignore" $part 3
        Assert-True ($code -eq 33 -and (Get-Item -LiteralPath $part).Length -eq 10) 'Нет отказа на игнорирование Range.'
        $code=& $realTransfer "http://127.0.0.1:$port/range416" $part 3
        Assert-True ($code -eq 33 -and (Get-Item -LiteralPath $part).Length -eq 10) 'HTTP 416 не распознан как отказ Range.'
        $code=& $realTransfer 'http://example.invalid/file' $part 3
        Assert-True ($code -eq 64) 'Разрешён внешний HTTP.'
        $clock=[Diagnostics.Stopwatch]::StartNew()
        $code=& $realTransfer "http://127.0.0.1:$port/stall" (Join-Path $serverRoot 'stall.bin') 1
        Assert-True ($code -eq 28 -and $clock.Elapsed.TotalSeconds -lt 6) 'Timeout не ограничил настоящий curl.'
        $requests=[IO.File]::ReadAllText((Join-Path $serverRoot 'requests.txt'))
        Assert-True ($requests -match '\|10\|' -and $requests -notmatch 'Authorization:') 'Range отсутствует или утекла авторизация.'
    }
    finally { if (-not $serverProcess.HasExited) { $serverProcess.Kill(); $serverProcess.WaitForExit() }; $serverProcess.Dispose(); [Console]::InputEncoding = $originalInputEncoding }
}
Write-Host "ARTIFACT_DOWNLOAD_OK: $script:passed"
