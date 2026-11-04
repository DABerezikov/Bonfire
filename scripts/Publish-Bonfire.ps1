[CmdletBinding()]
param([string]$Destination='\\MONOBLOCK\Bonfire')

function Invoke-BonfireLocalGit($Arguments) {
    $ErrorActionPreference='Continue'
    $lines=@(& git @Arguments 2>&1 | ForEach-Object { [string]$_ })
    return [pscustomobject]@{Success=($LASTEXITCODE -eq 0); Lines=$lines; Error=($lines -join "`n")}
}

function Get-BonfirePublishState {
    $branch=Invoke-BonfireLocalGit @('symbolic-ref','--quiet','--short','HEAD')
    if(-not $branch.Success -or ($branch.Lines -join '') -ne 'master') {
        return [pscustomobject]@{Success=$false;Error='git publish запускается только из master.'}
    }
    $dirty=Invoke-BonfireLocalGit @('status','--porcelain=v1','--untracked-files=no')
    if(-not $dirty.Success -or $dirty.Lines.Count -gt 0) {
        return [pscustomobject]@{Success=$false;Error='Сначала закоммитьте изменения отслеживаемых файлов.'}
    }
    $expectedOrigin='^(https://github\.com/|git@github\.com:)DABerezikov/Bonfire(\.git)?/?$'
    $origin=Invoke-BonfireLocalGit @('remote','get-url','origin')
    $pushOrigin=Invoke-BonfireLocalGit @('remote','get-url','--push','origin')
    if(-not $origin.Success -or -not $pushOrigin.Success -or
        ($origin.Lines -join '') -notmatch $expectedOrigin -or ($pushOrigin.Lines -join '') -notmatch $expectedOrigin) {
        return [pscustomobject]@{Success=$false;Error='Адреса fetch и push origin должны указывать на DABerezikov/Bonfire.'}
    }
    $head=Invoke-BonfireLocalGit @('rev-parse','--verify','HEAD')
    $root=Invoke-BonfireLocalGit @('rev-parse','--show-toplevel')
    $common=Invoke-BonfireLocalGit @('rev-parse','--git-common-dir')
    if(-not $head.Success -or -not $root.Success -or -not $common.Success) {
        return [pscustomobject]@{Success=$false;Error='Не удалось определить состояние репозитория.'}
    }
    $repositoryRoot=$root.Lines -join ''
    $commonPath=$common.Lines -join ''
    if(-not [IO.Path]::IsPathRooted($commonPath)){$commonPath=Join-Path $repositoryRoot $commonPath}
    return [pscustomobject]@{Success=$true;Commit=($head.Lines -join '');Root=$repositoryRoot;CommonDirectory=[IO.Path]::GetFullPath($commonPath)}
}

function Open-BonfirePublishLock($Directory) {
    try {
        $handle=[IO.File]::Open((Join-Path $Directory 'bonfire-publish.lock'),'OpenOrCreate','ReadWrite','None')
        return [pscustomobject]@{Success=$true;Handle=$handle}
    }
    catch [IO.IOException] { return [pscustomobject]@{Success=$false;Error='Уже выполняется git publish или файл блокировки недоступен.'} }
}

function Push-BonfirePublishCommit($Commit) {
    $result=Invoke-BonfireLocalGit @('push','origin',($Commit + ':refs/heads/master'))
    if($result.Lines.Count -gt 0){Write-Host ($result.Lines -join "`n")}
    return $result
}

function Test-BonfirePublishRemote($Commit) {
    $remote=Invoke-BonfireLocalGit @('ls-remote','--exit-code','origin','refs/heads/master')
    if(-not $remote.Success -or $remote.Lines.Count -ne 1 -or ($remote.Lines[0] -split '\s+')[0] -ne $Commit) {
        return [pscustomobject]@{Success=$false;Error='origin/master недоступен или уже указывает на другой коммит; устаревшая установка отменена.'}
    }
    return [pscustomobject]@{Success=$true}
}

function Test-BonfirePublishInstalled($Destination,$Commit) {
    $path=Join-Path $Destination '.bonfire-version.json'
    if(-not (Test-Path -LiteralPath $path -PathType Leaf)){return [pscustomobject]@{Success=$true;Installed=$false}}
    try { $manifest=Get-Content -LiteralPath $path -Raw -Encoding UTF8 -ErrorAction Stop | ConvertFrom-Json -ErrorAction Stop }
    catch { return [pscustomobject]@{Success=$false;Error='Не удалось прочитать версию установки. Проверьте доступ к MONOBLOCK.'} }
    return [pscustomobject]@{Success=$true;Installed=($manifest.Commit -eq $Commit -and (Test-Path -LiteralPath (Join-Path $Destination 'Bonfire.exe') -PathType Leaf))}
}

function New-BonfirePublishSnapshot($Commit) {
    $parent=Join-Path ([IO.Path]::GetTempPath()) 'Bonfire-local-publish'
    $root=Join-Path $parent ([guid]::NewGuid().ToString('N'))
    if((Test-Path -LiteralPath $parent) -and ((Get-Item -LiteralPath $parent -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)){return [pscustomobject]@{Success=$false;Error='Родитель снимка не должен быть ссылкой.'}}
    [IO.Directory]::CreateDirectory($root) | Out-Null
    $snapshot=[pscustomobject]@{Success=$true;Root=$root;Source=(Join-Path $root 'source');Publish=(Join-Path $root 'publish');Build=(Join-Path $root 'build')}
    $archive=Join-Path $root 'source.zip'
    $result=Invoke-BonfireLocalGit @('archive','--format=zip',('--output='+$archive),$Commit)
    if(-not $result.Success){return [pscustomobject]@{Success=$false;Error="Не удалось создать снимок $Commit. Диагностика: $root"}}
    Expand-Archive -LiteralPath $archive -DestinationPath $snapshot.Source -ErrorAction Stop
    return $snapshot
}

function Build-BonfirePublishSnapshot($Snapshot,$Commit) {
    $ErrorActionPreference='Continue'
    Push-Location $Snapshot.Source
    try {
        Write-Host "Release-гейт снимка $Commit"
        & dotnet test Bonfire.sln -c Release --artifacts-path $Snapshot.Build --nologo | Out-Host
        if($LASTEXITCODE -ne 0){return [pscustomobject]@{Success=$false;Error='Release-гейт не прошёл.'}}
        foreach($test in @('LocalPublish.Tests.ps1','LocalPublish.Git.Tests.ps1','ArtifactDownload.Tests.ps1','Deploy.Tests.ps1','Runner.Tests.ps1','InteractiveUpdate.Tests.ps1','ClosedUpdate.Integration.Tests.ps1','WorkflowEncoding.Tests.ps1','Confirmation.Wpf.Tests.ps1')) {
            & "$env:SystemRoot\System32\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -STA -ExecutionPolicy Bypass -File (Join-Path $Snapshot.Source ('Tests/Deployment/'+$test)) | Out-Host
            if($LASTEXITCODE -ne 0){return [pscustomobject]@{Success=$false;Error="Не прошла проверка $test."}}
        }
        & dotnet run --project Tests/Startup.Smoke/Startup.Smoke.csproj -c Release --artifacts-path $Snapshot.Build | Out-Host
        if($LASTEXITCODE -ne 0){return [pscustomobject]@{Success=$false;Error='Проверка запуска/завершения не прошла.'}}
        & dotnet publish Bonfire/Bonfire.csproj -p:PublishProfile=FolderProfile ("-p:SourceRevisionId=$Commit") --artifacts-path $Snapshot.Build -o $Snapshot.Publish --nologo | Out-Host
        if($LASTEXITCODE -ne 0){return [pscustomobject]@{Success=$false;Error='Публикация не собрана.'}}
        & "$env:SystemRoot\System32\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -ExecutionPolicy Bypass -File (Join-Path $Snapshot.Source 'Tests/Deployment/SingleFile.Tests.ps1') -PublishDirectory $Snapshot.Publish | Out-Host
        if($LASTEXITCODE -ne 0){return [pscustomobject]@{Success=$false;Error='Проверка единого exe не прошла.'}}
        return [pscustomobject]@{Success=$true}
    }
    finally { Pop-Location }
}

function Install-BonfirePublishSnapshot($Snapshot,$Destination,$Commit) {
    $ErrorActionPreference='Continue'
    & "$env:SystemRoot\System32\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -STA -ExecutionPolicy Bypass -File (Join-Path $Snapshot.Source 'scripts/Update-BonfireInteractive.ps1') -PublishDirectory $Snapshot.Publish -Destination $Destination -Version $Commit | Out-Host
    if($LASTEXITCODE -ne 0){return [pscustomobject]@{Success=$false;Error='Обновление отменено или завершилось ошибкой; проверьте доступ и закройте Bonfire на MONOBLOCK.'}}
    return [pscustomobject]@{Success=$true}
}

function Remove-BonfirePublishSnapshot($Snapshot) {
    $parent=[IO.Path]::GetFullPath((Join-Path ([IO.Path]::GetTempPath()) 'Bonfire-local-publish')).TrimEnd('\')
    $path=[IO.Path]::GetFullPath($Snapshot.Root).TrimEnd('\')
    if([IO.Path]::GetDirectoryName($path) -ne $parent -or [IO.Path]::GetFileName($path) -notmatch '^[0-9a-f]{32}$') {
        Write-Warning 'Путь снимка не соответствует рабочему каталогу; очистка пропущена.'; return
    }
    try {
        if(-not (Test-Path -LiteralPath $path)){return}
        if((Get-Item -LiteralPath $path -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) {
            Write-Warning 'Снимок является ссылкой; очистка пропущена.'; return
        }
        Remove-Item -LiteralPath $path -Recurse -Force -ErrorAction Stop
    }
    catch { Write-Warning "Обновление завершено, но временный снимок остался: $path" }

}

function Invoke-BonfirePublish {
    param([string]$Destination='\\MONOBLOCK\Bonfire')
    $pushed=$false; $lock=$null; $snapshot=$null
    try {
        $state=Get-BonfirePublishState
        if(-not $state.Success){return [pscustomobject]@{Success=$false;PushSucceeded=$false;Error=$state.Error}}
        $commit=$state.Commit
        $lock=Open-BonfirePublishLock $state.CommonDirectory
        if(-not $lock.Success){return [pscustomobject]@{Success=$false;PushSucceeded=$false;Error=$lock.Error}}
        Write-Host "Отправка $commit в origin/master"
        $push=Push-BonfirePublishCommit $commit
        if(-not $push.Success){return [pscustomobject]@{Success=$false;PushSucceeded=$false;Error=$push.Error}}
        $pushed=$true
        $remote=Test-BonfirePublishRemote $commit
        if(-not $remote.Success){return [pscustomobject]@{Success=$false;PushSucceeded=$true;Error=$remote.Error}}
        $installed=Test-BonfirePublishInstalled $Destination $commit
        if(-not $installed.Success){return [pscustomobject]@{Success=$false;PushSucceeded=$true;Error=$installed.Error}}
        if($installed.Installed){return [pscustomobject]@{Success=$true;PushSucceeded=$true;Commit=$commit;Status='AlreadyCurrent'}}
        $snapshot=New-BonfirePublishSnapshot $commit
        if(-not $snapshot.Success){return [pscustomobject]@{Success=$false;PushSucceeded=$true;Error=$snapshot.Error}}
        $build=Build-BonfirePublishSnapshot $snapshot $commit
        if(-not $build.Success){return [pscustomobject]@{Success=$false;PushSucceeded=$true;Error="$($build.Error) Снимок: $($snapshot.Root)"}}
        $remote=Test-BonfirePublishRemote $commit
        if(-not $remote.Success){return [pscustomobject]@{Success=$false;PushSucceeded=$true;Error=$remote.Error}}
        $update=Install-BonfirePublishSnapshot $snapshot $Destination $commit
        if(-not $update.Success){return [pscustomobject]@{Success=$false;PushSucceeded=$true;Error="$($update.Error) Снимок: $($snapshot.Root)"}}
        Remove-BonfirePublishSnapshot $snapshot
        return [pscustomobject]@{Success=$true;PushSucceeded=$true;Commit=$commit;Status='Deployed'}
    }
    catch { return [pscustomobject]@{Success=$false;PushSucceeded=$pushed;Error="Системная ошибка локальной публикации: $($_.Exception.Message)"} }
    finally { if($null -ne $lock -and $lock.Success){$lock.Handle.Dispose()} }
}

if($MyInvocation.InvocationName -ne '.') {
    $ErrorActionPreference='Stop'
    $result=Invoke-BonfirePublish -Destination $Destination
    if(-not $result.Success) {
        if($result.PushSucceeded){Write-Host 'Push выполнен. Локальное обновление не завершено; повторите git publish после устранения причины.'}
        Write-Error $result.Error -ErrorAction Continue; exit 1
    }
    Write-Host "git publish: $($result.Status), commit $($result.Commit)"
}
