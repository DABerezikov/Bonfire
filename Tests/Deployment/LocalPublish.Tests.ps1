$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot '../../scripts/Publish-Bonfire.ps1')
$script:passed=0
function Assert-True($Value,$Message){if(-not $Value){throw $Message}}
function Test-Case($Name,$Action){& $Action; $script:passed++; Write-Host "PASS: $Name"}
function New-Fixture {
    $script:events=@(); $script:failure=''; $script:remoteChanged=$false
    $script:remoteReads=0; $script:installed=$false; $script:current=$false
    $script:commit='a'*40; $script:builtCommit=''; $script:updatedCommit=''
}
function Get-BonfirePublishState {
    $script:events+='state'
    if($script:failure -eq 'state'){return [pscustomobject]@{Success=$false;Error='dirty'}}
    return [pscustomobject]@{Success=$true;Commit=$script:commit;Root='fixture';CommonDirectory='git-fixture'}
}
function Open-BonfirePublishLock($Directory){
    $script:events+='lock'
    if($script:failure -eq 'lock'){return [pscustomobject]@{Success=$false;Error='busy'}}
    return [pscustomobject]@{Success=$true;Handle=[IO.MemoryStream]::new()}
}
function Push-BonfirePublishCommit($Commit){
    $script:events+='push'
    return [pscustomobject]@{Success=($script:failure -ne 'push');Error='push failed'}
}
function Test-BonfirePublishRemote($Commit){
    $script:events+='remote'; $script:remoteReads++
    return [pscustomobject]@{Success=(-not ($script:remoteChanged -and $script:remoteReads -eq 2));Error='newer remote'}
}
function Test-BonfirePublishInstalled($Destination,$Commit){$script:events+='current';return [pscustomobject]@{Success=$true;Installed=$script:current}}
function New-BonfirePublishSnapshot($Commit){
    $script:events+='snapshot'; $script:builtCommit=$Commit
    # Моделируем следующий коммит/редактирование сразу после создания снимка.
    $script:commit='b'*40
    return [pscustomobject]@{Success=$true;Root='owned-temp';Source='snapshot';Publish='publish';Error=''}
}
function Build-BonfirePublishSnapshot($Snapshot,$Commit){
    $script:events+='build'
    Assert-True ($Commit -eq 'a'*40) 'Собирается изменённый HEAD.'
    return [pscustomobject]@{Success=($script:failure -ne 'build');Error='test/publish failed'}
}
function Install-BonfirePublishSnapshot($Snapshot,$Destination,$Commit){
    $script:events+='install'; $script:updatedCommit=$Commit
    $script:installed=($script:failure -ne 'install')
    return [pscustomobject]@{Success=$script:installed;Error='install failed'}
}
function Remove-BonfirePublishSnapshot($Snapshot){$script:events+='cleanup'}

Test-Case 'Push затем снимок, проверки, установка именно отправленного SHA' {
    New-Fixture; $result=Invoke-BonfirePublish
    Assert-True $result.Success $result.Error
    Assert-True (($script:events -join ',') -eq 'state,lock,push,remote,current,snapshot,build,remote,install,cleanup') 'Неверный порядок.'
    Assert-True ($script:builtCommit -eq 'a'*40 -and $script:updatedCommit -eq 'a'*40) 'Изменённый HEAD попал в установку.'
}
foreach($phase in @('state','lock','push')){
    Test-Case "Отказ $phase запрещает сборку и установку" {
        New-Fixture; $script:failure=$phase; $result=Invoke-BonfirePublish
        Assert-True (-not $result.Success -and -not $result.PushSucceeded) 'Отказ скрыт.'
        Assert-True (-not $script:installed -and $script:events -notcontains 'build') 'Начата сборка после отказа.'
    }
}
Test-Case 'Отказ тестов/публикации сохраняет уже выполненный push и запрещает установку' {
    New-Fixture; $script:failure='build'; $result=Invoke-BonfirePublish
    Assert-True (-not $result.Success -and $result.PushSucceeded -and -not $script:installed) 'Неверный результат.'
    Assert-True ($script:events -notcontains 'cleanup') 'Диагностический снимок удалён.'
}
Test-Case 'Новый remote после сборки запрещает устаревшую установку' {
    New-Fixture; $script:remoteChanged=$true; $result=Invoke-BonfirePublish
    Assert-True (-not $result.Success -and -not $script:installed) 'Установлен устаревший SHA.'
}
Test-Case 'Уже установленный SHA не пересобирается и не создаёт бэкап' {
    New-Fixture; $script:current=$true; $result=Invoke-BonfirePublish
    Assert-True ($result.Success -and $script:events -notcontains 'build' -and -not $script:installed) 'Повторная установка.'
}
Test-Case 'Отказ установки сохраняет снимок и сообщает отказ после успешного push' {
    New-Fixture; $script:failure='install'; $result=Invoke-BonfirePublish
    Assert-True (-not $result.Success -and $result.PushSucceeded -and $script:events -notcontains 'cleanup') 'Отказ установки скрыт.'
}
Test-Case 'GitHub сохраняет проверки и архив, но не запускает вторую установку' {
    $workflow=Get-Content -LiteralPath (Join-Path $PSScriptRoot '../../.github/workflows/deploy-monoblock.yml') -Raw -Encoding UTF8
    Assert-True ($workflow -notmatch '(?m)^  deploy:' -and $workflow -notmatch 'runs-on: \[self-hosted') 'Облачная установка осталась включённой.'
    Assert-True ($workflow -match 'dotnet test Bonfire.sln' -and $workflow -match 'name: bonfire-win-x64') 'Проверки или архив удалены.'
}
Write-Host "LOCAL_PUBLISH_OK: $script:passed"
