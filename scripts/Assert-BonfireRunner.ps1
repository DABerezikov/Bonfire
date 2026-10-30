$ErrorActionPreference = 'Stop'
if ($env:COMPUTERNAME -ne 'MONOBLOCK') { throw 'Неверный компьютер runner.' }
Write-Host "Runner: $env:RUNNER_NAME; компьютер: $env:COMPUTERNAME"
