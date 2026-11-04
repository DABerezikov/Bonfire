$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot '../../scripts/Publish-Bonfire.ps1')
$fixture=Join-Path ([IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../artifacts/local-publish-git'))) ([guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($fixture) | Out-Null
function Assert-True($Value,$Message){if(-not $Value){throw $Message}}
function Git-Fixture($Arguments){
    $result=Invoke-BonfireLocalGit $Arguments
    Assert-True $result.Success $result.Error
    return $result
}
Push-Location $fixture
$snapshot=$null
try {
    Git-Fixture @('init','--initial-branch=master') | Out-Null
    Git-Fixture @('config','user.name','Local test') | Out-Null
    Git-Fixture @('config','user.email','local@example.invalid') | Out-Null
    Git-Fixture @('config','core.hooksPath',(Join-Path $fixture 'empty-hooks')) | Out-Null
    Git-Fixture @('remote','add','origin','https://github.com/DABerezikov/Bonfire.git') | Out-Null
    [IO.File]::WriteAllText((Join-Path $fixture 'tracked.txt'),'first')
    Git-Fixture @('add','tracked.txt') | Out-Null
    Git-Fixture @('commit','-m','fixture') | Out-Null
    $state=Get-BonfirePublishState
    Assert-True $state.Success $state.Error
    [IO.File]::WriteAllText((Join-Path $fixture 'untracked.txt'),'not published')
    Assert-True (Get-BonfirePublishState).Success 'Неотслеживаемый файл блокирует архив отслеживаемых данных.'
    $first=Open-BonfirePublishLock $state.CommonDirectory
    try {
        Assert-True $first.Success 'Нет блокировки.'
        $second=Open-BonfirePublishLock $state.CommonDirectory
        Assert-True (-not $second.Success) 'Параллельная публикация допущена.'
    }
    finally { if($first.Success){$first.Handle.Dispose()} }
    $snapshot=New-BonfirePublishSnapshot $state.Commit
    Assert-True $snapshot.Success $snapshot.Error
    Assert-True (-not (Test-Path -LiteralPath (Join-Path $snapshot.Source 'untracked.txt'))) 'Неотслеживаемые данные попали в снимок.'
    [IO.File]::WriteAllText((Join-Path $fixture 'tracked.txt'),'second')
    Assert-True (-not (Get-BonfirePublishState).Success) 'Грязные отслеживаемые файлы разрешены.'
    Git-Fixture @('add','tracked.txt') | Out-Null
    Git-Fixture @('commit','-m','second') | Out-Null
    Assert-True ([IO.File]::ReadAllText((Join-Path $snapshot.Source 'tracked.txt')) -eq 'first') 'Снимок изменился после нового коммита.'
    Git-Fixture @('switch','-c','feature/fixture') | Out-Null
    Assert-True (-not (Get-BonfirePublishState).Success) 'Публикация feature разрешена.'
    Git-Fixture @('switch','master') | Out-Null
    Git-Fixture @('remote','set-url','origin','https://example.invalid/other.git') | Out-Null
    Assert-True (-not (Get-BonfirePublishState).Success) 'Публикация другого origin разрешена.'
    Git-Fixture @('remote','set-url','origin','https://github.com/DABerezikov/Bonfire.git') | Out-Null
    Git-Fixture @('config','remote.origin.pushurl','https://example.invalid/other.git') | Out-Null
    Assert-True (-not (Get-BonfirePublishState).Success) 'Посторонний pushurl разрешён.'
    [IO.Directory]::CreateDirectory((Join-Path $fixture 'scripts')) | Out-Null
    [IO.Directory]::CreateDirectory((Join-Path $fixture 'subdirectory')) | Out-Null
    [IO.File]::WriteAllText((Join-Path $fixture 'scripts/Publish-Bonfire.ps1'), "[IO.File]::WriteAllText('alias-root.txt', (Get-Location).Path)")
    Git-Fixture @('config','alias.publish','!powershell.exe -NoProfile -STA -ExecutionPolicy Bypass -File scripts/Publish-Bonfire.ps1') | Out-Null
    Git-Fixture @('-C',(Join-Path $fixture 'subdirectory'),'publish') | Out-Null
    Assert-True ([IO.File]::ReadAllText((Join-Path $fixture 'alias-root.txt')) -eq $fixture) 'Alias запущен не из корня.'
    Write-Host 'LOCAL_PUBLISH_GIT_OK: master/dirty/origin, lock, immutable archive without untracked files.'
}
finally {
    Pop-Location
    if($null -ne $snapshot -and $snapshot.Success){Remove-BonfirePublishSnapshot $snapshot}
}
