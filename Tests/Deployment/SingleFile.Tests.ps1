param([Parameter(Mandatory = $true)][string]$PublishDirectory)

$ErrorActionPreference = 'Stop'
$executable = Join-Path $PublishDirectory 'Bonfire.exe'
if (-not (Test-Path -LiteralPath $executable -PathType Leaf)) { throw 'Нет Bonfire.exe.' }
$externalRuntime = @(Get-ChildItem -LiteralPath $PublishDirectory -Recurse -File | Where-Object {
    $_.Extension -eq '.dll' -or $_.Name -match '\.(deps|runtimeconfig)\.json$'
})
if ($externalRuntime.Count -gt 0) {
    throw "Зависимости остались вне Bonfire.exe: $($externalRuntime.Count) файлов."
}
if (-not (Test-Path -LiteralPath (Join-Path $PublishDirectory 'appsettings.json'))) {
    throw 'Настройки должны оставаться внешним файлом.'
}
Write-Host 'SINGLE_FILE_OK: Bonfire.exe содержит управляемые и нативные библиотеки.'
